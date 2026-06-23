using System.Text.RegularExpressions;
using Content.Server._Moffstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Speech.EntitySystems;

public sealed partial class GrayAccentSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
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

    // Controls the likelyhood of the more difficult to understand parts of the gray accent
    // TODO: In the future we should add this as a customization option, but for now turning it down should suffice
    private const float GleebGlorpGloopSpunch = 0.6f;

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
        msg = ChanceReplace(msg, RegexLowercaseOo, "o'u");
        msg = ChanceReplace(msg, RegexUppercaseOo, "O'U");
        msg = ChanceReplace(msg, RegexSentenceCaseOo, "O'u");

        // vote -> votii
        msg = RegexLowercaseEndingE.Replace(msg, "ii");
        msg = RegexUppercaseEndingE.Replace(msg, "II");

        // somebody -> somebod'i
        msg = ChanceReplace(msg, RegexLowercaseEndingY, "'i");
        msg = ChanceReplace(msg, RegexUppercaseEndingY, "'I");

        // cat -> xeat
        msg = ChanceReplace(msg, RegexLowercaseC, "xe");
        msg = ChanceReplace(msg, RegexUppercaseC, "XE");

        // blah -> bl'arch
        msg = ChanceReplace(msg, RegexLowercaseAh, "'arc");
        msg = ChanceReplace(msg, RegexUppercaseAh, "'ARC");

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

    private string ChanceReplace(string input, Regex regex, string replacement)
    {
        return regex.Replace(input, match => _random.Prob(GleebGlorpGloopSpunch) ? replacement : match.Value);
    }

    private void OnAccent(Entity<GrayAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
