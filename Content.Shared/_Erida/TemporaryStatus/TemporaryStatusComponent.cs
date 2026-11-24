// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Erida.TemporaryStatus;

/// <summary>
/// Lets the entity set a short status line shown on examine until it expires or the entity dies.
/// </summary>
[RegisterComponent]
public sealed partial class TemporaryStatusComponent : Component
{
    /// <summary>
    /// Quick dialog short text input rejects anything longer than 100 characters.
    /// </summary>
    [DataField(readOnly: true)]
    public int MaxLength = 100;

    [DataField(readOnly: true)]
    public TimeSpan Duration = TimeSpan.FromMinutes(20);

    [ViewVariables]
    public string? Text;

    [ViewVariables]
    public TimeSpan ExpiresAt;
}
