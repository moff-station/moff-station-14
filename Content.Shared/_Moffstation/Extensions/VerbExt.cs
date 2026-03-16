using System.Runtime.CompilerServices;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static class VerbExt
{
    // These extensions are literally copy/pasted because I can't use generic `new()` construction because
    // sandboxing. Kill me.

    extension(SortedSet<UtilityVerb> verbs)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(VerbInfo info, Action act) => verbs.Add(info, default, act);

        public void Add(VerbInfo info, int priority, Action act) =>
            verbs.Add(new UtilityVerb
            {
                Act = act,
                Priority = priority,
                Icon = info.Icon,
                Text = info.Text(),
            });
    }

    extension(SortedSet<AlternativeVerb> verbs)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(VerbInfo info, Action act) => verbs.Add(info, default, act);

        public void Add(VerbInfo info, int priority, Action act) =>
            verbs.Add(new AlternativeVerb
            {
                Act = act,
                Priority = priority,
                Icon = info.Icon,
                Text = info.Text(),
            });
    }

    extension(SortedSet<InteractionVerb> verbs)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(VerbInfo info, Action act) => verbs.Add(info, default, act);

        public void Add(VerbInfo info, int priority, Action act) =>
            verbs.Add(new InteractionVerb
            {
                Act = act,
                Priority = priority,
                Icon = info.Icon,
                Text = info.Text(),
            });
    }
}

public readonly record struct VerbInfo(
    string LocPrefix,
    SpriteSpecifier Icon
)
{
    private static ILocalizationManager LocalizationManager => IoCManager.Resolve<ILocalizationManager>();
    private const string TextSuffix = "-verb-text";
    private const string PopupSuffix = "-popup";
    private const string PopupOtherSuffix = "-popup-other";

    public static VerbInfo Build(
        string locPrefix,
        string verbIconName
    ) => new(
        locPrefix,
        new SpriteSpecifier.Texture(new ResPath($"Interface/VerbIcons/{verbIconName}.svg.192dpi.png"))
    );

    public string Text((string, object)[]? args = null) =>
        LocalizationManager.GetString($"{LocPrefix}{TextSuffix}", args ?? []);

    public string Popup((string, object)[]? args = null) =>
        LocalizationManager.GetString($"{LocPrefix}{PopupSuffix}", args ?? []);

    public string PopupOther((string, object)[]? args = null) =>
        LocalizationManager.GetString($"{LocPrefix}{PopupOtherSuffix}", args ?? []);
}
