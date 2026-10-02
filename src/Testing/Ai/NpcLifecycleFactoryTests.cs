namespace LastBreathTest.Ai
{
    using System.Collections.Generic;
    using Core.Ai.World;
    using Core.Data.NpcData;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>
    /// The single place where a definition becomes a post-defeat cycle. Its consumer is a scene node
    /// that no headless test builds, so a mixed-up branch would compile and silently give a peaceful
    /// resident the undead ending: these asserts are what keeps the two endings apart. The two kinds
    /// are told apart by their own numbers as well, so handing the cycle the other kind's config is
    /// caught too.
    /// </summary>
    [TestClass]
    public class NpcLifecycleFactoryTests
    {
        private const int Seed = 42;

        // Ranges that cannot be confused: whichever config reached the cycle is readable off one roll.
        private const float UndeadResurrectMinSeconds = 500f;
        private const float UndeadResurrectMaxSeconds = 501f;
        private const float VillagerRecoverMinSeconds = 30f;
        private const float VillagerRecoverMaxSeconds = 31f;

        [TestMethod]
        public void VillagerKind_BuildsTheCycleThatGetsUpAliveAndNeverBurns()
        {
            var lifecycle = NpcLifecycleFactory.Create(Definition(NpcLifecycleKind.Villager), Rolls());

            Assert.IsInstanceOfType<IAliveRiseLifecycle>(lifecycle, "a resident gets back on his feet as himself");
            Assert.IsNotInstanceOfType<IUndeadRiseLifecycle>(lifecycle, "nobody may await an undead rising from a resident");

            lifecycle.OnDefeated(isUndead: false);
            Assert.IsFalse(lifecycle.CanBeBurned, "a resident body is never burned, however long it lies");
            Assert.IsFalse(lifecycle.TryBurn(), "the resident cycle has no final death to reach");
        }

        [TestMethod]
        public void VillagerKind_CountsDownTheVillagerTimers_NotTheUndeadOnes()
        {
            var lifecycle = NpcLifecycleFactory.Create(Definition(NpcLifecycleKind.Villager), Rolls());

            lifecycle.OnDefeated(isUndead: false);

            Assert.IsTrue(lifecycle.ResurrectDelay is >= VillagerRecoverMinSeconds and <= VillagerRecoverMaxSeconds,
                $"the villager cycle must be fed the villager config, rolled {lifecycle.ResurrectDelay}");
        }

        [TestMethod]
        public void KindNamingNothing_KeepsTheUndeadCycleWithItsOwnConfig()
        {
            var lifecycle = NpcLifecycleFactory.Create(DefinitionNamingNoKind(), Rolls());

            Assert.IsInstanceOfType<IUndeadRiseLifecycle>(lifecycle, "the fate that existed before the villager one stays the default");
            Assert.IsNotInstanceOfType<IAliveRiseLifecycle>(lifecycle, "the same creature never comes back alive from this cycle");

            lifecycle.OnDefeated(isUndead: false);
            Assert.IsTrue(lifecycle.ResurrectDelay is >= UndeadResurrectMinSeconds and <= UndeadResurrectMaxSeconds,
                $"the undead cycle must be fed the undead config, rolled {lifecycle.ResurrectDelay}");
            Assert.IsTrue(lifecycle.CanBeBurned, "an undead-fated body can still be put down for good");
        }

        [TestMethod]
        public void UndeadKind_BuildsTheSameCycleAsNamingNoKindAtAll()
        {
            var lifecycle = NpcLifecycleFactory.Create(Definition(NpcLifecycleKind.Undead), Rolls());

            Assert.IsInstanceOfType<IUndeadRiseLifecycle>(lifecycle);
            Assert.IsNotInstanceOfType<IAliveRiseLifecycle>(lifecycle);

            lifecycle.OnDefeated(isUndead: false);
            Assert.IsTrue(lifecycle.ResurrectDelay is >= UndeadResurrectMinSeconds and <= UndeadResurrectMaxSeconds,
                $"the same cycle means the same config too, rolled {lifecycle.ResurrectDelay}");
        }

        [TestMethod]
        public void EveryLifecycleKind_HasAnAnsweredEnding()
        {
            // The factory falls back to the undead cycle for an unlisted kind, which is a safe runtime
            // answer and a silent one: a member added to the enum would quietly get the wrong ending in
            // the world. The loudness lives here — a kind nobody named an expected ending for fails
            // before release, and the table is the review of what each kind was promised.
            foreach (var kind in Enum.GetValues<NpcLifecycleKind>())
            {
                var lifecycle = NpcLifecycleFactory.Create(Definition(kind), Rolls());

                switch (kind)
                {
                    case NpcLifecycleKind.Villager:
                        Assert.IsInstanceOfType<IAliveRiseLifecycle>(lifecycle, $"'{kind}' must get back up alive");
                        break;
                    case NpcLifecycleKind.Undead:
                        Assert.IsInstanceOfType<IUndeadRiseLifecycle>(lifecycle, $"'{kind}' must come back as undead");
                        break;
                    default:
                        Assert.Fail($"'{kind}' was added to the enum without naming the ending its bodies get");
                        break;
                }
            }
        }

        private static IRandomNumberGenerator Rolls() => new DefaultRandomNumberGenerator(Seed);

        private static NpcDefinition DefinitionNamingNoKind() => new()
        {
            NpcId = "Npc_LifecycleFactory_Test",
            Parameters = new Dictionary<EntityParameter, float>(),
            Abilities = [],
            Lifecycle = new NpcLifecycleConfig
            {
                ResurrectMinSeconds = UndeadResurrectMinSeconds,
                ResurrectMaxSeconds = UndeadResurrectMaxSeconds,
            },
            VillagerLifecycle = new VillagerLifecycleConfig
            {
                RecoverMinSeconds = VillagerRecoverMinSeconds,
                RecoverMaxSeconds = VillagerRecoverMaxSeconds,
            },
        };

        private static NpcDefinition Definition(NpcLifecycleKind kind) => DefinitionNamingNoKind() with { LifecycleKind = kind };
    }
}
