// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared._Erida.SplitPersonality;

/// <summary>Recolors existing markings without adding, removing or changing any physical feature.</summary>
public static class PersonalityColors
{
    public static void Apply(MarkingSet body, MarkingSet palette)
    {
        foreach (var (category, markings) in body.Markings)
        {
            if (!palette.Markings.TryGetValue(category, out var colors))
                continue;

            // Hair has one profile color, but legacy profiles can contain several hair markings.
            var hair = category is MarkingCategories.Hair or MarkingCategories.FacialHair;
            var uniformHair = hair && colors.Count == 1;
            var occurrences = new Dictionary<string, int>();
            for (var index = 0; index < markings.Count; index++)
            {
                var marking = markings[index];
                var occurrence = occurrences.GetValueOrDefault(marking.MarkingId);
                occurrences[marking.MarkingId] = occurrence + 1;
                // Prefer the same feature. For a different horn/wing style use its position in the category.
                var source = uniformHair ? colors.FirstOrDefault() :
                    colors.Where(m => m.MarkingId == marking.MarkingId).Skip(occurrence).FirstOrDefault()
                    ?? (index < colors.Count ? colors[index] : null);
                if (source == null)
                    continue;

                for (var layer = 0; layer < marking.MarkingColors.Count; layer++)
                {
                    var sourceLayer = uniformHair ? Math.Min(layer, source.MarkingColors.Count - 1) : layer;
                    if (sourceLayer >= 0 && sourceLayer < source.MarkingColors.Count)
                        marking.SetColor(layer, source.MarkingColors[sourceLayer].WithAlpha(marking.MarkingColors[layer].A));
                }

                if (marking.Gradient is { } gradient)
                {
                    var accent = source.Gradient?.Color;
                    if (accent == null && hair && source.MarkingColors.Count > 0)
                        accent = source.MarkingColors[0];
                    if (accent is { } color)
                        marking.Gradient = gradient with { Color = color.WithAlpha(gradient.Color.A) };
                }
            }
        }
    }

    public static MarkingSet Copy(MarkingSet source)
    {
        var copy = new MarkingSet();
        foreach (var (category, markings) in source.Markings)
            copy.Markings[category] = markings.Select(m => new Marking(m)).ToList();
        return copy;
    }
}
