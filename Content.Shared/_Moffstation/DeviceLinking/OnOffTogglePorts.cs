using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.DeviceLinking;

/// <summary>
/// This static class supports basic on/off/toggle signal handling. It defines generic ports and
/// provides a <see cref="HandleEnablementSignal">convenient signal handling function</see>.
/// </summary>
public static class OnOffTogglePorts
{
    private static readonly ProtoId<SinkPortPrototype> OnPort = "MoffOnGeneric";
    private static readonly ProtoId<SinkPortPrototype> OffPort = "MoffOffGeneric";
    private static readonly ProtoId<SinkPortPrototype> TogglePort = "MoffToggleGeneric";

    /// <summary>
    /// Handles the given <paramref name="args"/>, this function invokes <paramref name="handler"/>
    /// with the enablement state as determined by the <paramref name="currentState"/> and received
    /// signal. If the signal is received on a port other than one of the ones defined in this class,
    /// <paramref name="handler"/> is not run.
    /// </summary>
    public static void HandleEnablementSignal(
        ref SignalReceivedEvent args,
        bool currentState,
        Action<bool> handler
    )
    {
        if (args.Port == OnPort)
        {
            handler(true);
        }
        else if (args.Port == OffPort)
        {
            handler(false);
        }
        else if (args.Port == TogglePort)
        {
            handler(!currentState);
        }
    }
}
