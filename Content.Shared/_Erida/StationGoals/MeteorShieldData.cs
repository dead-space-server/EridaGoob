// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.StationGoals;

[Serializable, NetSerializable]
public enum MeteorShieldVisuals : byte
{
    Active,
}

[Serializable, NetSerializable]
public sealed partial class MeteorShieldToggleDoAfterEvent : SimpleDoAfterEvent;
