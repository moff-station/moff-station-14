using System.Text.RegularExpressions;
using Content.Server._Moffstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Moffstation.Speech.EntitySystems;

public sealed partial class GrayAccentSystem : EntitySystem
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexLowercaseOu = new(@"\Bou(?!nt\b)");
    private static readonly Regex RegexUppercaseOu = new(@"\BOU(?!NT\b)");
    private static readonly Regex RegexLowercaseOo = new(@"oo\B");
    private static readonly Regex RegexUppercaseOo = new(@"OO\B");
    private static readonly Regex RegexSentenceCaseOo = new(@"Oo\B");
    private static readonly Regex RegexLowercaseEndingE = new(@"\Be(?=(s\b|\b))");
    private static readonly Regex RegexUppercaseEndingE = new(@"\BE(?=(s\b|\b))");
    private static readonly Regex RegexLowercaseEndingY = new(@"\By(?=(s\b|\b))");
    private static readonly Regex RegexUppercaseEndingY = new(@"\BY(?=(s\b|\b))");
    private static readonly Regex RegexLowercaseC = new(@"c\B");
    private static readonly Regex RegexUppercaseC = new(@"C\B");
    private static readonly Regex RegexLowercaseAh = new(@"\Ba(?=(r|h))");
    private static readonly Regex RegexUppercaseAh = new(@"\BA(?=(R|H))");
    private static readonly Regex RegexLowercaseEr = new(@"er\B");
    private static readonly Regex RegexUppercaseEr = new(@"ER\B");
    private static readonly Regex RegexSentenceCaseEr = new(@"Er\B");
    private static readonly Regex RegexLowercaseUr = new(@"ur\B");
    private static readonly Regex RegexUppercaseUr = new(@"UR\B");
    private static readonly Regex RegexSentenceCaseUr = new(@"Ur\B");

    public override void Initialize()
    {
        SubscribeLocalEvent<GrayAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "Gray");

        // mouse -> moose
        // this doesn't change "bounty" because that feels wrong to me
        msg = RegexLowercaseOu.Replace(msg, "oo");
        msg = RegexUppercaseOu.Replace(msg, "OO");

        // goodbye -> go'udbye
        msg = RegexLowercaseOo.Replace(msg, "o'u");
        msg = RegexUppercaseOo.Replace(msg, "O'U");
        msg = RegexSentenceCaseOo.Replace(msg, "O'u");

        // meter -> metah
        msg = RegexLowercaseEndingE.Replace(msg, "ii");
        msg = RegexUppercaseEndingE.Replace(msg, "II");

        // somebody -> somebod'i
        msg = RegexLowercaseEndingY.Replace(msg, "'i");
        msg = RegexUppercaseEndingY.Replace(msg, "'I");

        // cat -> xeat
        msg = RegexLowercaseC.Replace(msg, "xe");
        msg = RegexUppercaseC.Replace(msg, "XE");

        // hue -> hø
        msg = RegexLowercaseAh.Replace(msg, "'arc");
        msg = RegexUppercaseAh.Replace(msg, "'ARC");

        // everyone -> evrëyone
        msg = RegexLowercaseEr.Replace(msg, "rë");
        msg = RegexUppercaseEr.Replace(msg, "RË");
        msg = RegexSentenceCaseEr.Replace(msg, "Rë");

        // yuri -> yaoi
        msg = RegexLowercaseUr.Replace(msg, "ao");
        msg = RegexUppercaseUr.Replace(msg, "AO");
        msg = RegexSentenceCaseUr.Replace(msg, "Ao");

        return msg;
    }
    private void OnAccent(Entity<GrayAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
