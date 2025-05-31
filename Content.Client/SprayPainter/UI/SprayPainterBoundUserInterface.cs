using Content.Client.SprayPainter.Airlocks;
using Content.Client.SprayPainter.Airlocks.UI;
using Content.Client.SprayPainter.AtmosPipes.UI;
using Content.Client.SprayPainter.GasTanks.UI;
using Content.Shared.SprayPainter.Airlocks;
using Content.Shared.SprayPainter.Airlocks.Components;
using Content.Shared.SprayPainter.AtmosPipes;
using Content.Shared.SprayPainter.GasTanks;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SprayPainter.UI;

[UsedImplicitly]
public sealed class SprayPainterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    protected override void Open()
    {
        base.Open();

        var window = this.CreateWindow<SprayPainterWindow>();

        if (EntMan.TryGetComponent(Owner, out AirlockPainterComponent? airlockPainter))
        {
            window.AddTab(
                new AirlockPainterWindow(
                    EntMan.System<AirlockPainterSystem>().Entries,
                    airlockPainter,
                    styleIndex => SendMessage(new AirlockPainterSpritePickedMessage(styleIndex))
                ),
                Loc.GetString("spray-painter-category-airlocks")
            );
        }

        if (EntMan.TryGetComponent(Owner, out AtmosPipePainterComponent? pipePainter))
        {
            window.AddTab(
                new AtmosPipePainterWindow(
                    pipePainter,
                    colorKey => SendMessage(new AtmosPipePainterColorPickedMessage(colorKey))
                ),
                Loc.GetString("spray-painter-category-pipes")
            );
        }

        if (EntMan.TryGetComponent(Owner, out GasTankPainterComponent? gasTankPainter))
        {
            window.AddTab(
                new GasTankPainterWindow(
                    gasTankPainter.ConfiguredVisuals,
                    visuals => SendMessage(new GasTankPainterConfigUpdateMessage(visuals))
                ),
                Loc.GetString("spray-painter-category-gasTanks")
            );
        }
    }
}
