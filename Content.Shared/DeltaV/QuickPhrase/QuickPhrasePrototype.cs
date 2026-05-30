using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._Moffstation.Extensions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.DeltaV.QuickPhrase;

[Prototype]
public sealed partial class QuickPhrasePrototype : IPrototype, IInheritingPrototype
{
    /// <summary>
    /// The "in code name" of the object. Must be unique.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The prototype we inherit from.
    /// </summary>
    [ViewVariables]
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<QuickPhrasePrototype>))]
    public string[]? Parents { get; set; }

    [ViewVariables]
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; set; }

    /// <summary>
    /// The phrase that this prototype represents.
    /// </summary>
    [DataField]
    public LocId? Text;

    /// <summary>
    /// The localized text of the phrase. If <see cref="Text"/> is not null, that is the localization string used. If it
    /// is null, we construct the localization string based on <see cref="ID"/>, stripping "phrase" from the ends of the
    /// string, converting it from camelCase to kebab-case, and adding "phrase-" to the front.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedText => Loc.GetString(
        Text ?? $"phrase-{ID.Trim("Phrase").CamelCaseToKebabCase()}"
    );

    /// <summary>
    /// The tab in the UI that this phrase falls under.
    /// </summary>
    [DataField(required: true)]
    public LocId Tab;

    /// <summary>
    /// Determines how the phrase is sorted in the UI.
    /// </summary>
    [DataField(required: true)]
    public LocId Group;

    /// <summary>
    /// Color of button in UI.
    /// </summary>
    [DataField]
    public string StyleClass = string.Empty;
}
