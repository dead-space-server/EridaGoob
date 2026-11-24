// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;

namespace Content.Server.Players.PlayTimeTracking;

public sealed partial class PlayTimeTrackingManager
{
    /// <summary>
    /// Completes when every save started so far has reached the database.
    /// </summary>
    public Task WaitPendingSaves()
    {
        return Task.WhenAll(_pendingSaveTasks);
    }
}
