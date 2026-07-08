using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.MoffPatreon;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.MoffPatreon;

public sealed partial class MoffPatreonSystem : EntitySystem
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    private static readonly ResPath SavePath = new("/moff_patreon.json");

    [Dependency] private IResourceManager _resource = default!;
    [Dependency] private IPlayerLocator _locator = default!;

    private Dictionary<Guid, MoffPatreonSaveEntry> _patronsById = new();

    public override void Initialize()
    {
        base.Initialize();

        _patronsById = Read().ToDictionary(p => p.Patron.Id);
    }

    public IEnumerable<MoffPatreonSaveEntry> GetPatrons() => _patronsById.Values;

    public bool IsMoffPatron(Guid userId)
    {
        return _patronsById.ContainsKey(userId);
    }

    private enum AddPatronResult { Added, AlreadyExists, NotFound }

    public async IAsyncEnumerable<string> AddPatrons(IEnumerable<string> usernameOrIds, ICommonSession addedBy)
    {
        var anyChange = false;
        try
        {
            foreach (var usernameOrId in usernameOrIds)
            {
                switch (await AddPatron(usernameOrId, addedBy))
                {
                    case AddPatronResult.NotFound:
                        yield return usernameOrId;
                        break;
                    case AddPatronResult.Added:
                        anyChange = true;
                        break;
                }
            }
        }
        finally
        {
            if (anyChange)
            {
                await Write();
            }
        }
    }

    private async Task<AddPatronResult> AddPatron(string usernameOrId, ICommonSession addedBy)
    {
        if (await _locator.LookupIdByNameOrIdAsync(usernameOrId) is not { } data)
            return AddPatronResult.NotFound;

        if (_patronsById.ContainsKey(data.UserId.UserId))
            return AddPatronResult.AlreadyExists;

        _patronsById.Add(data.UserId.UserId,
            new MoffPatreonSaveEntry
            {
                Patron = new UsernameAndGuid(data.Username, data.UserId.UserId),
                AddedBy = new UsernameAndGuid(addedBy.Name, addedBy.UserId.UserId),
                AddedAt = DateTime.UtcNow,
            });

        return AddPatronResult.Added;
    }

    public async Task<bool> RemovePatron(Guid userId)
    {
        if (_patronsById.Remove(userId))
        {
            await Write();
            return true;
        }

        return false;
    }

    private List<MoffPatreonSaveEntry> Read()
    {
        if (!_resource.UserData.Exists(SavePath))
        {
            return [];
        }

        try
        {
            using var stream = _resource.UserData.OpenRead(SavePath);
            return JsonSerializer.Deserialize<List<MoffPatreonSaveEntry>>(stream) ?? [];
        }
        catch
        {
            this.AssertOrLogError($"Failed to read patrons from {SavePath}, defaulting to no patrons.");
            return [];
        }
    }

    private async Task Write()
    {
        await Write(_patronsById.Values.ToList());
    }

    private async Task Write(List<MoffPatreonSaveEntry> entries)
    {
        await using var stream = _resource.UserData.OpenWrite(SavePath);
        await JsonSerializer.SerializeAsync(stream, entries, JsonSerializerOptions);
    }
}

public readonly record struct MoffPatreonSaveEntry(
    UsernameAndGuid Patron,
    UsernameAndGuid AddedBy,
    DateTime AddedAt
);
