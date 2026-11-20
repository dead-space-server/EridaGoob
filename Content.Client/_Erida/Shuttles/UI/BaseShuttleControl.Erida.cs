// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

public partial class BaseShuttleControl
{
    private static readonly TimeSpan GridRebuildDelay = TimeSpan.FromSeconds(0.5);
    private readonly List<(Vector2 Start, Vector2 End)> _mergedEdges = [];
    private readonly Dictionary<Vector2, int> _edgeStarts = [];
    private readonly Dictionary<Vector2, int> _edgeEnds = [];
    private int[] _nextByStart = Array.Empty<int>();
    private int[] _nextByEnd = Array.Empty<int>();
    private bool[] _edgeUsed = Array.Empty<bool>();

    /// <summary>
    /// Joins collinear edges of <see cref="_edges"/> into longer lines.
    /// </summary>
    private void MergeEdges()
    {
        var count = _edges.Count;
        Extensions.EnsureLength(ref _nextByStart, count);
        Extensions.EnsureLength(ref _nextByEnd, count);
        Extensions.EnsureLength(ref _edgeUsed, count);
        _edgeStarts.Clear();
        _edgeEnds.Clear();

        for (var i = 0; i < count; i++)
        {
            var (start, end) = _edges[i];
            _nextByStart[i] = _edgeStarts.TryGetValue(start, out var s) ? s : -1;
            _edgeStarts[start] = i;
            _nextByEnd[i] = _edgeEnds.TryGetValue(end, out var e) ? e : -1;
            _edgeEnds[end] = i;
            _edgeUsed[i] = false;
        }

        _mergedEdges.Clear();

        for (var i = 0; i < count; i++)
        {
            if (_edgeUsed[i])
                continue;

            _edgeUsed[i] = true;
            var (start, end) = _edges[i];

            // Extend forward: edges starting where we end.
            while (true)
            {
                var found = -1;
                for (var j = _edgeStarts.GetValueOrDefault(end, -1); j != -1; j = _nextByStart[j])
                {
                    if (!_edgeUsed[j] && Continues(start, end, _edges[j].End))
                    {
                        found = j;
                        break;
                    }
                }

                if (found == -1)
                    break;

                _edgeUsed[found] = true;
                end = _edges[found].End;
            }

            // Extend backward: edges ending where we start.
            while (true)
            {
                var found = -1;
                for (var j = _edgeEnds.GetValueOrDefault(start, -1); j != -1; j = _nextByEnd[j])
                {
                    if (!_edgeUsed[j] && Continues(_edges[j].Start, start, end))
                    {
                        found = j;
                        break;
                    }
                }

                if (found == -1)
                    break;

                _edgeUsed[found] = true;
                start = _edges[found].Start;
            }

            _mergedEdges.Add((start, end));
        }

        _edges.Clear();
        _edges.AddRange(_mergedEdges);
    }

    private static bool Continues(Vector2 a, Vector2 b, Vector2 c)
    {
        return Vector2.Dot(b - a, c - b) > 0f
               && CollinearSimplifier.IsCollinear(a, b, c, 10f * float.Epsilon);
    }
}

public sealed partial class GridDrawData
{
    public TimeSpan NextBuild;
}
