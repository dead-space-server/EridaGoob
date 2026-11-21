// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;

namespace Content.Server._Erida.SplitPersonality;

/// <summary>Round-local snapshots. Both personalities inhabit the same entity and mind.</summary>
[RegisterComponent]
public sealed partial class SplitPersonalityComponent : Component
{
    public HumanoidCharacterProfile Primary = default!;
    public HumanoidCharacterProfile Alternate = default!;
    public MarkingSet PrimaryColors = new();
    public MarkingSet AlternateColors = new();
    public bool AlternateActive;
    public EntityUid? SwitchAction;
}
