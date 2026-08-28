namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Godot;

    /// <summary>
    /// Pure geometry of the battle formation: the player's cluster on its side, hostile groups in
    /// their own sectors (2 groups face each other), members packed inside a cluster, latecomer
    /// groups taking the widest free gap and summon slots hugging the summoner. The core
    /// invariant everywhere: clusters are separated — the distance between any two members of
    /// DIFFERENT groups exceeds any intra-cluster distance.
    /// </summary>
    [TestClass]
    public class ArenaFormationTests
    {
        private static readonly ArenaFormationSettings s_settings = new();

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

        private static List<Vector2> ReserveMany(ArenaFormation formation, object groupKey, int count)
        {
            var slots = new List<Vector2>();
            for (int i = 0; i < count; i++)
                slots.Add(formation.ReserveSlot(groupKey));
            return slots;
        }

        /// <summary>The core invariant: any cross-group distance beats any intra-group distance.</summary>
        private static void AssertGroupsSeparated(List<Vector2> first, List<Vector2> second)
        {
            float minCross = float.MaxValue;
            foreach (var a in first)
                foreach (var b in second)
                    minCross = Mathf.Min(minCross, a.DistanceTo(b));

            float maxIntra = MaxIntraDistance(first);
            maxIntra = Mathf.Max(maxIntra, MaxIntraDistance(second));

            Assert.IsTrue(minCross > maxIntra,
                $"groups are not separated: closest cross-group pair {minCross} vs widest intra-cluster spread {maxIntra}");
        }

        private static float MaxIntraDistance(List<Vector2> slots)
        {
            float max = 0f;
            for (int i = 0; i < slots.Count; i++)
                for (int j = i + 1; j < slots.Count; j++)
                    max = Mathf.Max(max, slots[i].DistanceTo(slots[j]));
            return max;
        }
    }
}
