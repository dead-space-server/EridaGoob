// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Erida.Roles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Alternate title picked for each job, jobs without an entry use their own name.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> JobAlternateTitles { get; set; } = [];

    public HumanoidCharacterProfile WithJobAltTitle(ProtoId<JobPrototype> job, ProtoId<JobAlternateTitlePrototype>? title)
    {
        var titles = new Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>>(JobAlternateTitles);

        if (title is { } value)
            titles[job] = value;
        else
            titles.Remove(job);

        return new(this) { JobAlternateTitles = titles };
    }

    public bool TryGetJobAltTitle(
        ProtoId<JobPrototype> job,
        IPrototypeManager prototypes,
        [NotNullWhen(true)] out JobAlternateTitlePrototype? title)
    {
        title = null;
        return JobAlternateTitles.TryGetValue(job, out var id)
               && prototypes.TryIndex(id, out title)
               && title.Job == job;
    }

    private bool JobAltTitlesEqual(HumanoidCharacterProfile other)
    {
        if (JobAlternateTitles.Count != other.JobAlternateTitles.Count)
            return false;

        foreach (var (job, title) in JobAlternateTitles)
        {
            if (!other.JobAlternateTitles.TryGetValue(job, out var otherTitle) || otherTitle != title)
                return false;
        }

        return true;
    }

    private void ValidateJobAltTitles(IPrototypeManager prototypes)
    {
        foreach (var job in JobAlternateTitles.Keys)
        {
            if (!TryGetJobAltTitle(job, prototypes, out _))
                JobAlternateTitles.Remove(job);
        }
    }
}
