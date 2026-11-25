// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared._Erida.SplitPersonality;

/// <summary>Acquired trauma belongs to the physical brain, never the character profile.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SplitPersonalityTraumaComponent : Component;

/// <summary>Links an occupied body to its affected brain, or the inactive participant to that body.</summary>
/// <remarks>The client only needs to know that its own entity has the channel, so the brain is not networked.</remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class PersonalityChannelComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField]
    public EntityUid Brain;
}

/// <summary>A stationary, non-physical viewpoint. It has no ghost privileges or movement.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InactivePersonalityComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField]
    public EntityUid? Body;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MendPersonalityConditionComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class MendPersonalityStepComponent : Component;
