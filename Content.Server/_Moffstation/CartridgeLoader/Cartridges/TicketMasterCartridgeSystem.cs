using System.Text;
using Content.Shared._Moffstation.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Robust.Server.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

/// <summary>
/// This handles...
/// </summary>
public sealed class TicketMasterCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TicketMasterCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }


    private void OnUiMessage(Entity<TicketMasterCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not TicketMasterPrintMessageEvent cast || _gameTiming.CurTime < ent.Comp.NextAvailablePrint)
            return;

        var printed = Spawn(ent.Comp.MachineOutput, Transform(ent).Coordinates);
        _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);
        _audioSystem.PlayPvs(ent.Comp.SoundPrint, ent.Owner, null);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
            return;


        /* format the piece of paper */
        var text = new StringBuilder();

        text.Append(cast.Ticket.Description);

        _paperSystem.SetContent((printed, paperComp), text.ToString());

        ent.Comp.NextAvailablePrint = _gameTiming.CurTime + ent.Comp.PrintCooldown;
    }

        private void OnPrint(EntityUid ent, TicketMasterCartridgeComponent comp, TicketMasterPrintMessage args)
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
