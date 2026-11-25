// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Beam;
using Content.Server.Mining;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared._Erida.StationGoals;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Erida.StationGoals.MeteorShield;

/// <summary>
/// Destroys meteors in line of sight of working satellites and evaluates the coverage goal condition.
/// </summary>
public sealed class MeteorShieldSystem : SharedMeteorShieldSystem
{
    [Dependency] private BeamSystem _beam = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(0.2);

    private const int CoverageDirections = 180;

    private const int CoverageShifts = 8;

    /// <summary>
    /// Angular sectors in which the distance a satellite sees before a wall is measured.
    /// </summary>
    private const int VisionSectors = 72;

    /// <summary>
    /// Points checked for line of sight on the part of a meteor path inside the satellite range.
    /// </summary>
    private const int PathSamples = 5;

    /// <summary>
    /// Path points closer than this lie on the satellite own tile, a meteor there has already hit the satellite grid.
    /// </summary>
    private const float SatelliteClearance = 1f;

    /// <summary>
    /// How long the coverage shown on examine is reused, so examining satellites does not redo the raycasts.
    /// </summary>
    private static readonly TimeSpan CoverageCacheTime = TimeSpan.FromSeconds(5);

    private readonly HashSet<Entity<MeteorComponent>> _meteors = [];
    private readonly HashSet<EntityUid> _destroyed = [];
    private readonly List<CoverageShield> _coverageShields = [];
    private readonly List<RayCastResults> _rayResults = [];
    private readonly Dictionary<EntityUid, (TimeSpan Until, float Coverage)> _coverageCache = [];
    private TimeSpan _nextScan;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeteorShieldComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MeteorShieldCoverageConditionComponent, StationGoalGetProgressEvent>(OnCoverageProgress);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextScan)
            return;

        _nextScan = _timing.CurTime + ScanInterval;

        var meteorQuery = EntityQueryEnumerator<MeteorComponent>();
        if (!meteorQuery.MoveNext(out _, out _))
            return;

        _destroyed.Clear();
        var query = EntityQueryEnumerator<MeteorShieldComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shield, out var xform))
        {
            if (!IsWorking((uid, shield), xform))
                continue;

            var origin = _transform.GetWorldPosition(xform);
            _meteors.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, shield.Range, _meteors, LookupFlags.Dynamic);
            foreach (var meteor in _meteors)
            {
                var meteorXform = Transform(meteor);
                if (_destroyed.Contains(meteor)
                    || meteorXform.GridUid != null
                    || meteorXform.MapID != xform.MapID
                    || !InLineOfSight(xform.MapID, origin, _transform.GetWorldPosition(meteorXform)))
                    continue;

                _destroyed.Add(meteor);
                _beam.TryCreateBeam(uid, meteor, shield.BeamPrototype, accumulateIndex: false);
                QueueDel(meteor);
            }
        }
    }

    #region Handle events

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _nextScan = TimeSpan.Zero;
        _destroyed.Clear();
        _coverageCache.Clear();
    }

    #endregion

    #region Component events

    private void OnExamined(Entity<MeteorShieldComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(MeteorShieldComponent)))
        {
            var key = IsWorking(ent, Transform(ent))
                ? "meteor-shield-examine-active"
                : ent.Comp.Active
                    ? "meteor-shield-examine-unpowered"
                    : "meteor-shield-examine-inactive";
            args.PushMarkup(Loc.GetString(key));

            var mapId = Transform(ent).MapID;
            var query = EntityQueryEnumerator<StationGoalComponent>();
            while (query.MoveNext(out var station, out var stationGoal))
            {
                if (!TryComp<MeteorShieldCoverageConditionComponent>(stationGoal.Goal, out var condition)
                    || _station.GetLargestGrid(station) is not { } grid
                    || Transform(grid).MapID != mapId)
                    continue;

                args.PushMarkup(Loc.GetString("meteor-shield-examine-coverage",
                    ("station", MetaData(station).EntityName),
                    ("current", (int) MathF.Floor(GetCachedCoverage(station) * 100f)),
                    ("required", (int) MathF.Round(condition.Coverage * 100f))));
            }
        }
    }

    private void OnCoverageProgress(Entity<MeteorShieldCoverageConditionComponent> ent, ref StationGoalGetProgressEvent args)
    {
        args.Add(ent.Comp.Coverage <= 0f ? 1f : GetCoverage(args.Station) / ent.Comp.Coverage);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// <see cref="GetCoverage"/> reused for <see cref="CoverageCacheTime"/>, for frequent callers like examine.
    /// </summary>
    private float GetCachedCoverage(EntityUid station)
    {
        var now = _timing.CurTime;
        if (_coverageCache.TryGetValue(station, out var cached) && cached.Until > now)
            return cached.Coverage;

        var coverage = GetCoverage(station);
        _coverageCache[station] = (now + CoverageCacheTime, coverage);
        return coverage;
    }

    /// <summary>
    /// Share of meteors flying towards the station largest grid that come into line of sight of a working satellite
    /// before hitting the grid, from 0 to 1. Paths are sampled over spawn directions and sideways shifts,
    /// spawn distance along each path is accounted exactly.
    /// </summary>
    public float GetCoverage(EntityUid station)
    {
        if (_station.GetLargestGrid(station) is not { } grid)
            return 0f;

        var mapId = Transform(grid).MapID;
        var bounds = _physics.GetWorldAABB(grid);
        var center = bounds.Center;
        var radius = (bounds.TopRight - center).Length();
        var minSpawn = radius + MeteorSwarmSystem.SpawnMargin;
        var maxShift = radius * MeteorSwarmSystem.SpawnSideShift;

        _coverageShields.Clear();
        var query = EntityQueryEnumerator<MeteorShieldComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shield, out var xform))
        {
            if (xform.MapID != mapId || !IsWorking((uid, shield), xform))
                continue;

            var position = _transform.GetWorldPosition(xform);
            _coverageShields.Add(new CoverageShield(position - center, shield.Range, GetVisionDistances(mapId, position, shield.Range)));
        }

        if (_coverageShields.Count == 0)
            return 0f;

        var paths = 0;
        var covered = 0f;
        for (var i = 0; i < CoverageDirections; i++)
        {
            var angle = MathF.Tau * i / CoverageDirections;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var side = new Vector2(-direction.Y, direction.X);

            for (var j = 0; j < CoverageShifts; j++)
            {
                var shift = maxShift * ((2f * j + 1f) / CoverageShifts - 1f);
                if (!TryGetHullDistance(bounds, center + side * shift, direction, out var hull))
                    continue;

                paths++;
                covered += GetInterceptedShare(direction, side, shift, hull, minSpawn);
            }
        }

        return paths == 0 ? 0f : covered / paths;
    }

    /// <summary>
    /// Share of meteors coming along <paramref name="direction"/> that come into line of sight of a satellite
    /// between their spawn point and the grid hull. Distances are measured along the path from the station centre line.
    /// A meteor is caught if it spawns beyond the nearest visible point of a satellite range that is still outside the hull.
    /// </summary>
    private float GetInterceptedShare(Vector2 direction, Vector2 side, float shift, float hull, float minSpawn)
    {
        var entry = float.PositiveInfinity;
        foreach (var shield in _coverageShields)
        {
            var offset = Vector2.Dot(shield.Position, side) - shift;
            if (MathF.Abs(offset) >= shield.Range)
                continue;

            var along = Vector2.Dot(shield.Position, direction);
            var half = MathF.Sqrt(shield.Range * shield.Range - offset * offset);
            var far = along + half;
            if (far <= hull)
                continue;

            var near = MathF.Max(along - half, hull);
            for (var k = 0; k < PathSamples; k++)
            {
                var distance = near + (far - near) * k / (PathSamples - 1);
                if (distance >= entry)
                    break;

                if (!shield.Sees(side * shift + direction * distance))
                    continue;

                entry = distance;
                break;
            }
        }

        return Math.Clamp((minSpawn + MeteorSwarmSystem.SpawnDepth - Math.Max(entry, minSpawn)) / MeteorSwarmSystem.SpawnDepth, 0f, 1f);
    }

    /// <summary>
    /// Distance a satellite sees in each of <see cref="VisionSectors"/> before the nearest occluder, at most its range.
    /// </summary>
    private float[] GetVisionDistances(MapId mapId, Vector2 origin, float range)
    {
        var distances = new float[VisionSectors];
        for (var i = 0; i < VisionSectors; i++)
        {
            var angle = MathF.Tau * (i + 0.5f) / VisionSectors;
            var ray = new Ray(origin, new Vector2(MathF.Cos(angle), MathF.Sin(angle)));
            _occluder.IntersectRay(_rayResults, mapId, ray, range);

            var distance = range;
            foreach (var result in _rayResults)
            {
                distance = MathF.Min(distance, result.Distance);
            }

            distances[i] = distance;
        }

        return distances;
    }

    private bool InLineOfSight(MapId mapId, Vector2 origin, Vector2 target)
    {
        var delta = target - origin;
        var length = delta.Length();
        return length < 0.01f || _occluder.IntersectRay(mapId, new Ray(origin, delta / length), length) == null;
    }

    /// <summary>
    /// Farthest point of the line <c>origin + t * direction</c> inside the bounds, where a meteor coming from far along it hits the grid.
    /// </summary>
    private static bool TryGetHullDistance(Box2 bounds, Vector2 origin, Vector2 direction, out float distance)
    {
        var min = float.NegativeInfinity;
        var max = float.PositiveInfinity;
        distance = 0f;

        if (!ClipSlab(origin.X, direction.X, bounds.Left, bounds.Right, ref min, ref max)
            || !ClipSlab(origin.Y, direction.Y, bounds.Bottom, bounds.Top, ref min, ref max))
            return false;

        distance = max;
        return true;
    }

    private static bool ClipSlab(float origin, float direction, float low, float high, ref float min, ref float max)
    {
        if (MathF.Abs(direction) < 1e-6f)
            return origin >= low && origin <= high;

        var t1 = (low - origin) / direction;
        var t2 = (high - origin) / direction;
        if (t1 > t2)
            (t1, t2) = (t2, t1);

        min = MathF.Max(min, t1);
        max = MathF.Min(max, t2);
        return min <= max;
    }

    #endregion

    /// <summary>
    /// Working satellite for the coverage, position is relative to the station centre.
    /// </summary>
    private readonly record struct CoverageShield(Vector2 Position, float Range, float[] Vision)
    {
        public bool Sees(Vector2 point)
        {
            var offset = point - Position;
            var distance = offset.Length();
            if (distance < SatelliteClearance)
                return false;

            var angle = MathF.Atan2(offset.Y, offset.X);
            if (angle < 0f)
                angle += MathF.Tau;

            var sector = (int) (angle / MathF.Tau * VisionSectors) % VisionSectors;
            return distance <= Vision[sector];
        }
    }
}
