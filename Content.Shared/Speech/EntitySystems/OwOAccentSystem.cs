using System.Collections.Frozen;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class OwOAccentSystem : RelayAccentSystem<OwOAccentComponent>
{
    [Dependency] private IGameTiming _timing = null!;
    [Dependency] private IRobustRandom _random = null!;

    public static IReadOnlyList<string> Faces { get; } = // Moffstation - remove "readonly" per Rider suggestions
    [
        " (•`ω´•)", " ;;w;;", " owo", " UwU", " >w<", " ^w^",
    ];

    private static readonly FrozenDictionary<string, string> SpecialWords =
        new Dictionary<string, string>
        {
            { "you", "wu" },
        }.ToFrozenDictionary();

    public override string? Accentuate(string message, Entity<OwOAccentComponent>? ent = null) // Moffstation - add ? to string
    {
        _ = ent.HasValue // Moffstation - remove var random per Rider suggestions
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;

        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        /* Moffstation - Start - Change message.replace to exclude faces entirely
        return message.Replace("!", random.Pick(Faces))
            .Replace("r", "w")
            .Replace("R", "W")
            .Replace("l", "w")
            .Replace("L", "W");
         Moffstation - End - Change message.replace to exclude faces entirely*/
        return null;
    }
}
