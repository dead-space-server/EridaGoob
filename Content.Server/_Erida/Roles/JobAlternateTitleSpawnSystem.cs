// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Shared._Erida.Roles;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.Roles;

/// <summary>
/// Puts the alternate job title from the profile on the spawned player's ID card and station record.
/// </summary>
public sealed partial class JobAlternateTitleSpawnSystem : EntitySystem
{
    [Dependency] private JobAlternateTitleSystem _titles = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StationRecordsSystem _records = default!;

    private EntityQuery<StationRecordKeyStorageComponent> _keyStorageQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(StationRecordsSystem)]);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRulePlayerJobsAssigned, after: [typeof(PresetIdCardSystem)]);

        _keyStorageQuery = GetEntityQuery<StationRecordKeyStorageComponent>();
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (string.IsNullOrEmpty(args.JobId))
            return;

        ApplyTitle(args.Mob, args.Profile, args.JobId);
    }

    /// <summary>
    /// On extended access stations the preset ID cards reset their job title after the round start spawns.
    /// </summary>
    private void OnRulePlayerJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        foreach (var session in args.Players)
        {
            if (session.AttachedEntity is not { } mob
                || !args.Profiles.TryGetValue(session.UserId, out var profile)
                || !_mind.TryGetMind(session.UserId, out var mind)
                || !_jobs.MindTryGetJobId(mind.Value.Owner, out var job)
                || job is not { } jobId)
            {
                continue;
            }

            ApplyTitle(mob, profile, jobId);
        }
    }

    private void ApplyTitle(EntityUid mob, HumanoidCharacterProfile profile, ProtoId<JobPrototype> job)
    {
        if (!_titles.TryGetTitle(profile, job, out var title)
            || !_idCard.TryFindIdCard(mob, out var card))
            return;

        var name = title.LocalizedName(profile.Gender);
        _idCard.TryChangeJobTitle(card, name, card.Comp);

        if (!_keyStorageQuery.TryComp(card, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        record.JobTitle = name;
        _records.Synchronize(key);
    }
}
