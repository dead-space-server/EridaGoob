// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface;
using Content.Client.Lobby;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida.Lobby.UI;

public sealed partial class EridaLobby
{
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private EridaThemeManager _theme = default!;

    private const int PopupJobsPerPriority = 6;

    private static readonly JobPriority[] ShownPriorities = [JobPriority.High, JobPriority.Medium, JobPriority.Low];

    private SharedJobSystem? _jobs;
    private PlayerPreferences? _shownPreferences;

    private void InitBarInfo()
    {
        PlayersIcon.Texture = EridaIcons.Get("group");
        RoleIcon.AddStyleClass(EridaStyleClass.BarIcon);

        EridaBarPopup.Attach(PlayersGroup, BuildPlayersPopup);
        EridaBarPopup.Attach(RoleGroup, BuildRolePopup);
    }

    private void UpdatePlayers()
    {
        if (_gameTicker is not { } ticker)
            return;

        var started = ticker.IsGameStarted;

        PlayersLabel.Text = ticker.LobbyPlayerCount.ToString();
        PlayersStatusIcon.Texture = EridaIcons.Get(started ? "rocket_launch" : "check_circle");
        PlayersStatusLabel.Text = (started ? ticker.LobbyInGameCount : ticker.LobbyReadyCount).ToString();
    }

    private EridaBarPopup BuildPlayersPopup()
    {
        var popup = new EridaBarPopup();
        popup.AddHeader(Loc.GetString("erida-lobby-players-popup-title"));

        if (_gameTicker is not { } ticker)
            return popup;

        var online = ticker.LobbyPlayerCount;
        var inGame = ticker.LobbyInGameCount;

        popup.AddRow(EridaIcons.Get("group"), Loc.GetString("erida-lobby-players-online"), online.ToString());
        popup.AddRow(EridaIcons.Get("hourglass_top"), Loc.GetString("erida-lobby-players-in-lobby"), Math.Max(0, online - inGame).ToString());

        if (ticker.IsGameStarted)
            popup.AddRow(EridaIcons.Get("rocket_launch"), Loc.GetString("erida-lobby-players-in-game"), inGame.ToString());
        else
            popup.AddRow(EridaIcons.Get("check_circle"), Loc.GetString("erida-lobby-players-ready"), ticker.LobbyReadyCount.ToString());

        return popup;
    }

    private void UpdateRoleIfChanged()
    {
        if (!_preferences.ServerDataLoaded)
        {
            RoleGroup.Visible = false;
            return;
        }

        var preferences = _preferences.Preferences;

        // Preferences are replaced on every change, comparing references is enough.
        if (ReferenceEquals(preferences, _shownPreferences))
            return;

        _shownPreferences = preferences;
        RoleGroup.Visible = true;

        if (GetSelectedProfile() is not { } profile || !TryGetTopJob(profile, out var job))
        {
            RoleIcon.Texture = GetDepartmentIcon(null);
            RoleIcon.ModulateSelfOverride = null;
            RoleLabel.Text = Loc.GetString("erida-lobby-role-none");
            return;
        }

        var department = GetDepartment(job);
        RoleIcon.Texture = GetDepartmentIcon(department);
        RoleIcon.ModulateSelfOverride = GetDepartmentColor(department);
        RoleLabel.Text = job.LocalizedName;
    }

    private EridaBarPopup BuildRolePopup()
    {
        var popup = new EridaBarPopup();
        popup.AddHeader(Loc.GetString("erida-lobby-role-popup-title"));

        if (GetSelectedProfile() is not { } profile)
        {
            popup.AddRow(null, Loc.GetString("erida-lobby-role-none-hint"), muted: true);
            return popup;
        }

        var any = false;

        foreach (var priority in ShownPriorities)
        {
            var jobs = GetJobs(profile, priority);

            if (jobs.Count == 0)
                continue;

            any = true;
            popup.AddRow(null, Loc.GetString(GetPriorityLocId(priority)), muted: true);

            foreach (var job in jobs.Take(PopupJobsPerPriority))
            {
                var department = GetDepartment(job);
                popup.AddRow(GetDepartmentIcon(department), job.LocalizedName, iconColor: GetDepartmentColor(department));
            }

            if (jobs.Count > PopupJobsPerPriority)
                popup.AddRow(null, Loc.GetString("erida-lobby-role-more", ("count", jobs.Count - PopupJobsPerPriority)), muted: true);
        }

        if (!any)
            popup.AddRow(null, Loc.GetString("erida-lobby-role-none-hint"), muted: true);

        return popup;
    }

    private static string GetPriorityLocId(JobPriority priority)
    {
        return priority switch
        {
            JobPriority.High => "erida-lobby-role-priority-high",
            JobPriority.Medium => "erida-lobby-role-priority-medium",
            _ => "erida-lobby-role-priority-low",
        };
    }

    private HumanoidCharacterProfile? GetSelectedProfile()
    {
        if (!_preferences.ServerDataLoaded || _preferences.Preferences is not { } preferences)
            return null;

        return preferences.Characters.GetValueOrDefault(preferences.SelectedCharacterIndex) as HumanoidCharacterProfile;
    }

    private bool TryGetTopJob(HumanoidCharacterProfile profile, [NotNullWhen(true)] out JobPrototype? job)
    {
        foreach (var priority in ShownPriorities)
        {
            job = GetJobs(profile, priority).FirstOrDefault();

            if (job != null)
                return true;
        }

        job = null;
        return false;
    }

    private List<JobPrototype> GetJobs(HumanoidCharacterProfile profile, JobPriority priority)
    {
        List<JobPrototype> jobs = [];

        foreach (var (id, jobPriority) in profile.JobPriorities)
        {
            if (jobPriority == priority && _prototypes.TryIndex(id, out var job))
                jobs.Add(job);
        }

        jobs.Sort((a, b) => string.Compare(a.LocalizedName, b.LocalizedName, StringComparison.CurrentCulture));
        return jobs;
    }

    private DepartmentPrototype? GetDepartment(JobPrototype job)
    {
        return _jobs != null && _jobs.TryGetPrimaryDepartment(job.ID, out var department) ? department : null;
    }

    private Texture GetDepartmentIcon(DepartmentPrototype? department)
    {
        return EridaDepartments.GetIcon(department);
    }

    private Color? GetDepartmentColor(DepartmentPrototype? department)
    {
        return EridaDepartments.GetColor(_theme.Palette, department);
    }
}
