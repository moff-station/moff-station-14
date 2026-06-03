using Content.Shared._Moffstation.Extensions;
using NUnit.Framework;

namespace Content.Tests.Shared._Moffstation;

[TestFixture]
[TestOf(typeof(StringExt))]
public sealed class StringExtTests
{
    [Test]
    [TestCase("some Thing", "some thing")]
    [TestCase("some XThing", "some x-thing")]
    [TestCase("something", "something")]
    [TestCase("someThing", "some-thing")]
    [TestCase("SomeThing", "some-thing")]
    [TestCase("some-thing", "some-thing")]
    [TestCase("SomeXThing", "some-x-thing")]
    public void CamelCaseToKebabCase(string input, string expected)
    {
        Assert.That(input.CamelCaseToKebabCase(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("root", "affix", "root")]
    [TestCase("affixroot", "affix", "affixroot")]
    [TestCase("rootaffix", "affix", "root")]
    [TestCase("affixrootaffix", "affix", "affixroot")]
    [TestCase("roaffixot", "affix", "roaffixot")]
    public void TrimSuffix(string input, string affix, string expected)
    {
        Assert.That(input.TrimSuffix(affix), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("root", "affix", "root")]
    [TestCase("affixroot", "affix", "root")]
    [TestCase("rootaffix", "affix", "rootaffix")]
    [TestCase("affixrootaffix", "affix", "rootaffix")]
    [TestCase("roaffixot", "affix", "roaffixot")]
    public void TrimPrefix(string input, string affix, string expected)
    {
        Assert.That(input.TrimPrefix(affix), Is.EqualTo(expected));
    }
}
