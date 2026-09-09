using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Initializes a <see cref="GasFilterWindow"/> and updates it from the entity's <see cref="GasFilterComponent"/>.
/// </summary>
[UsedImplicitly]
public sealed partial class GasFilterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    [ViewVariables]
    private GasFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GasFilterWindow>();
        // _window.PopulateGasList(_atmosphere.Gases); // Mof - filter multiple gases

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.FilterTransferRateChanged += OnFilterTransferRatePressed;
        // _window.SelectGasPressed += OnSelectGasPressed; // Mof - filter multiple gases
        _window.FilterGasToggled += OnToggleGasPressed; // Mof - filter multiple gases

        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out GasFilterComponent? filter))
            return;

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.SetFilterStatus(filter.Enabled);
        _window.SetTransferRate(filter.TransferRate);

        // Moff start - Filter multiple gasses
        _window.SetFilteredGases(filter.FilteredGases);

        // if (filter.FilteredGas is { } filtered)
        // {
        //     var gas = _atmosphere.GetGas(filtered);
        //     _window.SetGasFiltered(gas.ID, Loc.GetString(gas.Name));
        // }
        // else
        // {
        //     _window.SetGasFiltered(null, Loc.GetString("comp-gas-filter-ui-filter-gas-none"));
        // }
        // Moff end
    }

    private void OnToggleStatusButtonPressed(bool status)
    {
        SendPredictedMessage(new GasFilterToggleStatusMessage(status));
    }

    private void OnFilterTransferRatePressed(string value)
    {
        var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;

        SendPredictedMessage(new GasFilterChangeRateMessage(rate));
    }

    // Moff start - Filter multiple gassses
    private void OnToggleGasPressed(Gas gas, bool filtered)
    {
        if (_window is null)
            return;

        SendPredictedMessage(new GasFilterSelectGasMessage(gas, filtered));
    }

    // private void OnSelectGasPressed()
    // {
    //     if (_window is null)
    //         return;
    //
    //     if (_window.SelectedGas is null)
    //     {
    //         SendPredictedMessage(new GasFilterSelectGasMessage(null));
    //     }
    //     else
    //     {
    //         if (!Enum.TryParse<Gas>(_window.SelectedGas, out var gas))
    //             return;
    //
    //         SendPredictedMessage(new GasFilterSelectGasMessage(gas));
    //     }
    // }
    // Moff end
}
