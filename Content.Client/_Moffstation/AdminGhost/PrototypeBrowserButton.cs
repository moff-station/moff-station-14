using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.AdminGhost;

public sealed class PrototypeBrowserButton : Control
{
    public string PrototypeID => Prototype.ID;
    public EntityPrototype Prototype { get; set; } = default!;
    public Button ActualButton { get; }
    public EntityPrototypeView EntityTextureRects { get; }
    public Label EntityLabel { get; }
    public int Index { get; set; }

    public PrototypeBrowserButton()
    {
        ActualButton = new Button();
        AddChild(ActualButton);

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                (EntityTextureRects = new EntityPrototypeView
                {
                    SetSize = new Vector2(32, 32),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    Stretch = SpriteView.StretchMode.Fill
                }),
                (EntityLabel = new Label
                {
                    VerticalAlignment = VAlignment.Center,
                    HorizontalExpand = true,
                    ClipText = true
                })
            }
        });
    }
}
