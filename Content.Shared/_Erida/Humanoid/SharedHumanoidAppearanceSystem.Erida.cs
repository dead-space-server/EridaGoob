// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.Humanoid.Markings;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem
{
    /// <summary>
    /// Adds a copy of the marking with its colors and gradient.
    /// </summary>
    public void AddMarking(EntityUid uid,
        Marking marking,
        bool sync = true,
        bool forced = false,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking.MarkingId, out var prototype))
            return;

        var markingObject = new Marking(marking.MarkingId, marking.MarkingColors)
        {
            Forced = forced,
            Gradient = prototype.ForcedColoring ? null : marking.Gradient?.Sanitized(),
        };

        humanoid.MarkingSet.AddBack(prototype.MarkingCategory, markingObject);
        RaiseMarkingsChanged(uid);

        if (sync)
            Dirty(uid, humanoid);
    }

    /// <summary>
    /// Sets the gradient of the marking in a category at an index in the category's list.
    /// </summary>
    /// <param name="gradient">The new gradient, null to remove it.</param>
    public void SetMarkingGradient(EntityUid uid,
        MarkingCategories category,
        int index,
        MarkingGradient? gradient,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count
            || !_markingManager.TryGetMarking(markings[index], out var prototype)
            || prototype.ForcedColoring)
            return;

        markings[index].Gradient = gradient?.Sanitized();
        Dirty(uid, humanoid);
    }

    /// <summary>
    /// Sets the gradient of the marking added last to a category, such as hair right after loading it from a profile.
    /// </summary>
    public void SetLastMarkingGradient(EntityUid uid, MarkingCategories category, MarkingGradient? gradient)
    {
        if (gradient == null
            || !TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || markings.Count == 0)
            return;

        SetMarkingGradient(uid, category, markings.Count - 1, gradient, humanoid);
    }

    /// <summary>
    /// Erida and Nebula export hair as a marking in the head's hair layer, while here it lives in its own fields.
    /// Moves the first marking of a hair or facial hair layer into those fields, with its color and gradient.
    /// </summary>
    /// <returns>True if the layer was a hair layer, so its markings must not go to the marking list.</returns>
    private static bool TryMoveHairLayer(MappingDataNode appearance, string layer, SequenceDataNode markings)
    {
        var prefix = layer switch
        {
            nameof(HumanoidVisualLayers.Hair) => "hair",
            nameof(HumanoidVisualLayers.FacialHair) => "facialHair",
            _ => null,
        };

        if (prefix == null)
            return false;

        if (appearance.Has(prefix)
            || markings.Count == 0
            || markings[0] is not MappingDataNode marking
            || !marking.TryGet<ValueDataNode>("markingId", out var id))
            return true;

        appearance[prefix] = id.Copy();

        if (marking.TryGet<SequenceDataNode>("markingColor", out var colors) && colors.Count > 0)
            appearance[$"{prefix}Color"] = colors[0].Copy();

        if (marking.TryGet<MappingDataNode>("gradient", out var gradient))
            appearance[$"{prefix}Gradient"] = gradient.Copy();

        return true;
    }

    /// <summary>
    /// Nebula keeps flavor texts in a nested <c>detailExaminableData</c>, here they are fields of the profile itself.
    /// </summary>
    private static void HoistDetailExaminableData(MappingDataNode profile)
    {
        if (!profile.TryGet<MappingDataNode>("detailExaminableData", out var data))
            return;

        profile.Remove("detailExaminableData");
        foreach (var (key, value) in data)
        {
            profile.TryAdd(key, value.Copy());
        }
    }
}
