// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Erida.Silicons.StationAi;

/// <summary>
/// Grid with an active AI remote core. The station AI eye on this grid sees and interacts here
/// as on its own grid, still limited by camera vision of the target tile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AiRemoteGridSystem))]
public sealed partial class AiRemoteGridComponent : Component;
