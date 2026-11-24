// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.Roles;

/// <summary>
/// Cosmetic title a player can pick for a job, shown on the ID card, in station records and in announcements.
/// </summary>
[Prototype]
public sealed partial class JobAlternateTitlePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The job this title belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    /// <summary>
    /// Locale key of the title, receives <c>$gender</c> to pick gendered forms.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    public string LocalizedName(Gender gender)
    {
        return Loc.GetString(Name, ("gender", gender));
    }
}
