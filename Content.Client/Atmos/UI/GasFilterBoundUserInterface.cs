using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasFilterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasFilterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private const float MaxTransferRate = Atmospherics.MaxTransferRate;

        [ViewVariables]
        private GasFilterWindow? _window;

        public GasFilterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var atmosSystem = EntMan.System<AtmosphereSystem>();

            _window = this.CreateWindow<GasFilterWindow>();

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.FilterTransferRateChanged += OnFilterTransferRatePressed;
            _window.FilterGasToggled += OnToggleGasPressed; // Moffstation - filter multiple gases
        }

        private void OnToggleStatusButtonPressed(bool status)
        {
            SendMessage(new GasFilterToggleStatusMessage(status));
        }

        private void OnFilterTransferRatePressed(string value)
        {
            var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;

            SendMessage(new GasFilterChangeRateMessage(rate));
        }

        // Moffstation - Begin (filter multiple gases)
        private void OnToggleGasPressed(Gas gas, bool filtered)
        {
            if (_window is null)
                return;

            SendMessage(new GasFilterToggleGasMessage(gas, filtered));
        }
        // Moffstation - End

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasFilterBoundUserInterfaceState cast)
                return;

            _window.Title = (cast.FilterLabel);
            _window.SetFilterStatus(cast.Enabled);
            _window.SetTransferRate(cast.TransferRate);
            _window.SetFilteredGases(cast.FilteredGases); // Moffstation - filter multiple gases
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
