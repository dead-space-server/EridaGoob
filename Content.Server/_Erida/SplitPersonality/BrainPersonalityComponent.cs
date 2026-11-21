// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

namespace Content.Server._Erida.SplitPersonality;

/// <summary>Runtime state on the brain. Primary/secondary are slots, not immutable player identities.</summary>
[RegisterComponent]
public sealed partial class BrainPersonalityComponent : Component
{
    public bool Rolled;
    public EntityUid? Primary;
    public EntityUid? Secondary;
    public EntityUid? Observer;
    public EntityUid? Host;
    public bool SecondaryActive;
    public bool Transferring;
    public float Remaining;
}

/// <summary>The waiting role is distinct from the observer occupied by the inactive participant.</summary>
[RegisterComponent]
public sealed partial class PersonalityRoleComponent : Component
{
    [DataField] public EntityUid Brain;
}
