// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

namespace Content.Shared.Access;

/// <summary>
/// Raised on a renamed character before automatically updating their ID-card, PDA, or station registration.
/// Cancelling this leaves the equipment's registered identity unchanged, without cancelling the character rename.
/// </summary>
[ByRefEvent]
public record struct AutoRenameIdentityAttemptEvent
{
    public bool Cancelled;
}
