// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Erida.ExamineNotice;

/// <summary>
/// Shows a popup to this entity when another living mob examines it.
/// </summary>
[RegisterComponent]
public sealed partial class ExamineNoticeComponent : Component
{
    /// <summary>
    /// Max distance to the examiner at which the notice is shown.
    /// </summary>
    [DataField(readOnly: true)]
    public float Range = 5f;

    /// <summary>
    /// Minimal time between notices from the same examiner.
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Arc behind this entity in which examiners go unnoticed.
    /// </summary>
    [DataField(readOnly: true)]
    public Angle BackArc = Angle.FromDegrees(90);

    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> NextNotice = [];
}
