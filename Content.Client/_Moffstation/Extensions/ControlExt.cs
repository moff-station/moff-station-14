using System.Runtime.CompilerServices;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Moffstation.Extensions;

public static class ControlExt
{
    extension(Label label)
    {
        /// If <paramref name="text"/> is not null, sets the text on this label and ensures the label is visible. If
        /// <paramref name="text"/> is null, makes the label not visible.
        public bool SetOrHideText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                label.Text = "";
                label.Visible = false;
                return false;
            }

            label.Text = text;
            label.Visible = true;
            return true;
        }
    }

    extension(ItemList.ItemListSelectedEventArgs args)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemList.Item Item() => args.ItemList[args.ItemIndex];
    }
}
