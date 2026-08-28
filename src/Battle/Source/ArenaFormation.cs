namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    /// <summary>
    /// Named distances of the battle formation. Pure data — tune to taste.
    /// The scale they are written in: a fighter's sprite is a 480² frame at 0.35 scale ≈ 168 px on the
    /// field, a beast's body reaching ~155 px across. Every distance below is therefore measured in
    /// bodies, not in pixels-that-looked-nice: two slots a body-and-a-quarter apart read as two
    /// fighters, two slots half a body apart read as one blob.
    /// </summary>
    public sealed record ArenaFormationSettings
    {
        /// <summary>Distance between the FRONT ranks of two neighbouring clusters — the no-man's-land
        /// the fight is read across. Below ~500 the sides merge into one crowd, above ~700 the duel
        /// falls apart into two unrelated groups. This is the single knob that scales the whole field.</summary>
        public float FrontlineGap { get; init; } = 680f;

        /// <summary>The anti-stack floor: no two slots of the battlefield — same cluster, other
        /// cluster or summon — are ever placed closer than this. One body (~168 px) plus a margin.</summary>
        public float MinimumSlotDistance { get; init; } = 200f;

        /// <summary>How far apart the nearest bodies of two DIFFERENT clusters stand at the worst
        /// angle (three or four sectors sharing the circle, flanks pointing at each other). Wider
        /// than <see cref="MinimumSlotDistance"/> by design: sides must not interleave, only stand apart.</summary>
        public float MinimumClusterGap { get; init; } = 320f;

        /// <summary>Distance between successive ranks of a cluster; ranks recede AWAY from the arena centre.</summary>
        public float RankDepth { get; init; } = 380f;

        /// <summary>Distance between two neighbours standing shoulder to shoulder in one rank.</summary>
        public float RankSpread { get; init; } = 300f;

        /// <summary>The rank is bowed, not straight: a member <c>n</c> columns off the centre is pulled
        /// back by <c>RankBow * n²</c>. Costs nothing at the centre, opens the flanks of a wide line
        /// (and keeps the fight from looking like two rulers laid on the ground).</summary>
        public float RankBow { get; init; } = 90f;

        /// <summary>Odd ranks are shifted sideways by this fraction of a spread, so a second-row
        /// fighter stands in the gap of the first row instead of behind a back.</summary>
        public float RankStagger { get; init; } = 0.5f;

        /// <summary>Members per rank while at most two sides face each other — a wide battle line.</summary>
        public int WideRankWidth { get; init; } = 5;

        /// <summary>Members per rank once three or more clusters share the circle: a wide line would
        /// swing its flanks into the neighbouring sector, so the cluster goes deep instead.</summary>
        public int NarrowRankWidth { get; init; } = 3;

        /// <summary>Base distance of a summon slot from its summoner's slot.</summary>
        public float SummonSpacing { get; init; } = 210f;

        /// <summary>How many times a slot that landed too close to an occupied one may be nudged
        /// before it is placed anyway. Only summons and mid-battle latecomers ever need a nudge.</summary>
        public int MaxRelocationSteps { get; init; } = 6;

        /// <summary>The rectangle the battle camera actually frames (zoom 0.5 on 1920x1080 shows
        /// 3840x2160 — the formation is meant to fill its central two thirds). Not used by the
        /// geometry: it is the budget the geometry is tuned against, and what the pins measure.</summary>
        public float FrameWidth { get; init; } = 2600f;

        /// <inheritdoc cref="FrameWidth"/>
        public float FrameHeight { get; init; } = 1400f;
    }

    /// <summary>
    /// Pure slot geometry of the battlefield, computed from the battle's composition: the player's
    /// group is one cluster on its side, every hostile group is its own cluster around the arena
    /// centre (two groups face each other, three and more spread into sectors). A cluster is a
    /// bowed battle line receding into staggered ranks, sized from the number of sectors that have
    /// to share the circle; latecomer groups take the widest free angular gap and summon slots hug
    /// the summoner. Every slot handed out is remembered, and no new slot is ever placed within
    /// <see cref="ArenaFormationSettings.MinimumSlotDistance"/> of one — that is what keeps two
    /// bodies from standing in the same spot. All results are offsets from the arena centre — the
    /// arena adds its own centre point.
    /// </summary>
    public sealed class ArenaFormation(ArenaFormationSettings settings, float playerDirectionRadians = Mathf.Pi)
    {
        // Summon slots ring the summoner: behind-left / behind-right / straight behind
        // ("behind" = away from the arena centre), each measured in SummonSpacing units and each
        // at least one spacing from the summoner and from its litter-mates. Further rings recede
        // one whole spacing more.
        private static readonly (float Forward, float Side)[] s_summonPattern = [(0.6f, -0.85f), (0.6f, 0.85f), (1.15f, 0f)];

        private sealed class Cluster
        {
            public float Angle;

            /// <summary>Distance of the cluster's FRONT rank from the arena centre. Chosen when the
            /// cluster is born, from how many sectors shared the circle at that moment: the fronts of
            /// two neighbours on a circle of radius R sit <c>2R·sin(π/N)</c> apart, so R is solved
            /// back from the wanted <see cref="ArenaFormationSettings.FrontlineGap"/>.</summary>
            public float Radius;

            /// <summary>Members per rank of this cluster — wide while it owns half the circle, narrow
            /// once it only owns a sector.</summary>
            public int RankWidth;

            public int Members;
        }

        private readonly Dictionary<object, Cluster> _clusters = [];
        private readonly Dictionary<string, int> _summonCounters = [];

        // Every slot ever handed out, in centre-offset space. The anti-stack invariant is enforced
        // against this list, so it holds for members, latecomers and summons alike. Dead fighters
        // never release their slot: a replacement must not land on a body that is still lying there.
        private readonly List<Vector2> _taken = [];

        /// <summary>Plans the clusters known at battle start: the FIRST key is the player's side,
        /// the rest split the remaining circle evenly (2 groups → opposite, 3 → a triangle).</summary>
        public void PlanGroups(IReadOnlyList<object> groupKeys)
        {
            for (int i = 0; i < groupKeys.Count; i++)
            {
                if (_clusters.ContainsKey(groupKeys[i])) continue;
                _clusters[groupKeys[i]] = MakeCluster(playerDirectionRadians + Mathf.Tau * i / groupKeys.Count, groupKeys.Count);
            }
        }

        /// <summary>Centre offset for the group's next member. An unplanned group (latecomer of a
        /// brand-new side) claims a fresh cluster in the widest angular gap.</summary>
        public Vector2 ReserveSlot(object groupKey)
        {
            if (!_clusters.TryGetValue(groupKey, out var cluster))
            {
                cluster = MakeCluster(FindWidestGapAngle(), _clusters.Count + 1);
                _clusters[groupKey] = cluster;
            }

            int index = cluster.Members++;
            int rank = index / cluster.RankWidth;
            var outward = Vector2.FromAngle(cluster.Angle);
            var sideways = outward.Orthogonal();

            // Columns fill outwards from the centre of the rank (0, -1, +1, -2, +2 ...), odd ranks
            // are shifted half a step so a second-row fighter looks through the gap, not at a back.
            float column = ColumnOffset(index % cluster.RankWidth) + (rank % 2 == 1 ? settings.RankStagger : 0f);
            float depth = rank * settings.RankDepth + settings.RankBow * column * column;
            var desired = outward * (cluster.Radius + depth) + sideways * (column * settings.RankSpread);
            return PlaceClear(desired, outward);
        }

        /// <summary>Centre offset of the next summon slot beside the summoner's slot. The counter is
        /// per summoner and monotonic — a replacement wolf never lands on a living one — and the slot
        /// is nudged outward if the pattern would have dropped it onto a neighbouring fighter.</summary>
        public Vector2 ReserveSummonSlot(string summonerId, Vector2 summonerOffset)
        {
            int index = _summonCounters.GetValueOrDefault(summonerId);
            _summonCounters[summonerId] = index + 1;

            var outward = summonerOffset.LengthSquared() > 0.001f ? summonerOffset.Normalized() : Vector2.Right;
            var sideways = outward.Orthogonal();
            (float forward, float side) = s_summonPattern[index % s_summonPattern.Length];
            float ring = index / s_summonPattern.Length;
            var desired = summonerOffset
                          + outward * settings.SummonSpacing * (forward + ring)
                          + sideways * settings.SummonSpacing * side;
            return PlaceClear(desired, outward);
        }

        /// <summary>A cluster born into a circle shared by <paramref name="sectors"/> sides.</summary>
        private Cluster MakeCluster(float angle, int sectors) => new()
        {
            Angle = angle,
            Radius = FrontRadius(sectors),
            RankWidth = sectors <= 2 ? settings.WideRankWidth : settings.NarrowRankWidth,
        };

        /// <summary>Radius at which N evenly spaced fronts stand <see cref="ArenaFormationSettings.FrontlineGap"/>
        /// apart from their neighbours. A lone cluster has no neighbour to measure against — it keeps
        /// half a gap, so the anchor stays inside the field.</summary>
        private float FrontRadius(int sectors) =>
            sectors < 2 ? settings.FrontlineGap / 2f : settings.FrontlineGap / (2f * Mathf.Sin(Mathf.Pi / sectors));

        /// <summary>0, -1, +1, -2, +2 ... — a rank grows outwards from its centre, so a half-filled
        /// rank still stands around the cluster's axis instead of trailing off one flank.</summary>
        private static float ColumnOffset(int indexInRank) =>
            (indexInRank + 1) / 2 * (indexInRank % 2 == 1 ? -1f : 1f);

        /// <summary>Books the slot, keeping the anti-stack floor: a position that lands on top of an
        /// already booked one is nudged away — first backwards (deeper into the own formation), then
        /// to either flank — until it is clear.</summary>
        private Vector2 PlaceClear(Vector2 desired, Vector2 away)
        {
            if (IsClear(desired)) return Book(desired);

            var sideways = away.Orthogonal();
            for (int step = 1; step <= settings.MaxRelocationSteps; step++)
            {
                float shift = step * settings.MinimumSlotDistance;
                if (IsClear(desired + away * shift)) return Book(desired + away * shift);
                if (IsClear(desired + sideways * shift)) return Book(desired + sideways * shift);
                if (IsClear(desired - sideways * shift)) return Book(desired - sideways * shift);
            }

            // Out of room (a battlefield far past its slot budget): the fighter is still placed —
            // a body on a crowded field beats a body that never appears.
            return Book(desired);
        }

        private bool IsClear(Vector2 candidate) =>
            _taken.All(slot => slot.DistanceSquaredTo(candidate) >= settings.MinimumSlotDistance * settings.MinimumSlotDistance);

        private Vector2 Book(Vector2 slot)
        {
            _taken.Add(slot);
            return slot;
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
