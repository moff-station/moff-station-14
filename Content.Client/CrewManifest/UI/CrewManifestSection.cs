// Moff start - Moved to `CrewManifestSection.xaml.cs
// using System.Numerics;
// using Content.Shared.CrewManifest;
// using Content.Shared.Roles;
// using Content.Shared.StatusIcon;
// using Robust.Client.GameObjects;
// using Robust.Client.Graphics;
// using Robust.Client.UserInterface.Controls;
// using Robust.Shared.Prototypes;
//
// namespace Content.Client.CrewManifest.UI;
//
// public sealed class CrewManifestSection : BoxContainer
// {
//     private const int NameColumnMinWidth = 190; // Moff - improved manifest
//     public const string GenderStyleClass = "CrewManifestGender"; // Moff - improved manifest
//
//     public CrewManifestSection(
//         IPrototypeManager prototypeManager,
//         SpriteSystem spriteSystem,
//         DepartmentPrototype section,
//         List<CrewManifestEntry> entries)
//     {
//         Orientation = LayoutOrientation.Vertical;
//         HorizontalExpand = true;
//
//         // Moff - improved manifest - department header
//         // AddChild(new Label()
//         // {
//         //     StyleClasses = { "LabelBig" },
//         //     Text = Loc.GetString(section.Name)
//         // });
//
//         AddChild(new PanelContainer
//         {
//             PanelOverride = new StyleBoxFlat { BackgroundColor = section.Color },
//             Children =
//             {
//                 new RichTextLabel
//                 {
//                     Text = Loc.GetString("moff-crew-manifest-department-header", ("departmentName", Loc.GetString(section.Name))),
//                     Margin = new Thickness(5f, 2f, 0, 2f),
//                 },
//             },
//         });
//         // Moff end
//
//         var gridContainer = new GridContainer()
//         {
//             Margin = new Thickness(0, 4, 0, 0), // Moff
//             HorizontalExpand = true,
//             Columns = 2
//         };
//
//         // AddChild(gridContainer); // Moff
//
//         foreach (var entry in entries)
//         {
//             // Moff start - improved crew manifest
//             var nameLabel = new RichTextLabel();
//             // {
//             //     HorizontalExpand = true,
//             // };
//             // Moff end
//             nameLabel.SetMessage(entry.Name);
//
//             // Moff start - improved crew manifest - display pronouns
//             var gender = new RichTextLabel
//             {
//                 Margin = new Thickness(6, 0, 0, 0),
//                 StyleClasses = { GenderStyleClass },
//             };
//             gender.SetMessage(Loc.GetString("gender-display", ("gender", entry.Gender)), Color.Gray);
//
//             var name = new BoxContainer
//             {
//                 Orientation = LayoutOrientation.Horizontal,
//                 MinWidth = NameColumnMinWidth,
//                 Children = { nameLabel, gender },
//             };
//             // Moff end
//
//             var titleContainer = new BoxContainer()
//             {
//                 Orientation = LayoutOrientation.Horizontal,
//                 HorizontalExpand = true
//             };
//
//             var title = new RichTextLabel();
//             title.SetMessage(entry.JobTitle);
//
//
//             if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
//             {
//                 var icon = new TextureRect()
//                 {
//                     TextureScale = new Vector2(2, 2),
//                     VerticalAlignment = VAlignment.Center,
//                     Texture = spriteSystem.Frame0(jobIcon.Icon),
//                     Margin = new Thickness(0, 0, 4, 0)
//                 };
//
//                 titleContainer.AddChild(icon);
//                 titleContainer.AddChild(title);
//             }
//             else
//             {
//                 titleContainer.AddChild(title);
//             }
//
//             gridContainer.AddChild(name);
//             gridContainer.AddChild(titleContainer);
//         }
//
//         AddChild(gridContainer); // Moff
//     }
// }
// Moff end
