// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;
using System.Linq;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingManager
{
    /// <summary>
    /// Every marking of the category the species can wear, ignoring species restrictions.
    /// Empty if the species has no marking points for the category at all.
    /// </summary>
    public IReadOnlyDictionary<string, MarkingPrototype> AllowedMarkings(MarkingCategories category, string species)
    {
        if (!HasCategory(species, category))
            return FrozenDictionary<string, MarkingPrototype>.Empty;

        return MarkingsByCategory(category);
    }

    /// <summary>
    /// Whether the species has marking points for the category.
    /// </summary>
    public bool HasCategory(string species, MarkingCategories category)
    {
        var speciesProto = _prototypeManager.Index<SpeciesPrototype>(species);
        return _prototypeManager.Index(speciesProto.MarkingPoints).Points.ContainsKey(category);
    }

    /// <summary>
    /// Whether the marking lists no species, so it is meant for everyone.
    /// </summary>
    public static bool IsCommon(MarkingPrototype marking)
    {
        return marking.SpeciesRestrictions is not { Count: > 0 };
    }

    /// <summary>
    /// Whether the marking is meant for the species: either it lists the species or it lists none.
    /// </summary>
    public static bool IsRecommendedFor(MarkingPrototype marking, string species)
    {
        return IsCommon(marking) || marking.SpeciesRestrictions!.Contains(species);
    }

    /// <summary>
    /// Drops the markings, hair and facial hair meant for other species, so a new species does not keep the old one's snout or tail.
    /// </summary>
    public HumanoidCharacterAppearance WithoutForeignMarkings(HumanoidCharacterAppearance appearance, string species)
    {
        var markings = appearance.Markings
            .Where(marking => !TryGetMarking(marking, out var proto) || IsRecommendedFor(proto, species))
            .ToList();

        appearance = appearance.WithMarkings(markings);

        if (Markings.TryGetValue(appearance.HairStyleId, out var hair) && !IsRecommendedFor(hair, species))
            appearance = appearance.WithHairStyleName(HairStyles.DefaultHairStyle);

        if (Markings.TryGetValue(appearance.FacialHairStyleId, out var facialHair) && !IsRecommendedFor(facialHair, species))
            appearance = appearance.WithFacialHairStyleName(HairStyles.DefaultFacialHairStyle);

        return appearance;
    }
}
