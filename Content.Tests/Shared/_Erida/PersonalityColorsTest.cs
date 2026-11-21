// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Content.Shared._Erida.Humanoid.Markings;
using Content.Shared._Erida.SplitPersonality;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Shared._Erida;

[TestFixture]
public sealed class PersonalityColorsTest
{
    [Test]
    public void RecolorPreservesShapeVisibilityAlphaAndGradientStyle()
    {
        var marking = new Marking("OriginalHorns", new[] { Color.Blue.WithAlpha(0.5f), Color.Green })
        {
            Visible = false,
            Forced = true,
            Gradient = new MarkingGradient { Style = MarkingGradientStyle.Split, Offset = 0.3f, Color = Color.Yellow },
        };
        var body = Set(marking);
        var original = PersonalityColors.Copy(body);
        var palette = Set(new Marking("DifferentHorns", new[] { Color.Red, Color.White })
        {
            Gradient = new MarkingGradient { Style = MarkingGradientStyle.Ombre, Offset = 0.8f, Color = Color.Purple },
        });
        PersonalityColors.Apply(body, palette);
        Assert.Multiple(() =>
        {
            Assert.That(marking.MarkingId, Is.EqualTo("OriginalHorns"));
            Assert.That(marking.MarkingColors[0], Is.EqualTo(Color.Red.WithAlpha(0.5f)));
            Assert.That(marking.MarkingColors[1], Is.EqualTo(Color.White));
            Assert.That(marking.Visible, Is.False);
            Assert.That(marking.Forced, Is.True);
            Assert.That(marking.Gradient!.Value.Style, Is.EqualTo(MarkingGradientStyle.Split));
            Assert.That(marking.Gradient.Value.Offset, Is.EqualTo(0.3f));
            Assert.That(marking.Gradient.Value.Color, Is.EqualTo(Color.Purple));
        });
        PersonalityColors.Apply(body, original);
        Assert.That(marking.MarkingColors[0], Is.EqualTo(Color.Blue.WithAlpha(0.5f)));
        Assert.That(marking.Gradient!.Value.Color, Is.EqualTo(Color.Yellow));
    }

    [Test]
    public void MatchingIdsAndDuplicateOccurrencesTakePrecedenceOverPosition()
    {
        var body = Set(new Marking("A", new[] { Color.Blue }), new Marking("A", new[] { Color.Green }));
        var palette = Set(new Marking("B", new[] { Color.Yellow }), new Marking("A", new[] { Color.Red }),
            new Marking("A", new[] { Color.White }));
        PersonalityColors.Apply(body, palette);
        Assert.That(body.Markings[MarkingCategories.Head][0].MarkingColors[0], Is.EqualTo(Color.Red));
        Assert.That(body.Markings[MarkingCategories.Head][1].MarkingColors[0], Is.EqualTo(Color.White));
        Assert.That(palette.Markings[MarkingCategories.Head][0].MarkingColors[0], Is.EqualTo(Color.Yellow));
    }

    [Test]
    public void MissingLayersAndCategoriesDoNotRemoveOrAddFeatures()
    {
        var body = Set(new Marking("A", new[] { Color.Blue, Color.Green }), new Marking("B", new[] { Color.White }));
        var palette = Set(new Marking("A", new[] { Color.Red }));
        PersonalityColors.Apply(body, palette);
        Assert.That(body.Markings[MarkingCategories.Head].Count, Is.EqualTo(2));
        Assert.That(body.Markings[MarkingCategories.Head][0].MarkingColors[1], Is.EqualTo(Color.Green));
        Assert.That(body.Markings[MarkingCategories.Head][1].MarkingColors[0], Is.EqualTo(Color.White));
        PersonalityColors.Apply(body, new MarkingSet());
        Assert.That(body.Markings[MarkingCategories.Head][0].MarkingColors[0], Is.EqualTo(Color.Red));
    }

    [Test]
    public void ProfileCopyAndDirtyComparisonIncludeAlternatePersonality()
    {
        var original = new HumanoidCharacterProfile { AlternatePersonalitySlot = 2 };
        original.Appearance.SkinColor = Color.White;
        var copy = original.WithName("Other");
        Assert.That(copy.AlternatePersonalitySlot, Is.EqualTo(2));
        var same = original.Clone();
        Assert.That(same.MemberwiseEquals(original), Is.True);
        same.AlternatePersonalitySlot = null;
        Assert.That(same.MemberwiseEquals(original), Is.False);
    }

    [TestCase(MarkingCategories.Hair)]
    [TestCase(MarkingCategories.FacialHair)]
    public void EveryLegacyHairLayerRecolorsAndRestores(MarkingCategories category)
    {
        var body = new MarkingSet();
        body.Markings[category] = new()
        {
            new Marking("OriginalHair", new[] { Color.Blue }),
            new Marking("OriginalHair", new[] { Color.Blue }),
            new Marking("AdditionalLayer", new[] { Color.Blue, Color.Yellow }),
        };
        body.Markings[category][0].Gradient = new MarkingGradient
        {
            Style = MarkingGradientStyle.Split, Color = Color.Green,
        };
        var original = PersonalityColors.Copy(body);
        var palette = new MarkingSet();
        palette.Markings[category] = new() { new Marking("DifferentHair", new[] { Color.Red }) };
        PersonalityColors.Apply(body, palette);
        foreach (var marking in body.Markings[category])
            foreach (var color in marking.MarkingColors)
                Assert.That(color, Is.EqualTo(Color.Red));
        Assert.That(body.Markings[category][0].Gradient!.Value.Color, Is.EqualTo(Color.Red));
        Assert.That(body.Markings[category][0].Gradient!.Value.Style, Is.EqualTo(MarkingGradientStyle.Split));
        Assert.That(body.Markings[category][2].MarkingId, Is.EqualTo("AdditionalLayer"));
        PersonalityColors.Apply(body, original);
        for (var i = 0; i < body.Markings[category].Count; i++)
            Assert.That(body.Markings[category][i].MarkingColors, Is.EqualTo(original.Markings[category][i].MarkingColors));
        Assert.That(body.Markings[category][0].Gradient!.Value.Color, Is.EqualTo(Color.Green));
    }

    private static MarkingSet Set(params Marking[] markings) => new()
    {
        Markings = new Dictionary<MarkingCategories, List<Marking>> { [MarkingCategories.Head] = new(markings) },
    };
}
