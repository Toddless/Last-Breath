namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    /// <summary>Named distances of the battle formation. Pure data — tune to taste.</summary>
    public sealed record ArenaFormationSettings
    {
        /// <summary>Cluster anchor's distance from the arena center.</summary>
        public float ClusterDistance { get; init; } = 350;

        /// <summary>Depth step of a member rank: ranks recede AWAY from the center (0 / 1,2 / 3,4...).</summary>
        public float MemberDepthStep { get; init; } = 185f;

        /// <summary>Side offset of a rank pair, perpendicular to the cluster direction.</summary>
        public float MemberSpread { get; init; } = 150f;

        /// <summary>Base distance of a summon slot from its summoner's slot.</summary>
        public float SummonSpacing { get; init; } = 170f;
    }

    /// <summary>
    /// Pure slot geometry of the battlefield, computed from the battle's composition: the player's
    /// group is one cluster on its side, every hostile group is its own cluster around the arena
    /// center (two groups face each other, three and more spread into sectors). Latecomer groups
    /// take the widest free angular gap; summon slots hug the summoner's slot. All results are
    /// offsets from the arena center — the arena adds its own center point.
    /// </summary>
    public sealed class ArenaFormation(ArenaFormationSettings settings, float playerDirectionRadians = Mathf.Pi)
    {
        // Summon slots ring the summoner: slightly behind-left / behind-right / straight behind
        // ("behind" = away from the arena center), further rings recede one step more.
        private static readonly (float Forward, float Side)[] s_summonPattern = [(0.55f, -0.9f), (0.55f, 0.9f), (1.15f, 0f)];

        private sealed class Cluster
        {
            public float Angle;
            public int Members;
        }

        private readonly Dictionary<object, Cluster> _clusters = [];
        private readonly Dictionary<string, int> _summonCounters = [];

        /// <summary>Plans the clusters known at battle start: the FIRST key is the player's side,
        /// the rest split the remaining circle evenly (2 groups → opposite, 3 → a triangle).</summary>
        public void PlanGroups(IReadOnlyList<object> groupKeys)
        {
            for (int i = 0; i < groupKeys.Count; i++)
            {
                if (_clusters.ContainsKey(groupKeys[i])) continue;
                _clusters[groupKeys[i]] = new Cluster { Angle = playerDirectionRadians + Mathf.Tau * i / groupKeys.Count };
            }
        }

        /// <summary>Center offset for the group's next member. An unplanned group (latecomer of a
        /// brand-new side) claims a fresh cluster in the widest angular gap.</summary>
        public Vector2 ReserveSlot(object groupKey)
        {
            if (!_clusters.TryGetValue(groupKey, out var cluster))
            {
                cluster = new Cluster { Angle = FindWidestGapAngle() };
                _clusters[groupKey] = cluster;
            }

            int rank = cluster.Members++;
            var outward = Vector2.FromAngle(cluster.Angle);
            var perpendicular = outward.Orthogonal();
            float depth = (rank + 1) / 2 * settings.MemberDepthStep;
            float side = rank == 0 ? 0f : rank % 2 == 1 ? -1f : 1f;
            return outward * (settings.ClusterDistance + depth) + perpendicular * side * settings.MemberSpread;
        }

        /// <summary>Center offset of the next summon slot beside the summoner's slot. The counter is
        /// per summoner and monotonic — a replacement wolf never lands on a living one.</summary>
        public Vector2 ReserveSummonSlot(string summonerId, Vector2 summonerOffset)
        {
            int index = _summonCounters.GetValueOrDefault(summonerId);
            _summonCounters[summonerId] = index + 1;

            var outward = summonerOffset.LengthSquared() > 0.001f ? summonerOffset.Normalized() : Vector2.Right;
            var perpendicular = outward.Orthogonal();
            (float forward, float side) = s_summonPattern[index % s_summonPattern.Length];
            float ringDepth = index / s_summonPattern.Length * 0.9f;
            return summonerOffset + outward * settings.SummonSpacing * (forward + ringDepth)
                                  + perpendicular * settings.SummonSpacing * side;
        }

        /// <summary>Midpoint of the widest gap between the existing cluster directions.</summary>
        private float FindWidestGapAngle()
        {
            if (_clusters.Count == 0) return playerDirectionRadians;

            var angles = _clusters.Values.Select(cluster => Mathf.PosMod(cluster.Angle, Mathf.Tau)).OrderBy(angle => angle).ToList();
            if (angles.Count == 1) return angles[0] + Mathf.Pi;

            float bestStart = angles[^1];
            float bestGap = angles[0] + Mathf.Tau - angles[^1];
            for (int i = 1; i < angles.Count; i++)
            {
                float gap = angles[i] - angles[i - 1];
                if (gap <= bestGap) continue;
                bestGap = gap;
                bestStart = angles[i - 1];
            }

            return bestStart + bestGap / 2f;
        }
    }
}
