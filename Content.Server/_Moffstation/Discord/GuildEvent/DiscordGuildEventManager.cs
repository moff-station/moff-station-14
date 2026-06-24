using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Discord.GuildEvent;

public sealed partial class DiscordGuildEventManager : IPostInjectInit
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Dependency] private ILogManager _log = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private const string BaseUrl = "https://discord.com/api/v10";
    private readonly HttpClient _http = new();
    private ISawmill _sawmill = default!;

    private ulong? _activeEventId;
    private readonly SemaphoreSlim _lock = new(1, 1);

    void IPostInjectInit.PostInject()
    {
        _sawmill = _log.GetSawmill("discord.guild_events");
    }

    public void Shutdown()
    {
        if (_activeEventId == null)
            return;
        try
        {
            EndActiveEventAsync().GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error ending Discord guild scheduled event on shutdown:\n{e}");
        }
    }

    public async Task EnsureEventActiveAsync(string name, string description, string location)
    {
        if (!IsConfigured())
            return;

        using var _ = await _lock.WaitGuardAsync();
        try
        {
            if (_activeEventId != null)
            {
                if (await GetExistingEventAsync(_activeEventId.Value) != null)
                    return;
                _activeEventId = null;
            }

            // Recover from lost state (e.g. server restart mid-round): claim an existing active event.
            _activeEventId = await FindActiveEventIdAsync(name);
            if (_activeEventId != null)
                return;

            await CreateAndActivateEventAsync(name, description, location);

            if (_activeEventId == null)
                _sawmill.Warning("Failed to create Discord guild scheduled event.");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error ensuring Discord round event active:\n{e}");
        }
    }

    public async Task EndActiveEventAsync()
    {
        using var _ = await _lock.WaitGuardAsync();
        try
        {
            if (_activeEventId == null)
                return;
            await EndEventAsync(_activeEventId.Value);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error ending Discord guild scheduled event:\n{e}");
        }
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_cfg.GetCVar(CCVars.DiscordToken))
            && !string.IsNullOrWhiteSpace(_cfg.GetCVar(CCVars.DiscordGuildId));
    }

    private string GetEventsUrl()
    {
        return $"{BaseUrl}/guilds/{_cfg.GetCVar(CCVars.DiscordGuildId)}/scheduled-events";
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _cfg.GetCVar(CCVars.DiscordToken));
        if (body != null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private async Task CreateAndActivateEventAsync(string name, string description, string location)
    {
        var eventsUrl = GetEventsUrl();
        var now = DateTime.UtcNow;
        var createPayload = new ScheduledEventCreatePayload
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            EntityType = 3,
            EntityMetadata = new ScheduledEventEntityMetadata { Location = location },
            ScheduledStartTime = now.AddSeconds(1).ToString("o"),
            ScheduledEndTime = now.AddHours(24).ToString("o"),
            Status = 1,
        };

        var createResponse = await _http.SendAsync(BuildRequest(HttpMethod.Post, eventsUrl, createPayload));
        if (!createResponse.IsSuccessStatusCode)
        {
            var body = await createResponse.Content.ReadAsStringAsync();
            _sawmill.Error($"Failed to create guild scheduled event. Status: {createResponse.StatusCode}\n{body}");
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<ScheduledEventResponse>(JsonOptions);
        if (created == null)
            return;

        var activatePayload = new ScheduledEventPatchPayload { Status = 2 };
        var activateResponse = await _http.SendAsync(BuildRequest(HttpMethod.Patch, $"{eventsUrl}/{created.Id}", activatePayload));
        if (!activateResponse.IsSuccessStatusCode)
            _sawmill.Warning($"Failed to activate guild scheduled event {created.Id}. Status: {activateResponse.StatusCode}");

        _activeEventId = created.Id;
    }

    private async Task EndEventAsync(ulong eventId)
    {
        var eventsUrl = GetEventsUrl();
        var payload = new ScheduledEventPatchPayload { Status = 3 };
        var response = await _http.SendAsync(BuildRequest(HttpMethod.Patch, $"{eventsUrl}/{eventId}", payload));
        if (!response.IsSuccessStatusCode)
            _sawmill.Error($"Failed to end guild scheduled event {eventId}. Status: {response.StatusCode}");
        else if (_activeEventId == eventId)
            _activeEventId = null;
    }

    private async Task<ScheduledEventResponse?> GetExistingEventAsync(ulong eventId)
    {
        var response = await _http.SendAsync(BuildRequest(HttpMethod.Get, $"{GetEventsUrl()}/{eventId}"));
        if (!response.IsSuccessStatusCode)
            return null;
        var ev = await response.Content.ReadFromJsonAsync<ScheduledEventResponse>(JsonOptions);
        return ev?.Status == 2 ? ev : null;
    }

    private async Task<ulong?> FindActiveEventIdAsync(string name)
    {
        var response = await _http.SendAsync(BuildRequest(HttpMethod.Get, GetEventsUrl()));
        if (!response.IsSuccessStatusCode)
            return null;
        var events = await response.Content.ReadFromJsonAsync<List<ScheduledEventResponse>>(JsonOptions);
        return events?.FirstOrDefault(e => e.Status == 2 && string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;
    }
}

public sealed class ScheduledEventCreatePayload
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("entity_type")] public required int EntityType { get; init; }
    [JsonPropertyName("entity_metadata")] public required ScheduledEventEntityMetadata EntityMetadata { get; init; }
    [JsonPropertyName("scheduled_start_time")] public required string ScheduledStartTime { get; init; }
    [JsonPropertyName("scheduled_end_time")] public required string ScheduledEndTime { get; init; }
    [JsonPropertyName("privacy_level")] public int PrivacyLevel { get; init; } = 2;
    [JsonPropertyName("status")] public required int Status { get; init; }
}

public sealed class ScheduledEventEntityMetadata
{
    [JsonPropertyName("location")] public required string Location { get; init; }
}

public sealed class ScheduledEventPatchPayload
{
    [JsonPropertyName("status")] public required int Status { get; init; }
}

public sealed class ScheduledEventResponse
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong Id { get; init; }

    [JsonPropertyName("status")] public int Status { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}
