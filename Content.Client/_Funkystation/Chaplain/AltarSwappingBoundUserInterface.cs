using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._Funkystation.Chaplain;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Funkystation.Chaplain;

[UsedImplicitly]
public sealed partial class AltarSwappingBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<AltarSwappingComponent>(Owner, out var altar))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(CreateOptions(altar));
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> CreateOptions(
        AltarSwappingComponent altar)
    {
        var styles = AltarStyles
            .GetAvailableStyles(_prototypes, altar)
            .OrderBy(prototype => prototype.Name);

        foreach (var prototype in styles)
        {
            EntProtoId prototypeId = prototype.ID;

            yield return new RadialMenuActionOption<EntProtoId>(OnStyleSelected, prototypeId)
            {
                IconSpecifier =
                    new RadialMenuEntityPrototypeIconSpecifier(prototypeId),

                ToolTip = prototype.Name
            };
        }
    }

    private void OnStyleSelected(EntProtoId prototypeId)
    {
        SendMessage(new AltarSwappingSelectMessage(prototypeId));
    }
}
