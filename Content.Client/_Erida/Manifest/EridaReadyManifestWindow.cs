// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface.Controls;
using Content.Shared._Erida.ReadyManifest;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida.Manifest;

/// <summary>
/// Jobs the ready players want, by department, with each player's character for admins.
/// </summary>
public sealed partial class EridaReadyManifestWindow : EridaWindow
{
    [Dependency] private IEntitySystemManager _entitySystems = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly Label _summary;
    private readonly EridaManifestListing _listing = new();

    public EridaReadyManifestWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("erida-ready-manifest-title");
        SetSize = new Vector2(460, 640);
        MinSize = new Vector2(340, 300);

        _summary = new Label { Margin = new Thickness(6, 0) };
        _summary.AddStyleClass(EridaStyleClass.TextMuted);

        Contents.AddChild(_summary);
        Contents.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _listing },
        });
    }

    public void Populate(ReadyManifestEuiState state)
    {
        _listing.Clear();
        _summary.Text = Loc.GetString("erida-ready-manifest-summary", ("count", state.ReadyCount));

        var byJob = state.Entries
            .GroupBy(entry => entry.Job)
            .ToDictionary(group => group.Key, group => group.OrderBy(entry => entry.CharacterName).ToList());

        if (byJob.Count == 0)
        {
            _listing.AddMessage(Loc.GetString("erida-ready-manifest-empty"));
            return;
        }

        var jobSystem = _entitySystems.GetEntitySystem<SharedJobSystem>();

        // Heads are in Command and their own department, list each job once under its primary one.
        Dictionary<ProtoId<DepartmentPrototype>, List<JobPrototype>> byDepartment = [];

        foreach (var jobId in byJob.Keys)
        {
            if (!_prototypes.TryIndex(jobId, out var job)
                || !jobSystem.TryGetPrimaryDepartment(jobId, out var department))
            {
                continue;
            }

            if (!byDepartment.TryGetValue(department.ID, out var jobs))
                byDepartment[department.ID] = jobs = [];

            jobs.Add(job);
        }

        var departments = _prototypes.EnumeratePrototypes<DepartmentPrototype>()
            .Where(department => byDepartment.ContainsKey(department.ID))
            .ToList();

        departments.Sort(DepartmentUIComparer.Instance);

        foreach (var department in departments)
        {
            var jobs = byDepartment[department.ID];

            jobs.Sort(JobUIComparer.Instance);

            var section = _listing.AddDepartment(department);

            foreach (var job in jobs)
            {
                var players = byJob[job.ID];
                _listing.AddJob(section, job, players.Count.ToString());

                foreach (var player in players)
                {
                    if (player.CharacterName is { } name)
                        _listing.AddPerson(section, name, null);
                }
            }
        }
    }
}
