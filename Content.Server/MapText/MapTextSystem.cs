using System.Numerics; //Moffstation
using Content.Shared.MapText;
using Robust.Shared.GameStates;

namespace Content.Server.MapText;

/// <inheritdoc/>
public sealed class MapTextSystem : SharedMapTextSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapTextComponent, ComponentGetState>(GetCompState);
    }
//Moffstation - Start - Admin Customization Menu
    public void SetData(EntityUid uid, string? text, Color color, int fontSize, Vector2 offset)
    {
        var comp = EnsureComp<MapTextComponent>(uid);
        comp.Text = text;
        comp.Color = color;
        comp.FontSize = fontSize;
        comp.Offset = offset;
        Dirty(uid, comp);
    }

    public void Clear(EntityUid uid)
    {
        RemComp<MapTextComponent>(uid);
    }
    //Moffstation - End

    private void GetCompState(Entity<MapTextComponent> ent, ref ComponentGetState args)
    {
        args.State = new MapTextComponentState
        {
            Text = ent.Comp.Text,
            LocText = ent.Comp.LocText,
            Color = ent.Comp.Color,
            FontId = ent.Comp.FontId,
            FontSize = ent.Comp.FontSize,
            Offset = ent.Comp.Offset
        };
    }
}
