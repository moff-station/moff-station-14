using System.Collections.Frozen;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class OwOAccentSystem : RelayAccentSystem<OwOAccentComponent>
{
    // Moff start - Remove owo faces
    // [Dependency] private IGameTiming _timing = default!;
    // [Dependency] private IRobustRandom _random = default!;
    //
    // private static readonly IReadOnlyList<string> Faces =
    // [
    //     " (•`ω´•)", " ;;w;;", " owo", " UwU", " >w<", " ^w^",
    // ];
    // Moff end

    private static readonly FrozenDictionary<string, string> SpecialWords =
        new Dictionary<string, string>
        {
            { "you", "wu" },
        }.ToFrozenDictionary();

    public override string Accentuate(string message, Entity<OwOAccentComponent>? ent = null)
    {
        // Moff start - Remove owo faces
        // var random = ent.HasValue
        //     ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
        //     : _random;
        // Moff end

        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        return message //.Replace("!", random.Pick(Faces)) // Moff - Remove owo faces
            .Replace("r", "w")
            .Replace("R", "W")
            .Replace("l", "w")
            .Replace("L", "W");
    }
}
