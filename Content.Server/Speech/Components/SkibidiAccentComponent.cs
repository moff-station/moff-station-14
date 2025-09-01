using Content.Server.Speech.EntitySystems;
using Content.Server.Speech;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(SkibidiAccentSystem))]
public sealed partial class SkibidiAccentComponent : Component
{
    // Create a variable to control on average how cringe someone is.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cringeChance")]
    public float CringeChance = 0.8f; // 80% chance for a message to be cringe

    // Create a separate value for both the Prefix and Suffix Chance. So each sent message doesn't
    // always have a prefix. Or always have a suffix.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cringePrefixChance")]
    public float CringePrefixChance = 0.75f; // 75% of cringe messages has a prefix

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cringeSuffixChance")]
    public float CringeSuffixChance = 0.75f; // 75% of cringe messages has a suffix

    [ViewVariables]
    public readonly List<string> SkibidiPrefixs = new()
    {
        "accent-skibidi-prefix-1",
        "accent-skibidi-prefix-2",
        "accent-skibidi-prefix-3",
        "accent-skibidi-prefix-4",
        "accent-skibidi-prefix-5",
        "accent-skibidi-prefix-6",
        "accent-skibidi-prefix-7",
        "accent-skibidi-prefix-8",
        "accent-skibidi-prefix-9",
        "accent-skibidi-prefix-10"
    };
    public readonly List<string> SkibidiSuffixs = new()
    {
        "accent-skibidi-suffix-1",
        "accent-skibidi-suffix-2",
        "accent-skibidi-suffix-3",
        "accent-skibidi-suffix-4",
        "accent-skibidi-suffix-5",
        "accent-skibidi-suffix-6",
        "accent-skibidi-suffix-7",
        "accent-skibidi-suffix-8",
        "accent-skibidi-suffix-9",
        "accent-skibidi-suffix-10",
        "accent-skibidi-suffix-11",
        "accent-skibidi-suffix-12"
    };
}
