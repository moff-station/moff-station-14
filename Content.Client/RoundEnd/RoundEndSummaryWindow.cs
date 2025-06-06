using System.Linq;
using System.Numerics;
using Content.Client.Message;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd
{
    public sealed class RoundEndSummaryWindow : DefaultWindow
    {
        private readonly IEntityManager _entityManager;
        public int RoundId;

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, int roundId,
            RoundEndMessageEvent.RoundEndPlayerInfo[] info, IEntityManager entityManager)
        {
            _entityManager = entityManager;

            MinSize = SetSize = new Vector2(520, 580);

            Title = Loc.GetString("round-end-summary-window-title");

            // The round end window is split into two tabs, one about the round stats
            // and the other is a list of RoundEndPlayerInfo for each player.
            // This tab would be a good place for things like: "x many people died.",
            // "clown slipped the crew x times.", "x shots were fired this round.", etc.
            // Also good for serious info.

            RoundId = roundId;
            var roundEndTabs = new TabContainer();
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId));
            roundEndTabs.AddChild(MakePlayerManifestTab(info));

            Contents.AddChild(roundEndTabs);

            OpenCenteredRight();
            MoveToFront();
        }

        private BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration, int roundId)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            // Starlight - Start - Search filter Box
            var searchContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(10, 10, 10, 5),
                VerticalExpand = false,
                HorizontalExpand = true
            };

            var searchLabel = new Label
            {
                Text = Loc.GetString("round-end-summary-window-search-label"),
                MinSize = new Vector2(80, 0),
                VerticalAlignment = VAlignment.Center
            };

            var searchInput = new LineEdit
            {
                PlaceHolder = Loc.GetString("round-end-summary-window-search-placeholder"),
                HorizontalExpand = true,
                MinHeight = 30
            };

            searchContainer.AddChild(searchLabel);
            searchContainer.AddChild(searchInput);
            roundEndSummaryTab.AddChild(searchContainer);
            // Starlight - End

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            var gamemodeMessage = new FormattedMessage();
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-round-id-label", ("roundId", roundId)));
            gamemodeMessage.AddText(" ");
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
            gamemodeLabel.SetMessage(gamemodeMessage);
            roundEndSummaryContainer.AddChild(gamemodeLabel);

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours", roundDuration.Hours),
                                                   ("minutes", roundDuration.Minutes),
                                                   ("seconds", roundDuration.Seconds)));
            roundEndSummaryContainer.AddChild(roundTimeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                // Starlight - Start - Add dynamic search functionality
                UpdateRoundEndTextForSearch(roundEndLabel, roundEnd, "");
                roundEndSummaryContainer.AddChild(roundEndLabel);

                searchInput.OnTextChanged += args =>
                {
                    var isSearchDone = UpdateRoundEndTextForSearch(roundEndLabel, roundEnd, args.Text);
                    // the return value is only interesting for us to know if the two labels should be visible or not
                    gamemodeLabel.Visible = !isSearchDone;
                    roundTimeLabel.Visible = !isSearchDone;
                };
                // Starlight - End
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }

        // Starlight - Start - Search filter Box
        // This adds the filter input box. Skip this part if you only want to know how the list gets populated.
        private bool UpdateRoundEndTextForSearch(RichTextLabel label, string fullText, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // If no search term, show all text
                label.SetMarkup(fullText);
                return false;
            }

            // In the player manifest it's fine to split by every line but
            // in the round end summary it's better to give context to the search term
            // so we split by double newlines to provide the whole paragraph of text
            var blocks = fullText.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries);

            // Filter blocks that contain the search term
            var matchingBlocks = blocks.Where(block =>
                block.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase));

            // Join matching blocks back together
            var filteredText = string.Join("\n\n", matchingBlocks);

            // Set the text to whatever's found
            label.SetMarkup(filteredText);
            return true;
        }
        // Starlight - End

        private BoxContainer MakePlayerManifestTab(RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            var playerManifestTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-player-manifest-tab-title")
            };

            var playerInfoContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var playerInfoContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            // Starlight - Start - Search filter Box
            // This adds the filter input box. Skip this part if you only want to know how the list gets populated.
            var searchContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(10, 10, 10, 5),
                VerticalExpand = false,
                HorizontalExpand = true,
            };

            var searchLabel = new Label
            {
                Text = Loc.GetString("round-end-summary-window-search-label"),
                MinSize = new Vector2(80, 0),
                VerticalAlignment = VAlignment.Center,
            };

            var searchInput = new LineEdit
            {
                PlaceHolder = Loc.GetString("round-end-summary-window-search-placeholder"),
                HorizontalExpand = true,
                MinHeight = 30,
            };

            searchInput.OnTextChanged += args => OnSearchTextChanged(args, playerInfoContainer, playersInfo);

            searchContainer.AddChild(searchLabel);
            searchContainer.AddChild(searchInput);

            playerManifestTab.AddChild(searchContainer);
            // End of search box

            PopulatePlayerManifestList(playerInfoContainer, playersInfo);

            playerInfoContainerScrollbox.AddChild(playerInfoContainer);
            playerManifestTab.AddChild(playerInfoContainerScrollbox);

            return playerManifestTab;
        }

        private void PopulatePlayerManifestList(BoxContainer playerInfoContainer, RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            //Put observers at the bottom of the list. Put antags on top.
            var sortedPlayersInfo = playersInfo.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);

            //Create labels for each player info.
            foreach (var playerInfo in sortedPlayersInfo)
            {
                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var playerInfoText = new RichTextLabel
                {
                    VerticalAlignment = VAlignment.Center,
                    VerticalExpand = true,
                };

                if (playerInfo.PlayerNetEntity != null)
                {
                    hBox.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
                    {
                        OverrideDirection = Direction.South,
                        VerticalAlignment = VAlignment.Center,
                        SetSize = new Vector2(32, 32),
                        VerticalExpand = true,
                    });
                }

                if (playerInfo.PlayerICName != null)
                {
                    if (playerInfo.Observer)
                    {
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-observer-text",
                                          ("playerOOCName", playerInfo.PlayerOOCName),
                                          ("playerICName", playerInfo.PlayerICName)));
                    }
                    else
                    {
                        //TODO: On Hover display a popup detailing more play info.
                        //For example: their antag goals and if they completed them sucessfully.
                        var icNameColor = playerInfo.Antag ? "red" : "white";
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-not-observer-text",
                                ("playerOOCName", playerInfo.PlayerOOCName),
                                ("icNameColor", icNameColor),
                                ("playerICName", playerInfo.PlayerICName),
                                ("playerRole", Loc.GetString(playerInfo.Role))));
                    }
                }
                hBox.AddChild(playerInfoText);
                playerInfoContainer.AddChild(hBox);
            }
        }

        private void OnSearchTextChanged(LineEdit.LineEditEventArgs searchTerm, BoxContainer playerInfoContainer, RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            // Empty the result box when we star typing
            playerInfoContainer.RemoveAllChildren();

            var newText = searchTerm.Text;
            if (string.IsNullOrWhiteSpace(newText))
            {
                // If search is empty, show all text and all players
                PopulatePlayerManifestList(playerInfoContainer, playersInfo);
                return;
            }

            // Filter the player list based on search term
            // Searches: Player OOC Name, Player IC Name, and Player Role
            var filteredPlayersInfo = playersInfo.Where(player =>
                player.PlayerOOCName.Contains(newText, StringComparison.InvariantCultureIgnoreCase) ||
                (player.PlayerICName != null && player.PlayerICName.Contains(newText, StringComparison.InvariantCultureIgnoreCase)) ||
                Loc.GetString(player.Role).Contains(newText, StringComparison.InvariantCultureIgnoreCase)
            )
            .ToArray();

            // Populate the player list with filtered results
            PopulatePlayerManifestList(playerInfoContainer, filteredPlayersInfo);
        }

        // Starlight-end
    }

}
