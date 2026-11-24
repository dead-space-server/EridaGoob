// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Content.Shared._Erida.Roles;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    /// <summary>
    /// Stores the alternate job titles as a JSON object of job id to title id.
    /// </summary>
    private static string SerializeJobAltTitles(HumanoidCharacterProfile humanoid)
    {
        var raw = new Dictionary<string, string>(humanoid.JobAlternateTitles.Count);
        foreach (var (job, title) in humanoid.JobAlternateTitles)
        {
            raw[job.Id] = title.Id;
        }

        return JsonSerializer.Serialize(raw);
    }

    /// <summary>
    /// Reads the alternate job titles stored as a JSON object, broken or empty data gives no titles.
    /// </summary>
    private static Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> ParseJobAltTitles(Profile profile)
    {
        Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> titles = [];
        if (string.IsNullOrEmpty(profile.JobAlternateTitles))
            return titles;

        Dictionary<string, string>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<Dictionary<string, string>>(profile.JobAlternateTitles);
        }
        catch (JsonException)
        {
            return titles;
        }

        if (raw == null)
            return titles;

        foreach (var (job, title) in raw)
        {
            titles[job] = title;
        }

        return titles;
    }
}
