using Content.Client.UserInterface.Controls;
using Content.Shared._Moffstation.Storage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Storage;

[UsedImplicitly]
public sealed partial class LockboxSelectBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _protoManager = default!;

    private SimpleRadialMenu? _menu;

    public LockboxSelectBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not LockboxSelectBoundUserInterfaceState lockboxState || _menu == null)
            return;

        var options = new List<RadialMenuOptionBase>();

        foreach (var protoId in lockboxState.Options)
        {
            var option = new RadialMenuActionOption<EntProtoId>(
                proto => SendMessage(new LockboxSelectMessage(proto)),
                protoId
            )
            {
                IconSpecifier = RadialMenuIconSpecifier.With(protoId),
                ToolTip = GetEntityName(protoId),
            };
            options.Add(option);
        }

        _menu.SetButtons(options);
    }

    private string GetEntityName(EntProtoId protoId)
    {
        if (_protoManager.TryIndex<EntityPrototype>(protoId, out var proto))
            return proto.Name;
        return protoId;
    }
}
