using System.Linq;
using Content.Shared._Moffstation.Chitter;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._Moffstation.CartridgeLoader.Cartridges;

// Shared rendering logic for a "chat list + message detail" browser over a List<ChitterLogChat> - used
// by both the admin log window (ChitterLogWindow, which has a search box) and the Chitter P.I.
// cartridge fragment (ChitterPiUiFragment, which doesn't). Takes each host's own named Controls
// directly rather than owning them, so both keep their own XAML layout and styling unchanged.
public static class ChitterLogRenderer
{
    public static void RebuildChatList(
        BoxContainer chatList,
        List<ChitterLogChat> chats,
        ChitterLogChat? selected,
        string search,
        Action<ChitterLogChat> onSelect)
    {
        chatList.RemoveAllChildren();

        var filtered = string.IsNullOrEmpty(search)
            ? chats.AsEnumerable()
            : chats.Where(c =>
                c.ChatName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Participants.Any(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)));

        foreach (var chat in filtered.OrderByDescending(c => c.CreatedTime))
        {
            var names = string.Join(", ", chat.Participants.Select(p => p.Name));
            var label = string.IsNullOrWhiteSpace(chat.ChatName) ? names : chat.ChatName;
            if (chat.Archived)
                label = $"[{Loc.GetString("chitter-log-archived")}] {label}";

            var button = new Button
            {
                Text = label,
                ToolTip = names,
                HorizontalExpand = true,
                ClipText = true,
                Pressed = chat == selected,
            };
            button.OnPressed += _ => onSelect(chat);
            chatList.AddChild(button);
        }

        if (chatList.ChildCount == 0)
        {
            chatList.AddChild(new Label
            {
                Text = Loc.GetString("chitter-log-no-chats"),
                FontColorOverride = Color.Gray,
            });
        }
    }

    public static void RebuildMessageList(BoxContainer messageList, Label detailHeader, ChitterLogChat? selected)
    {
        messageList.RemoveAllChildren();

        if (selected == null)
        {
            detailHeader.Text = Loc.GetString("chitter-log-select-chat");
            return;
        }

        var participantNames = string.Join(", ", selected.Participants.Select(p => $"{p.Name} (#{p.AccountId:D4})"));
        detailHeader.Text = string.IsNullOrWhiteSpace(selected.ChatName)
            ? participantNames
            : $"{selected.ChatName} — {participantNames}";

        foreach (var message in selected.Messages)
        {
            var time = TimeSpan.FromSeconds(Math.Truncate(message.Timestamp.TotalSeconds)).ToString();
            var failedSuffix = message.DeliveryFailed ? $" ({Loc.GetString("chitter-log-delivery-failed")})" : "";

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Margin = new Thickness(0, 0, 0, 4),
            };

            row.AddChild(new Label
            {
                Text = $"[{time}] {message.SenderName} (#{message.SenderAccountId:D4}){failedSuffix}",
                FontColorOverride = Color.Gray,
            });
            row.AddChild(new Label
            {
                Text = message.Content,
            });

            messageList.AddChild(row);
        }

        if (selected.Messages.Count == 0)
        {
            messageList.AddChild(new Label
            {
                Text = Loc.GetString("chitter-log-no-messages"),
                FontColorOverride = Color.Gray,
            });
        }
    }
}
