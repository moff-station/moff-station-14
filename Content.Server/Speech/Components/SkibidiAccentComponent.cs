using Content.Server.Speech.EntitySystems;
using Content.Server.Speech;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(SkibidiAccentSystem))]
public sealed partial class SkibidiAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cringeChance")]
    public float CringeChance = 0.5f;

    [ViewVariables]
    public readonly List<string> SkibidiPrefixs = new()
    {
        "accent-skibidi-prefix-1",
        "accent-skibidi-prefix-2",
        "accent-skibidi-prefix-3"
    };
    public readonly List<string> SkibidiSuffixs = new()
    {
        "accent-skibidi-suffix-1",
        "accent-skibidi-suffix-2",
        "accent-skibidi-suffix-3"
    };
}
