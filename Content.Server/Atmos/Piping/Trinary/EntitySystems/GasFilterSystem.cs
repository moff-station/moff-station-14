using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class GasFilterSystem : EntitySystem
    {
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private SharedPopupSystem _popupSystem = default!;
        [Dependency] private NodeContainerSystem _nodeContainer = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasFilterComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceDisabledEvent>(OnFilterLeaveAtmosphere);
            SubscribeLocalEvent<GasFilterComponent, ActivateInWorldEvent>(OnFilterActivate);
            SubscribeLocalEvent<GasFilterComponent, GasAnalyzerScanEvent>(OnFilterAnalyzed);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasFilterComponent, GasFilterChangeRateMessage>(OnTransferRateChangeMessage);
            SubscribeLocalEvent<GasFilterComponent, GasFilterToggleStatusMessage>(OnToggleStatusMessage);
            SubscribeLocalEvent<GasFilterComponent, GasFilterToggleGasMessage>(OnToggleGasMessage); // Moffstation - filter multiple gases

        }

        private void OnInit(EntityUid uid, GasFilterComponent filter, ComponentInit args)
        {
            // Moffstation - Begin (filter multiple gases)
            if (filter.FilteredGas is {} filteredGas)
                filter.FilteredGases.Add(filteredGas);
            // Moffstation - End
            UpdateAppearance(uid, filter);
        }

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, ref AtmosDeviceUpdateEvent args)
        {
            if (!filter.Enabled
                || !_nodeContainer.TryGetNodes(uid, filter.InletName, filter.FilterName, filter.OutletName, out PipeNode? inletNode, out PipeNode? filterNode, out PipeNode? outletNode)
                || (outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure && filterNode.Air.Pressure >= Atmospherics.MaxOutputPressure)) // No need to transfer if targets are full.
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferVol = filter.TransferRate * _atmosphereSystem.PumpSpeedup() * args.dt;

            if (transferVol <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var removed = inletNode.Air.RemoveVolume(transferVol);

            // Moffstation - Begin (filter multiple gases)
            var passingGasses = Enum.GetValues<Gas>().Except(filter.FilteredGases).ToHashSet();

            var success = false;
            success |= TryTransfer(removed, filter.FilteredGases, filterNode.Air);
            success |= TryTransfer(removed, passingGasses, outletNode.Air);

            _ambientSoundSystem.SetAmbience(uid, success);
            _atmosphereSystem.Merge(inletNode.Air, removed);
            // Moffstation - End
        }

        private void OnFilterLeaveAtmosphere(EntityUid uid, GasFilterComponent filter, ref AtmosDeviceDisabledEvent args)
        {
            filter.Enabled = false;

            UpdateAppearance(uid, filter);
            _ambientSoundSystem.SetAmbience(uid, false);

            DirtyUI(uid, filter);
            _userInterfaceSystem.CloseUi(uid, GasFilterUiKey.Key);
        }

        private void OnFilterActivate(EntityUid uid, GasFilterComponent filter, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(args.User, out ActorComponent? actor))
                return;

            if (Comp<TransformComponent>(uid).Anchored)
            {
                _userInterfaceSystem.OpenUi(uid, GasFilterUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, filter);
            }
            else
            {
                _popupSystem.PopupCursor(Loc.GetString("comp-gas-filter-ui-needs-anchor"), args.User);
            }

            args.Handled = true;
        }

        private void DirtyUI(EntityUid uid, GasFilterComponent? filter)
        {
            if (!Resolve(uid, ref filter))
                return;

            _userInterfaceSystem.SetUiState(uid, GasFilterUiKey.Key,
                new GasFilterBoundUserInterfaceState(MetaData(uid).EntityName, filter.TransferRate, filter.Enabled, filter.FilteredGases)); // Moffstation - filter multiple gases
        }

        private void UpdateAppearance(EntityUid uid, GasFilterComponent? filter = null)
        {
            if (!Resolve(uid, ref filter, false))
                return;

            _appearanceSystem.SetData(uid, FilterVisuals.Enabled, filter.Enabled);
        }

        private void OnToggleStatusMessage(EntityUid uid, GasFilterComponent filter, GasFilterToggleStatusMessage args)
        {
            filter.Enabled = args.Enabled;
            _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, filter);
            UpdateAppearance(uid, filter);
        }

        private void OnTransferRateChangeMessage(EntityUid uid, GasFilterComponent filter, GasFilterChangeRateMessage args)
        {
            filter.TransferRate = Math.Clamp(args.Rate, 0f, filter.MaxTransferRate);
            _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the transfer rate on {ToPrettyString(uid):device} to {args.Rate}");
            DirtyUI(uid, filter);

        }

        // Moffstation - Begin (filter multiple gases)
        private void OnToggleGasMessage(Entity<GasFilterComponent> ent, ref GasFilterToggleGasMessage args)
        {
            if (!Enum.IsDefined(args.Gas))
            {
                Log.Warning($"{ToPrettyString(ent.Owner)} received GasFilterSelectGasMessage with an invalid ID: {args.Gas}");
                return;
            }

            if (args.Filtered)
            {
                ent.Comp.FilteredGases.Add(args.Gas);
            }
            else
            {
                ent.Comp.FilteredGases.Remove(args.Gas);
            }

            var proto = _atmosphereSystem.GetGas((int) args.Gas);
            _adminLogger.Add(
                LogType.AtmosFilterChanged,
                LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the filter of {Loc.GetString(proto.Name)} on {ToPrettyString(ent.Owner):device} to {args.Filtered.ToString()}");
            DirtyUI(ent.Owner, ent.Comp);
        }
        // Moffstation - End

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnFilterAnalyzed(EntityUid uid, GasFilterComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures ??= new List<(string, GasMixture?)>();

            // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
            if (_nodeContainer.TryGetNode(uid, component.InletName, out PipeNode? inlet) && inlet.Air.Volume != 0f)
            {
                var inletAirLocal = inlet.Air.Clone();
                inletAirLocal.Multiply(inlet.Volume / inlet.Air.Volume);
                inletAirLocal.Volume = inlet.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.FilterName, out PipeNode? filterNode) && filterNode.Air.Volume != 0f)
            {
                var filterNodeAirLocal = filterNode.Air.Clone();
                filterNodeAirLocal.Multiply(filterNode.Volume / filterNode.Air.Volume);
                filterNodeAirLocal.Volume = filterNode.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-filter"), filterNodeAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.OutletName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
            {
                var outletAirLocal = outlet.Air.Clone();
                outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
                outletAirLocal.Volume = outlet.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
            }

            args.DeviceFlipped = inlet != null && filterNode != null && inlet.CurrentPipeDirection.ToDirection() == filterNode.CurrentPipeDirection.ToDirection().GetClockwise90Degrees();
        }


        // Moffstation - Begin (filter multiple gases)
        private bool TryTransfer(GasMixture source, HashSet<Gas> gasses, GasMixture target)
        {
            var limitMoles = AtmosphereSystem.MolesToMaxPressure(source, target, Atmospherics.MaxOutputPressure);
            var availableMoles = gasses.Aggregate(0f, (x, gas) => x + source.GetMoles(gas));

            var transferredMoles = Math.Max(Math.Min(availableMoles, limitMoles), 0f);

            if (transferredMoles <= Atmospherics.GasMinMoles)
                return false;

            var transferredMixture = new GasMixture { Temperature = source.Temperature };
            foreach (var gas in gasses)
            {
                var value = (source.GetMoles(gas) / availableMoles) * transferredMoles;
                transferredMixture.SetMoles(gas, value);
                source.AdjustMoles(gas, -value);
            }

            _atmosphereSystem.Merge(target, transferredMixture);
            return true;
        }
        // Moffstation - End
    }
}
