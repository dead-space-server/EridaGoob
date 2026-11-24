// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.InteractionVerbs;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.SocialInteractions;

/// <summary>
/// Requires the target to have at least one marking in the given category, e.g. a tail.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HasMarkingCategoryRequirement : InteractionRequirement
{
    [DataField(required: true)]
    public MarkingCategories Category;

    public override bool IsMet(InteractionArgs args, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        return deps.EntMan.TryGetComponent<HumanoidAppearanceComponent>(args.Target, out var humanoid)
            && humanoid.MarkingSet.TryGetCategory(Category, out var markings)
            && markings.Count > 0;
    }
}
