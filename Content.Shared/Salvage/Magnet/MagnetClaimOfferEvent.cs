// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.Magnet;

/// <summary>
/// Claim an offer from the magnet UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class MagnetClaimOfferEvent : BoundUserInterfaceMessage
{
    public int Index;
}

// Erida start
[Serializable, NetSerializable]
public sealed class MagnetCancelOfferEvent : BoundUserInterfaceMessage
{
    public int Index;
}
// Erida end
