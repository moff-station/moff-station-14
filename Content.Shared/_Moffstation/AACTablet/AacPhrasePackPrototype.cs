using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._Moffstation.AacTablet;

/// A collection of <see cref="AacPhrase"/>s, grouped under <see cref="AacPhraseTab"/>s and <see cref="AacPhraseGroup"/>s.
[Prototype]
public sealed partial class AacPhrasePackPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// The tabs in this collection.
    [DataField]
    public List<AacPhraseTab> Tabs = new();
}

/// A collection of <see cref="AacPhraseGroup"/>s, displayed together as a tab in the tablet.
[DataRecord]
public partial record struct AacPhraseTab(
    [field: DataField("heading")] LocId HeadingLoc,
    List<AacPhraseGroup> Groups
)
{
    public string Heading => Loc.GetString(HeadingLoc);
}

/// A collection of <see cref="AacPhrase"/>s, displayed as a group in the tablet.
[DataRecord]
public partial record struct AacPhraseGroup(
    [field: DataField("heading")] LocId HeadingLoc,
    List<AacPhrase> Phrases
)
{
    public string Heading => Loc.GetString(HeadingLoc);
}

/// A single AAC phrase.
[DataRecord, Serializable, NetSerializable]
public partial record struct AacPhrase([field: DataField("phrase")] LocId PhraseLoc)
{
    public string Phrase => Loc.GetString(PhraseLoc);
}

/// This serializer allows <see cref="AacPhrase"/>s to be read/written as plain LocIds. If anything is ever added to
/// <see cref="AacPhrase"/>, this will need to be updated.
[TypeSerializer]
public sealed partial class AacPhraseSerializer : ITypeSerializer<AacPhrase, ValueDataNode>
{
    [Dependency] private LocIdSerializer _delegate = default!;

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    ) => _delegate.Validate(serializationManager, node, dependencies, context);

    public AacPhrase Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<AacPhrase>? instanceProvider = null
    ) => new(_delegate.Read(
        serializationManager,
        node,
        dependencies,
        hookCtx,
        context,
        instanceProvider is { } ip ? () => ip.Invoke().Phrase : null)
    );

    public DataNode Write(
        ISerializationManager serializationManager,
        AacPhrase value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    ) => _delegate.Write(serializationManager, value.Phrase, dependencies, alwaysWrite, context);
}
