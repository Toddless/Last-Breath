namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Godot;

    /// <summary>
    /// Pure geometry of the battle formation: the player's cluster on its side, hostile groups in
    /// their own sectors (2 groups face each other), members packed inside a cluster, latecomer
    /// groups taking the widest free gap and summon slots hugging the summoner. Two invariants hold
    /// everything up: no two slots of the whole battlefield ever stand on top of each other
    /// (MinimumSlotDistance), and the sides stand apart at a distance the fight can be read across
    /// (FrontlineGap for a duel of two sides, MinimumClusterGap once sectors share the circle).
    /// </summary>
    [TestClass]
    public class ArenaFormationTests
    {
        private static readonly ArenaFormationSettings s_settings = new();

        /// <summary>The band a two-sided fight has to be staged in: closer and the sides read as one
        /// crowd, wider and the duel falls apart into two unrelated groups (owner's call).</summary>
        private const float MinReadableFrontline = 500f;
        private const float MaxReadableFrontline = 700f;

        [TestMethod]
        public void TwoGroups_FaceEachOtherAndStaySeparated()
        {
            var formation = new ArenaFormation(s_settings);
            object player = new();
            object enemies = new();
            formation.PlanGroups([player, enemies]);

            var playerSlots = ReserveMany(formation, player, 2);
            var enemySlots = ReserveMany(formation, enemies, 5);

            // Opposite sides: the clusters sit on the opposite ends of the X axis.
            Assert.IsTrue(playerSlots.All(slot => slot.X < 0), "the player's cluster must sit on the player's side");
            Assert.IsTrue(enemySlots.All(slot => slot.X > 0), "the enemy cluster must sit opposite");
            AssertGroupsSeparated(playerSlots, enemySlots);
        }

        [TestMethod]
        public void ThreeGroups_SpreadIntoSectorsAndStaySeparated()
        {
            var formation = new ArenaFormation(s_settings);
            object player = new();
            object bandits = new();
            object undead = new();
            formation.PlanGroups([player, bandits, undead]);

            var playerSlots = ReserveMany(formation, player, 1);
            var banditSlots = ReserveMany(formation, bandits, 3);
            var undeadSlots = ReserveMany(formation, undead, 3);

            AssertGroupsSeparated(playerSlots, banditSlots);
            AssertGroupsSeparated(playerSlots, undeadSlots);
            AssertGroupsSeparated(banditSlots, undeadSlots);
        }

        [TestMethod]
        public void MembersOfOneGroup_GetDistinctSlots()
        {
            var formation = new ArenaFormation(s_settings);
            object group = new();
            formation.PlanGroups([new object(), group]);

            var slots = ReserveMany(formation, group, 5);

            for (int i = 0; i < slots.Count; i++)
                for (int j = i + 1; j < slots.Count; j++)
                    Assert.IsTrue(slots[i].DistanceTo(slots[j]) > 50f, $"slots {i} and {j} overlap: {slots[i]} vs {slots[j]}");
        }

        [TestMethod]
        public void LatecomerGroup_TakesTheWidestGap_AndStaysSeparated()
        {
            var formation = new ArenaFormation(s_settings);
            object player = new();
            object enemies = new();
            formation.PlanGroups([player, enemies]);
            var playerSlots = ReserveMany(formation, player, 1);
            var enemySlots = ReserveMany(formation, enemies, 2);

            object latecomers = new(); // a brand-new side joins mid-battle
            var lateSlots = ReserveMany(formation, latecomers, 2);

            AssertGroupsSeparated(lateSlots, playerSlots);
            AssertGroupsSeparated(lateSlots, enemySlots);
        }

        [TestMethod]
        public void SummonSlots_HugTheSummoner()
        {
            var formation = new ArenaFormation(s_settings);
            object player = new();
            object enemies = new();
            formation.PlanGroups([player, enemies]);
            ReserveMany(formation, player, 1);
            var summonerSlot = formation.ReserveSlot(enemies);

            var wolfSlots = new List<Vector2>();
            for (int i = 0; i < 3; i++)
                wolfSlots.Add(formation.ReserveSummonSlot("summoner", summonerSlot));

            foreach (var slot in wolfSlots)
                Assert.IsTrue(summonerSlot.DistanceTo(slot) <= s_settings.SummonSpacing * 2f,
                    $"a summon slot strayed from its summoner: {summonerSlot.DistanceTo(slot)}");

            for (int i = 0; i < wolfSlots.Count; i++)
                for (int j = i + 1; j < wolfSlots.Count; j++)
                    Assert.IsTrue(wolfSlots[i].DistanceTo(wolfSlots[j]) > 50f, "summon slots overlap");
        }

        [TestMethod]
        public void SummonSlots_AreMonotonic_ReplacementNeverLandsOnALivingOne()
        {
            var formation = new ArenaFormation(s_settings);
            formation.PlanGroups([new object()]);
            var anchor = new Vector2(600f, 0f);

            var first = formation.ReserveSummonSlot("summoner", anchor);
            var second = formation.ReserveSummonSlot("summoner", anchor);
            var third = formation.ReserveSummonSlot("summoner", anchor);
            var replacement = formation.ReserveSummonSlot("summoner", anchor); // wolf #4 after a death

            Vector2[] taken = [first, second, third];
            foreach (var slot in taken)
                Assert.IsTrue(replacement.DistanceTo(slot) > 25f, "the replacement slot landed on an earlier one");
        }

        /// <summary>
        /// The anti-stack pin. A body is ~168 px wide on the field, so two slots closer than
        /// MinimumSlotDistance are two fighters standing inside each other — which is exactly what
        /// the field looked like: pairs merged into a single silhouette while the arena stood empty
        /// around them. The invariant covers EVERY slot of the battlefield at once — cluster mates,
        /// members of different sides, latecomers and summons — because the stacking never respected
        /// those boundaries either.
        /// </summary>
        [TestMethod]
        public void NoTwoSlotsOfTheField_EverStandOnEachOther()
        {
            foreach (var composition in Compositions())
            {
                var slots = LayOut(composition, out string name);
                AssertNoStacking(slots, name);
            }
        }

        /// <summary>Same floor, but for the slots born mid-battle: a boss summoning its pack while
        /// the field is already full is the case that used to drop a wolf onto its master's rank.</summary>
        [TestMethod]
        public void SummonsJoiningAFullField_NeverStandOnAnybody()
        {
            var formation = new ArenaFormation(s_settings);
            object player = new();
            object enemies = new();
            formation.PlanGroups([player, enemies]);

            var slots = ReserveMany(formation, player, 2);
            var enemySlots = ReserveMany(formation, enemies, 4);
            slots.AddRange(enemySlots);

            // Two summoners of the same side call three wolves each, mid-battle.
            for (int wolf = 0; wolf < 3; wolf++)
            {
                slots.Add(formation.ReserveSummonSlot("boss", enemySlots[0]));
                slots.Add(formation.ReserveSummonSlot("shaman", enemySlots[1]));
            }

            AssertNoStacking(slots, "1+1 vs 4 with two summoners");
        }

        /// <summary>
        /// The fight has to read as a confrontation: the two sides stand across a no-man's-land wide
        /// enough to tell them apart and narrow enough to still be one battle. Composition must not
        /// move that gap — only the depth of the sides behind their front ranks.
        /// </summary>
        [TestMethod]
        public void TwoSides_StandAcrossAReadableFrontline()
        {
            foreach ((int allies, int enemies) in new[] { (1, 1), (1, 5), (2, 4), (3, 3), (1, 9) })
            {
                var formation = new ArenaFormation(s_settings);
                object player = new();
                object foes = new();
                formation.PlanGroups([player, foes]);
                var playerSlots = ReserveMany(formation, player, allies);
                var enemySlots = ReserveMany(formation, foes, enemies);

                float gap = MinCrossDistance(playerSlots, enemySlots);
                Assert.IsTrue(gap >= MinReadableFrontline && gap <= MaxReadableFrontline,
                    $"{allies}v{enemies}: the sides stand {gap} apart, outside the readable band " +
                    $"[{MinReadableFrontline};{MaxReadableFrontline}]");
            }
        }

        /// <summary>
        /// The formation has to FILL the frame it is shot in, not sit in it as a dot: the battle
        /// camera at zoom 0.5 shows 3840x2160, and the formation is budgeted for its central
        /// two thirds. This pin holds the upper end — the field must not grow past the frame either.
        /// </summary>
        [TestMethod]
        public void TheFormation_FitsTheCameraFrame()
        {
            foreach (var composition in Compositions())
            {
                var slots = LayOut(composition, out string name);
                float width = slots.Max(slot => slot.X) - slots.Min(slot => slot.X);
                float height = slots.Max(slot => slot.Y) - slots.Min(slot => slot.Y);

                Assert.IsTrue(width <= s_settings.FrameWidth && height <= s_settings.FrameHeight,
                    $"{name}: the formation spills out of the frame — {width}x{height} " +
                    $"against {s_settings.FrameWidth}x{s_settings.FrameHeight}");
            }
        }

        /// <summary>
        /// The arena frames the battlefield by pointing its camera AT THE ANCHOR (offset zero —
        /// BattleArena.FocusCameraOnFormation). That is only honest while the clusters stay balanced
        /// around it: a formation growing off to one side would leave the camera staring at its corner,
        /// which is exactly how the line ended up pressed into the top-left of the screen.
        /// </summary>
        [TestMethod]
        public void Clusters_StayBalancedAroundTheAnchor_SoTheCameraCanFrameThem()
        {
            foreach (int groupCount in new[] { 2, 3, 4 })
            {
                var formation = new ArenaFormation(s_settings);
                var keys = new List<object>();
                for (int i = 0; i < groupCount; i++)
                    keys.Add(new object());
                formation.PlanGroups(keys);

                var slots = new List<Vector2>();
                for (int i = 0; i < keys.Count; i++)
                    slots.AddRange(ReserveMany(formation, keys[i], i + 1)); // deliberately uneven sides

                float minX = slots.Min(slot => slot.X), maxX = slots.Max(slot => slot.X);
                float minY = slots.Min(slot => slot.Y), maxY = slots.Max(slot => slot.Y);
                Assert.IsTrue(minX <= 0f && maxX >= 0f && minY <= 0f && maxY >= 0f,
                    $"{groupCount} groups: the anchor fell outside the formation's bounds x[{minX};{maxX}] y[{minY};{maxY}]");

                float radius = slots.Max(slot => slot.Length());
                var boundsCenter = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
                Assert.IsTrue(boundsCenter.Length() < radius * 0.5f,
                    $"{groupCount} groups: the formation grew off-anchor by {boundsCenter.Length()} of its {radius} reach");
            }
        }

        /// <summary>The compositions both field-wide pins are swept over: a duel, a lone hero against
        /// a pack, a party fight, three and four sides, and a battle filled to the slot budget.</summary>
        private static IEnumerable<int[]> Compositions() =>
        [
            [1, 1], [1, 5], [2, 4], [3, 4], [1, 9], [3, 3, 4], [1, 3, 3], [2, 2, 3, 3],
        ];

        /// <summary>Lays a composition out: the first group is the player's side, the rest are its
        /// opposition, planned together at battle start.</summary>
        private static List<Vector2> LayOut(int[] composition, out string name)
        {
            name = string.Join(" vs ", composition);
            var formation = new ArenaFormation(s_settings);
            var keys = composition.Select(_ => new object()).ToList();
            formation.PlanGroups(keys);

            var slots = new List<Vector2>();
            for (int i = 0; i < keys.Count; i++)
                slots.AddRange(ReserveMany(formation, keys[i], composition[i]));
            return slots;
        }

        private static void AssertNoStacking(List<Vector2> slots, string composition)
        {
            for (int i = 0; i < slots.Count; i++)
                for (int j = i + 1; j < slots.Count; j++)
                {
                    float distance = slots[i].DistanceTo(slots[j]);
                    // Half a pixel of slack: the floor is enforced on squared distances in floats.
                    Assert.IsTrue(distance >= s_settings.MinimumSlotDistance - 0.5f,
                        $"{composition}: slots {i} and {j} stand {distance} apart — inside the " +
                        $"{s_settings.MinimumSlotDistance} body floor ({slots[i]} vs {slots[j]})");
                }
        }

        private static List<Vector2> ReserveMany(ArenaFormation formation, object groupKey, int count)
        {
            var slots = new List<Vector2>();
            for (int i = 0; i < count; i++)
                slots.Add(formation.ReserveSlot(groupKey));
            return slots;
        }

        /// <summary>
        /// The sides are told apart by the GAP between them, not by being tighter than it: a wide
        /// battle line is legitimately wider than the no-man's-land in front of it (five fighters
        /// shoulder to shoulder span more than the 680 px they face the enemy across). So the honest
        /// invariant is the one the eye uses — the nearest body of the other side stands further away
        /// than the nearest body of your own, and never closer than MinimumClusterGap.
        /// </summary>
        private static void AssertGroupsSeparated(List<Vector2> first, List<Vector2> second)
        {
            float minCross = MinCrossDistance(first, second);
            Assert.IsTrue(minCross >= s_settings.MinimumClusterGap,
                $"the sides crowd each other: closest cross-group pair {minCross} against the " +
                $"{s_settings.MinimumClusterGap} cluster gap");

            float tightestIntra = Mathf.Min(MinIntraDistance(first), MinIntraDistance(second));
            if (tightestIntra == float.MaxValue) return; // two lone fighters: no intra pair to beat
            Assert.IsTrue(minCross > tightestIntra,
                $"groups are not separated: closest cross-group pair {minCross} is not beyond the " +
                $"tightest intra-cluster pair {tightestIntra}");
        }

        private static float MinCrossDistance(List<Vector2> first, List<Vector2> second)
        {
            float min = float.MaxValue;
            foreach (var a in first)
                foreach (var b in second)
                    min = Mathf.Min(min, a.DistanceTo(b));
            return min;
        }

        /// <summary>Tightest pair inside one cluster; a lone fighter has no pair to measure.</summary>
        private static float MinIntraDistance(List<Vector2> slots)
        {
            float min = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
                for (int j = i + 1; j < slots.Count; j++)
                    min = Mathf.Min(min, slots[i].DistanceTo(slots[j]));
            return min;
        }
    }
}
