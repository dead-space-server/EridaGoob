// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Erida.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.Roles;

/// <summary>
/// Caches alternate titles per job and resolves the title a profile picked.
/// </summary>
public sealed partial class JobAlternateTitleSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly Dictionary<ProtoId<JobPrototype>, JobAlternateTitlePrototype[]> _titles = [];

    public bool Enabled { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        Subs.CVar(_cfg, ECCVars.AlternateJobTitlesEnabled, value => Enabled = value, true);
        CacheTitles();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<JobAlternateTitlePrototype>())
            CacheTitles();
    }

    private void CacheTitles()
    {
        Dictionary<ProtoId<JobPrototype>, List<JobAlternateTitlePrototype>> grouped = [];
        foreach (var title in _prototypes.EnumeratePrototypes<JobAlternateTitlePrototype>())
        {
            if (!grouped.TryGetValue(title.Job, out var list))
                grouped[title.Job] = list = [];

            list.Add(title);
        }

        _titles.Clear();
        foreach (var (job, list) in grouped)
        {
            list.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID));
            _titles[job] = list.ToArray();
        }
    }

    /// <summary>
    /// Alternate titles available for the job, empty when it has none.
    /// </summary>
    public IReadOnlyList<JobAlternateTitlePrototype> GetTitles(ProtoId<JobPrototype> job)
    {
        return _titles.TryGetValue(job, out var titles) ? titles : [];
    }

    /// <summary>
    /// The title the profile picked for the job, if titles are enabled and the pick is valid.
    /// </summary>
    public bool TryGetTitle(
        HumanoidCharacterProfile profile,
        ProtoId<JobPrototype> job,
        [NotNullWhen(true)] out JobAlternateTitlePrototype? title)
    {
        title = null;
        return Enabled && profile.TryGetJobAltTitle(job, _prototypes, out title);
    }

    /// <summary>
    /// Localized alternate title of the job, or <paramref name="fallback"/> when there is none.
    /// </summary>
    public string GetJobName(HumanoidCharacterProfile profile, ProtoId<JobPrototype> job, string fallback)
    {
        return TryGetTitle(profile, job, out var title) ? title.LocalizedName(profile.Gender) : fallback;
    }
}
