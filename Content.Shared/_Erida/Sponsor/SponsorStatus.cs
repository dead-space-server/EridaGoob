// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Erida.Sponsor;

public sealed class SponsorStatus
{
    public int Rank { get; set; }
    public DateTimeOffset StartedAt { get; set; }

    // The bot may still send the rank as "level".
    public int Level
    {
        get => Rank;
        set => Rank = value;
    }
}
