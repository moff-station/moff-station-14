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
public partial record struct AacPhraseTab(string Heading, List<AacPhraseGroup> Groups);

/// A collection of <see cref="AacPhrase"/>s, displayed as a group in the tablet.
[DataRecord]
public partial record struct AacPhraseGroup(string? Heading, List<AacPhrase> Phrases);

/// A single AAC phrase.
[DataRecord, Serializable, NetSerializable]
public partial record struct AacPhrase(string Phrase);

/// This serializer allows <see cref="AacPhrase"/>s to be read/written as plain LocIds. If anything is ever added to
/// <see cref="AacPhrase"/>, this will need to be updated.
[TypeSerializer]
public sealed partial class AacPhraseSerializer : ITypeSerializer<AacPhrase, ValueDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    ) => serializationManager.ValidateNode<string>(node, context);

    public AacPhrase Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<AacPhrase>? instanceProvider = null
    ) => new(serializationManager.Read<string>(node, context, notNullableOverride: true));

    public DataNode Write(
        ISerializationManager serializationManager,
        AacPhrase value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    ) => serializationManager.WriteValue(value.Phrase, context: context, notNullableOverride: true);
}
