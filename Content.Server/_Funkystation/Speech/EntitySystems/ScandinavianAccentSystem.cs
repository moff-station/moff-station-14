using System.Text;
using System.Text.RegularExpressions;
using Content.Server._Funkystation.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Funkystation.Speech.EntitySystems;

public sealed class ScandinavianAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex RegexLowercaseAe = new(@"ae");
    private static readonly Regex RegexUppercaseAe = new(@"A(?i)e");
    private static readonly Regex RegexLowercaseTh = new(@"th");
    private static readonly Regex RegexUppercaseTh = new(@"T(?i)h");

    private static readonly List<char> ReplacementsA = ['Å', 'Ä'];
    private static readonly List<char> ReplacementsALowerCase = ['å', 'ä'];
    private static readonly List<char> ReplacementsO = ['Ø', 'Ö'];
    private static readonly List<char> ReplacementsOLowerCase = ['ø', 'ö'];

    public override void Initialize()
    {
        SubscribeLocalEvent<ScandinavianAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        message = RegexLowercaseAe.Replace(message, "æ");
        message = RegexUppercaseAe.Replace(message, "Æ");
        message = RegexLowercaseTh.Replace(message, "ð");
        message = RegexUppercaseTh.Replace(message, "Ð");


        var messageBuilder = new StringBuilder(message);


        for (var i = 0; i < messageBuilder.Length; i++)
        {
            if (!_random.Prob(0.3f))
                continue;
            messageBuilder[i] = messageBuilder[i] switch
            {
                'A' => _random.Pick(ReplacementsA),
                'a' => _random.Pick(ReplacementsALowerCase),
                'E' => 'Æ',
                'e' => 'æ',
                'O' => _random.Pick(ReplacementsO),
                'o' => _random.Pick(ReplacementsOLowerCase),
                _ => messageBuilder[i],
            };
        }

        return messageBuilder.ToString();
    }
    private void OnAccent(Entity<ScandinavianAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
