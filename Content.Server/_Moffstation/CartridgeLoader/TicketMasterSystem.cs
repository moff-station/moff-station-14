using Content.Server.Hands.Systems;
using Content.Shared._Moffstation.CartridgeLoader;
using Content.Shared.Hands.EntitySystems;
using Microsoft.Extensions.Logging;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.CartridgeLoader;

/// <summary>
/// This handles...
/// </summary>
public sealed class TicketMasterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TicketMasterComponent, TicketMasterPrintMessage>(OnPrint);
    }

    private void OnPrint(EntityUid ent, TicketMasterComponent comp, TicketMasterPrintMessage args)
    {
        var user = args.Actor;

        if (_gameTiming.CurTime < comp.NextAvailablePrint)
            return;

        var printed = Spawn(comp.MachineOutput, Transform(ent).Coordinates);
        _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

        /* format the piece of paper
            _metaData.SetEntityName(printed, Loc.GetString("forensic-scanner-report-title", ("entity", component.LastScannedName)));

            var text = new StringBuilder();

            text.AppendLine(Loc.GetString("forensic-scanner-interface-fingerprints"));
            foreach (var fingerprint in component.Fingerprints)
            {
                text.AppendLine(fingerprint);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-fibers"));
            foreach (var fiber in component.Fibers)
            {
                text.AppendLine(fiber);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-dnas"));
            foreach (var dna in component.TouchDNAs)
            {
                text.AppendLine(dna);
            }
            foreach (var dna in component.SolutionDNAs)
            {
                Log.Debug(dna);
                if (component.TouchDNAs.Contains(dna))
                    continue;
                text.AppendLine(dna);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-residues"));
            foreach (var residue in component.Residues)
            {
                text.AppendLine(residue);
            }

            _paperSystem.SetContent((printed, paperComp), text.ToString());
            _audioSystem.PlayPvs(component.SoundPrint, uid,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));
         */
        comp.NextAvailablePrint = _gameTiming.CurTime + comp.PrintCooldown;
    }
}
