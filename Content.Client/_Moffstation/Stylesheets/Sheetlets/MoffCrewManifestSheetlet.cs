using Content.Client.CrewManifest.UI;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Moffstation.Stylesheets.Sheetlets;

/// <summary>
/// Provides style classes. In this case, just to style the pronouns on the crew manifest.
/// </summary>
[CommonSheetlet]
public sealed class MoffCrewManifestSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config) =>
    [
        Client.Stylesheets.StylesheetHelpers.E<RichTextLabel>()
            .Class(CrewManifestEntryControl.GenderStyleClass)
            .Font(sheet.BaseFont.GetFont(10, FontKind.Italic)),
    ];
}
