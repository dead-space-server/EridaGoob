// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Erida.Maps;

namespace Content.Server.Power.EntitySystems;

public sealed partial class CableSystem
{
    [Dependency] private readonly GridSplitAnchorSystem _gridSplitAnchor = default!;
}
