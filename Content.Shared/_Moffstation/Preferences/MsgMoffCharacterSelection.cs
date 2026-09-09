using System.IO;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Preferences;

/// <summary>
/// Sent by the server to hand the client its full <see cref="MoffCharacterSelectionState"/>,
/// either on connect or after the server has changed it.
/// </summary>
public sealed class MsgMoffCharacterSelectionState : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public MoffCharacterSelectionState State = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(length);
        buffer.ReadAlignedMemory(stream, length);
        State = serializer.Deserialize<MoffCharacterSelectionState>(stream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        using var stream = new MemoryStream();
        serializer.Serialize(stream, State);
        buffer.WriteVariableInt32((int)stream.Length);
        stream.TryGetBuffer(out var segment);
        buffer.Write(segment);
    }
}

/// <summary>
/// Sent by the client to replace its player-global job priorities.
/// </summary>
public sealed class MsgUpdateMoffJobPriorities : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(length);
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out JobPriorities);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        using var stream = new MemoryStream();
        serializer.SerializeDirect(stream, JobPriorities);
        buffer.WriteVariableInt32((int)stream.Length);
        stream.TryGetBuffer(out var segment);
        buffer.Write(segment);
    }
}

/// <summary>
/// Sent by the client to mark one of its character slots active or inactive.
/// </summary>
public sealed class MsgSetMoffCharacterEnabled : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Slot;
    public bool Enabled;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Slot = buffer.ReadVariableInt32();
        Enabled = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Slot);
        buffer.Write(Enabled);
    }
}
