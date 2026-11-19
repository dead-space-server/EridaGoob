// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingSet
{
    /// <summary>
    /// Points of every category the species' marking points do not list, so no category is left without a limit.
    /// </summary>
    public const int DefaultCategoryPoints = 6;

    private void AddDefaultPoints()
    {
        foreach (var category in Enum.GetValues<MarkingCategories>())
        {
            Points.TryAdd(category, new MarkingPoints { Points = DefaultCategoryPoints });
        }
    }

    /// <summary>
    /// Removes the markings meant for other species.
    /// </summary>
    public void RemoveForeign(string species, MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        List<(MarkingCategories Category, string Id)> toRemove = [];
        foreach (var (category, list) in Markings)
        {
            foreach (var marking in list)
            {
                if (markingManager.TryGetMarking(marking, out var proto) && !MarkingManager.IsRecommendedFor(proto, species))
                    toRemove.Add((category, marking.MarkingId));
            }
        }

        foreach (var (category, id) in toRemove)
        {
            Remove(category, id);
        }
    }
}
