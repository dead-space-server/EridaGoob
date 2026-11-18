// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.Humanoid.Markings;
using Content.Shared.Humanoid.Markings;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Shared._Erida;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(MarkingGradient))]
public sealed class MarkingGradientDbTest
{
    [Test]
    public void MarkingWithoutGradientKeepsOldFormat()
    {
        var marking = new Marking("HumanHairBob", [Color.Red, Color.Blue]);

        var dbString = marking.ToString();
        var parsed = Marking.ParseFromDbString(dbString);

        Assert.Multiple(() =>
        {
            Assert.That(dbString, Does.Not.Contain(MarkingGradient.DbSeparator));
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.Gradient, Is.Null);
            Assert.That(parsed.MarkingColors, Is.EqualTo(marking.MarkingColors));
        });
    }

    [Test]
    public void MarkingGradientRoundTrips()
    {
        var gradient = new MarkingGradient
        {
            Style = MarkingGradientStyle.Streaks,
            Color = Color.FromHex("#3366CC"),
            Offset = 0.25f,
        };
        var marking = new Marking("HumanHairBob", [Color.FromHex("#AA2200")]) { Gradient = gradient };

        var parsed = Marking.ParseFromDbString(marking.ToString());

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Equals(marking));
    }

    [Test]
    [TestCase("HumanHairBob@#FF0000FF", ExpectedResult = "#FF0000FF")]
    [TestCase("HumanHairBob@#FF0000FF|", ExpectedResult = "#FF0000FF")]
    [TestCase("HumanHairBob@#FF0000FF|9;#00FF00FF;0.5", ExpectedResult = "#FF0000FF")]
    public string OldOrBrokenGradientStillLoadsColors(string input)
    {
        var parsed = Marking.ParseFromDbString(input);

        Assert.That(parsed, Is.Not.Null);
        return parsed!.MarkingColors[0].ToHex();
    }

    [Test]
    [TestCase("#FF0000FF|abc;#00FF00FF;0.5")]
    [TestCase("#FF0000FF|1;not-a-color;0.5")]
    [TestCase("#FF0000FF|1;#00FF00FF;nope")]
    [TestCase("#FF0000FF|1;#00FF00FF")]
    public void BrokenGradientIsDropped(string input)
    {
        var colors = MarkingGradient.SplitFromDb(input, out var gradient);

        Assert.Multiple(() =>
        {
            Assert.That(colors, Is.EqualTo("#FF0000FF"));
            Assert.That(gradient, Is.Null);
        });
    }

    [Test]
    public void UnknownStyleAndOffsetAreSanitized()
    {
        MarkingGradient.SplitFromDb("#FF0000FF|9;#00FF00FF;5", out var gradient);

        Assert.That(gradient, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(gradient!.Value.Style, Is.EqualTo(MarkingGradientStyle.Ombre));
            Assert.That(gradient.Value.Offset, Is.EqualTo(1f));
        });
    }

    [Test]
    public void HairColorRoundTrips()
    {
        var gradient = new MarkingGradient { Style = MarkingGradientStyle.DipDye, Color = Color.FromHex("#112233"), Offset = 0.8f };
        var stored = MarkingGradient.AppendToDb(Color.Black.ToHex(), gradient);

        var colors = MarkingGradient.SplitFromDb(stored, out var parsed);

        Assert.Multiple(() =>
        {
            Assert.That(Color.FromHex(colors), Is.EqualTo(Color.Black));
            Assert.That(parsed, Is.EqualTo(gradient));
        });
    }
}
