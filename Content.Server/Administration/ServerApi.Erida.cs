// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Content.Server._Erida.Discord;
using Content.Server._Erida.Discord.Linking;
using Content.Server._Erida.Players;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.ServerStatus;
using Robust.Shared.Network;

namespace Content.Server.Administration;

public sealed partial class ServerApi
{
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly PlayTimeEditManager _playTimeEdit = default!;
    [Dependency] private readonly EridaWebhooks _eridaWebhooks = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private void InitializeEridaPart()
    {
        RegisterHandler(HttpMethod.Post, "/linking/update", RequestUpdateDiscordLinkingData);

        RegisterHandler(HttpMethod.Get, "/admin/playtime/trackers", GetPlayTimeTrackers);
        RegisterHandler(HttpMethod.Get, "/admin/playtime", GetPlayTime);
        RegisterActorHandler(HttpMethod.Post, "/admin/actions/playtime/add", ActionAddPlayTime);
    }

    private async Task RequestUpdateDiscordLinkingData(IStatusHandlerContext context)
    {
        var body = await ReadJson<UpdateLinkingDataBody>(context);
        if (body == null)
            return;

        await RunOnMainThread(async () =>
        {
            if (!_playerManager.TryGetSessionById(new NetUserId(body.Guid), out var player))
            {
                await RespondError(
                    context,
                    ErrorCode.PlayerNotFound,
                    HttpStatusCode.UnprocessableContent,
                    "Player not found");
                return;
            }

            var discordLinking = _entitySystemManager.GetEntitySystem<DiscordLinkingSystem>();

            discordLinking.ForceUpdateUserData(player.UserId);

            await RespondOk(context);

            _sawmill.Info($"Forced discord linking update for {player.Name}");
        });
    }

    /// <summary>
    /// Lists play time trackers with the names of the jobs that use them, and departments with their trackers.
    /// </summary>
    private async Task GetPlayTimeTrackers(IStatusHandlerContext context)
    {
        var response = await RunOnMainThread(() =>
        {
            Dictionary<string, string> names = [];
            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
                names.TryAdd(job.PlayTimeTracker, job.LocalizedName);

            var trackers = _prototypeManager.EnumeratePrototypes<PlayTimeTrackerPrototype>()
                .Select(t => new PlayTimeTrackerInfo(t.ID, names.GetValueOrDefault(t.ID, t.ID)))
                .OrderBy(t => t.Id != PlayTimeTrackingShared.TrackerOverall)
                .ThenBy(t => t.Name)
                .ToList();

            var departments = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>()
                .Where(d => !d.EditorHidden)
                .Select(d => new PlayTimeDepartmentInfo(
                    d.ID,
                    Loc.GetString(d.Name),
                    d.Roles
                        .Where(r => _prototypeManager.HasIndex(r))
                        .Select(r => _prototypeManager.Index(r).PlayTimeTracker.Id)
                        .Where(t => _prototypeManager.HasIndex<PlayTimeTrackerPrototype>(t))
                        .Distinct()
                        .ToList()))
                .Where(d => d.Trackers.Count > 0)
                .OrderBy(d => d.Name)
                .ToList();

            return new PlayTimeTrackersResponse(trackers, departments);
        });

        await context.RespondJsonAsync(response);
    }

    /// <summary>
    /// Play time of a player by name or GUID: <c>/admin/playtime?user=...</c>.
    /// </summary>
    private async Task GetPlayTime(IStatusHandlerContext context)
    {
        var user = HttpUtility.ParseQueryString(context.Url.Query)["user"];
        if (string.IsNullOrWhiteSpace(user))
        {
            await RespondBadRequest(context, "Query parameter 'user' is required");
            return;
        }

        if (await _playerLocator.LookupIdByNameOrIdAsync(user) is not { } located)
        {
            await RespondPlayerNotFound(context);
            return;
        }

        await RunOnMainThread(async () =>
        {
            var online = _playerManager.TryGetSessionById(located.UserId, out _);
            if (await _playTimeEdit.GetTimes(located.UserId) is not { } times)
            {
                await RespondPlayTimeLoading(context);
                return;
            }

            await context.RespondJsonAsync(new PlayTimeResponse(
                located.UserId.UserId,
                located.Username,
                online,
                ToMinutes(times)));
        });
    }

    /// <summary>
    /// Adds minutes to the given trackers of a player, online or not.
    /// </summary>
    private async Task ActionAddPlayTime(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadJson<AddPlayTimeActionBody>(context);
        if (body == null)
            return;

        if (body.Minutes.Count == 0)
        {
            await RespondBadRequest(context, "No trackers given");
            return;
        }

        if (await _playerLocator.LookupIdByNameOrIdAsync(body.User) is not { } located)
        {
            await RespondPlayerNotFound(context);
            return;
        }

        await RunOnMainThread(async () =>
        {
            var invalid = body.Minutes.Keys.Where(t => !_playTimeEdit.IsValidTracker(t)).ToList();
            if (invalid.Count > 0)
            {
                await RespondBadRequest(context, $"Unknown trackers: {string.Join(", ", invalid)}");
                return;
            }

            var requested = body.Minutes.ToDictionary(t => t.Key, t => TimeSpan.FromMinutes(t.Value));
            if (await _playTimeEdit.AddTime(located.UserId, requested) is not { } edit)
            {
                await RespondPlayTimeLoading(context);
                return;
            }

            var changes = string.Join(", ", edit.Changes.Select(t => $"{t.Key} {(long) t.Value.TotalMinutes:+#;-#;0}m"));
            var reason = string.IsNullOrWhiteSpace(body.Reason) ? string.Empty : $": {body.Reason}";

            _sawmill.Info($"Added play time to {located.Username} ({located.UserId}) by {FormatLogActor(actor)}: {changes}");
            _adminLog.Add(LogType.AdminCommands,
                LogImpact.High,
                $"{FormatLogActor(actor)} added play time to {located.Username} ({located.UserId}) via API: {changes}{reason}");
            _eridaWebhooks.SendTimeChangedMessage(new NetUserId(actor.Guid),
                located.UserId,
                edit.Changes.Where(t => t.Value != TimeSpan.Zero).ToDictionary(),
                adminName: actor.Name,
                targetName: located.Username);

            await context.RespondJsonAsync(new PlayTimeResponse(
                located.UserId.UserId,
                located.Username,
                _playerManager.TryGetSessionById(located.UserId, out _),
                ToMinutes(edit.Totals)));
        });
    }

    private static Dictionary<string, long> ToMinutes(Dictionary<string, TimeSpan> times)
    {
        return times.ToDictionary(t => t.Key, t => (long) t.Value.TotalMinutes);
    }

    private static Task RespondPlayerNotFound(IStatusHandlerContext context)
    {
        return RespondError(context, ErrorCode.PlayerNotFound, HttpStatusCode.UnprocessableContent, "Player not found");
    }

    private static Task RespondPlayTimeLoading(IStatusHandlerContext context)
    {
        return RespondError(context,
            ErrorCode.PlayTimeNotLoaded,
            HttpStatusCode.Conflict,
            "Player is connecting, play time is not loaded yet");
    }

    private sealed class UpdateLinkingDataBody
    {
        public required Guid Guid { get; init; }
    }

    private sealed class AddPlayTimeActionBody
    {
        /// <summary>
        /// Player name or GUID.
        /// </summary>
        public required string User { get; init; }

        /// <summary>
        /// Minutes to add per tracker. Negative values remove time.
        /// </summary>
        public required Dictionary<string, int> Minutes { get; init; }

        public string? Reason { get; init; }
    }

    private sealed record PlayTimeTrackerInfo(string Id, string Name);

    private sealed record PlayTimeDepartmentInfo(string Id, string Name, List<string> Trackers);

    private sealed record PlayTimeTrackersResponse(
        List<PlayTimeTrackerInfo> Trackers,
        List<PlayTimeDepartmentInfo> Departments);

    private sealed record PlayTimeResponse(Guid UserId, string UserName, bool Online, Dictionary<string, long> Minutes);
}
