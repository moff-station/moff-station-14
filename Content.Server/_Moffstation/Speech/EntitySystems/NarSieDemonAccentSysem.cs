using System.Text.RegularExpressions;
using Content.Server._Moffstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Speech.EntitySystems;

public sealed partial class NarSieDemonAccentSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexLowercaseA = new(@"a\B");
    private static readonly Regex RegexLowercaseB = new(@"b\B");
    private static readonly Regex RegexLowercaseC = new(@"c\B");
    private static readonly Regex RegexLowercaseD = new(@"d\B");
    private static readonly Regex RegexLowercaseE = new(@"e\B");
    private static readonly Regex RegexLowercaseF = new(@"f\B");
    private static readonly Regex RegexLowercaseG = new(@"g\B");
    private static readonly Regex RegexLowercaseH = new(@"h\B");
    private static readonly Regex RegexLowercaseI = new(@"i\B");
    private static readonly Regex RegexLowercaseJ = new(@"j\B");
    private static readonly Regex RegexLowercaseK = new(@"k\B");
    private static readonly Regex RegexLowercaseL = new(@"l\B");
    private static readonly Regex RegexLowercaseM = new(@"m\B");
    private static readonly Regex RegexLowercaseN = new(@"n\B");
    private static readonly Regex RegexLowercaseO = new(@"o\B");
    private static readonly Regex RegexLowercaseP = new(@"p\B");
    private static readonly Regex RegexLowercaseQ = new(@"q\B");
    private static readonly Regex RegexLowercaseR = new(@"r\B");
    private static readonly Regex RegexLowercaseS = new(@"s\B");
    private static readonly Regex RegexLowercaseT = new(@"t\B");
    private static readonly Regex RegexLowercaseU = new(@"u\B");
    private static readonly Regex RegexLowercaseV = new(@"v\B");
    private static readonly Regex RegexLowercaseW = new(@"w\B");
    private static readonly Regex RegexLowercaseX = new(@"x\B");
    private static readonly Regex RegexLowercaseY = new(@"y\B");
    private static readonly Regex RegexLowercaseZ = new(@"z\B");

    private static readonly Regex RegexUppercaseA = new(@"A\B");
    private static readonly Regex RegexUppercaseB = new(@"B\B");
    private static readonly Regex RegexUppercaseC = new(@"C\B");
    private static readonly Regex RegexUppercaseD = new(@"D\B");
    private static readonly Regex RegexUppercaseE = new(@"E\B");
    private static readonly Regex RegexUppercaseF = new(@"F\B");
    private static readonly Regex RegexUppercaseG = new(@"G\B");
    private static readonly Regex RegexUppercaseH = new(@"H\B");
    private static readonly Regex RegexUppercaseI = new(@"I\B");
    private static readonly Regex RegexUppercaseJ = new(@"J\B");
    private static readonly Regex RegexUppercaseK = new(@"K\B");
    private static readonly Regex RegexUppercaseL = new(@"L\B");
    private static readonly Regex RegexUppercaseM = new(@"M\B");
    private static readonly Regex RegexUppercaseN = new(@"N\B");
    private static readonly Regex RegexUppercaseO = new(@"O\B");
    private static readonly Regex RegexUppercaseP = new(@"P\B");
    private static readonly Regex RegexUppercaseQ = new(@"Q\B");
    private static readonly Regex RegexUppercaseR = new(@"R\B");
    private static readonly Regex RegexUppercaseS = new(@"S\B");
    private static readonly Regex RegexUppercaseT = new(@"T\B");
    private static readonly Regex RegexUppercaseU = new(@"U\B");
    private static readonly Regex RegexUppercaseV = new(@"V\B");
    private static readonly Regex RegexUppercaseW = new(@"W\B");
    private static readonly Regex RegexUppercaseX = new(@"X\B");
    private static readonly Regex RegexUppercaseY = new(@"Y\B");
    private static readonly Regex RegexUppercaseZ = new(@"Z\B");

    public override void Initialize()
    {
        SubscribeLocalEvent<NarSieDemonAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "NarSieDemon");

        // Lowercase
        msg = RegexLowercaseA.Replace(msg, "a̷̛͖̪̾");
        msg = RegexLowercaseB.Replace(msg, "b̶̤́");
        msg = RegexLowercaseC.Replace(msg, "c̷̦̾");
        msg = RegexLowercaseD.Replace(msg, "d̸̥̠͗̉");
        msg = RegexLowercaseE.Replace(msg, "ê̴̝̩͂");
        msg = RegexLowercaseF.Replace(msg, "f̴̂̏ͅ");
        msg = RegexLowercaseG.Replace(msg, "g̴̨͖̒̓");
        msg = RegexLowercaseH.Replace(msg, "h̸͆͝ͅ");
        msg = RegexLowercaseI.Replace(msg, "i̴̢̲̅͊");
        msg = RegexLowercaseJ.Replace(msg, "j̶̥̈́");
        msg = RegexLowercaseK.Replace(msg, "k̵̥̫͋");
        msg = RegexLowercaseL.Replace(msg, "l̴̫͐͘");
        msg = RegexLowercaseM.Replace(msg, "m̷͔̕");
        msg = RegexLowercaseN.Replace(msg, "n̷̢͝");
        msg = RegexLowercaseO.Replace(msg, "o̵͕̎ͅ");
        msg = RegexLowercaseP.Replace(msg, "p̵̣̍̒");
        msg = RegexLowercaseQ.Replace(msg, "q̸̞̰̈́");
        msg = RegexLowercaseR.Replace(msg, "r̴̼̍");
        msg = RegexLowercaseS.Replace(msg, "s̶̳̣͊͆");
        msg = RegexLowercaseT.Replace(msg, "t̸͍̔");
        msg = RegexLowercaseU.Replace(msg, "ṷ̶͌");
        msg = RegexLowercaseV.Replace(msg, "v̷̰̞̕");
        msg = RegexLowercaseW.Replace(msg, "w̶̹̲͝");
        msg = RegexLowercaseX.Replace(msg, "x̶̯̄");
        msg = RegexLowercaseY.Replace(msg, "ẏ̴̻̞̚");
        msg = RegexLowercaseZ.Replace(msg, "ź̵̠͎͝");

        // Uppercase
        msg = RegexUppercaseA.Replace(msg, "A̴̡̦̪̦̅͛̍̀");
        msg = RegexUppercaseB.Replace(msg, "B̴͕̜̌͑͗͊̏̐");
        msg = RegexUppercaseC.Replace(msg, "C̷͙̀̽͐͆͘͝");
        msg = RegexUppercaseD.Replace(msg, "D̴̲̮̈́͂͠");
        msg = RegexUppercaseE.Replace(msg, "E̶͎̋̊");
        msg = RegexUppercaseF.Replace(msg, "F̵̻̱̳̅̇̅̄͝");
        msg = RegexUppercaseG.Replace(msg, "G̷͕͕̱̓̔̀̈́͗͜͜");
        msg = RegexUppercaseH.Replace(msg, "H̶̘̙̄͐͝");
        msg = RegexUppercaseI.Replace(msg, "Ì̵̧̨̙͕̲");
        msg = RegexUppercaseJ.Replace(msg, "J̵̹̮̙̫͎̥̾̇");
        msg = RegexUppercaseK.Replace(msg, "K̴̟͍͑̈́̒͐");
        msg = RegexUppercaseL.Replace(msg, "L̸̪͉̙̣̼̺͐͌̀̕");
        msg = RegexUppercaseM.Replace(msg, "M̷̙̯̞̘͑̋͆");
        msg = RegexUppercaseN.Replace(msg, "Ň̶̨̨̼̠͓̌̇̌͘̚");
        msg = RegexUppercaseO.Replace(msg, "Ọ̵̘́͑͝");
        msg = RegexUppercaseP.Replace(msg, "P̵̰͆̓̈́̃̕͠");
        msg = RegexUppercaseQ.Replace(msg, "Q̶̧̻͎̺͎̩͘");
        msg = RegexUppercaseR.Replace(msg, "R̷̻̆̊̓̂͠");
        msg = RegexUppercaseS.Replace(msg, "S̵̬̙̦̑̎̽ͅ");
        msg = RegexUppercaseT.Replace(msg, "T̶̰͌͌̀͛͝");
        msg = RegexUppercaseU.Replace(msg, "Ṷ̸̡̙͋");
        msg = RegexUppercaseV.Replace(msg, "V̷̯͈̯̿̀̕");
        msg = RegexUppercaseW.Replace(msg, "Ẅ̷͎͖̻̼͇́̈́");
        msg = RegexUppercaseX.Replace(msg, "X̷̦͎̽");
        msg = RegexUppercaseY.Replace(msg, "Y̴̫̗̍̋͑̾͜");
        msg = RegexUppercaseZ.Replace(msg, "Z̸̮͍͙̺͋̀̽̈́̚");

        return msg;
    }

    private void OnAccent(Entity<NarSieDemonAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
