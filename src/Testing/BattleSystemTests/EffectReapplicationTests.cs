namespace LastBreathTest.BattleSystemTests
{
    using System.Threading.Tasks;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;

    /// <summary>
    /// What laying an effect on somebody who already carries it does. Below the ceiling it adds a stack and
    /// the newcomer arrives at its full duration; AT the ceiling it buys time and nothing else — the pile
    /// keeps the stacks it has and the standing one is refreshed.
    ///
    /// The arrangement this replaces evicted the oldest stack to make room. Nothing was compared on the way
    /// in, so a stack laid weak by a weak cast knocked out a strong one that was working, and the bearer
    /// paid for the privilege. And the refresh is to the LONGER of the two durations, never to the
    /// newcomer's: turns somebody spent an extension budget on are not undone by a plain re-application.
    /// </summary>
    [TestClass]
    public class EffectReapplicationTests
    {
        private const string ProbeId = "Effect_Reapplication_Probe";
        private const int Turns = 3;

        [TestMethod]
        public async Task AStackLaidUnderTheCeilingJoinsTheOnesAlreadyStanding()
        {
            // The premise of everything below: while there is room, a re-application is a new stack.
            var bearer = new ConditionOwner();

            await Laid(bearer, maxStacks: 2, payload: 10f);
            await Laid(bearer, maxStacks: 2, payload: 1f);

            Assert.AreEqual(2, bearer.Effects.Effects.Count, "a second stack was not laid although the ceiling had room");
        }

        [TestMethod]
        public async Task AtTheCeilingThePileKeepsExactlyTheStacksItHad()
        {
            var bearer = new ConditionOwner();
            IEffect strong = await Laid(bearer, maxStacks: 2, payload: 10f);
            IEffect second = await Laid(bearer, maxStacks: 2, payload: 10f);

            IEffect weak = await Laid(bearer, maxStacks: 2, payload: 1f);

            CollectionAssert.AreEquivalent(new[] { strong, second }, bearer.Effects.Effects.ToArray(),
                "the pile at its ceiling is no longer made of the stacks that were standing on it");
            Assert.IsFalse(bearer.Effects.Effects.Contains(weak), "a stack was laid on a pile that was already full");
        }

        [TestMethod]
        public async Task AWeakNewcomerDoesNotKnockOutAStrongStandingStack()
        {
            // The bug the eviction was: the multi-stack road compares nothing, so whoever laid last won.
            // A single enemy chipping in a feeble stack could put out the one the player had built up.
            var bearer = new ConditionOwner();
            var strong = (Probe)await Laid(bearer, maxStacks: 1, payload: 10f);

            await Laid(bearer, maxStacks: 1, payload: 1f);

            Assert.AreEqual(1, bearer.Effects.Effects.Count, "the bearer carries a different number of stacks than the ceiling allows");
            Assert.AreEqual(10f, ((Probe)bearer.Effects.Effects[0]).Payload, 0.0001f,
                "the standing stack now carries the weak newcomer's figure — it was evicted after all");
            Assert.AreSame(strong, bearer.Effects.Effects[0], "the strong stack is not the one still standing");
        }

        [TestMethod]
        public async Task ARefusedStackStillRefreshesTheStandingOneToItsFullDuration()
        {
            var bearer = new ConditionOwner();
            IEffect standing = await Laid(bearer, maxStacks: 1, payload: 10f);
            standing.Duration = 1; // a turn or two have passed

            await Laid(bearer, maxStacks: 1, payload: 10f);

            Assert.AreEqual(Turns, standing.Duration, "the re-application did not refresh the standing effect");
        }

        [TestMethod]
        public async Task ARefreshAtTheCeilingReachesTheStackThatWasStanding()
        {
            var bearer = new ConditionOwner();
            IEffect standing = await Laid(bearer, maxStacks: 2, payload: 10f);
            IEffect second = await Laid(bearer, maxStacks: 2, payload: 10f);
            standing.Duration = 1;
            second.Duration = 2;

            await Laid(bearer, maxStacks: 2, payload: 10f);

            Assert.AreEqual(Turns, standing.Duration,
                "a re-application onto a full pile bought no time at all — at the ceiling it is the only thing it buys");
        }

        [TestMethod]
        public async Task TheTimeGoesToTheStackClosestToGoingOut()
        {
            // Not the oldest one laid — the shortest one left. An old stack somebody extended can already
            // outlast the newcomer, and refreshing THAT one spends the whole application on nothing while
            // the stack that was about to go out goes out anyway.
            var bearer = new ConditionOwner();
            IEffect longest = await Laid(bearer, maxStacks: 2, payload: 10f);
            IEffect shortest = await Laid(bearer, maxStacks: 2, payload: 10f);
            longest.Duration = Turns + 5; // extended by somebody
            shortest.Duration = 1;

            await Laid(bearer, maxStacks: 2, payload: 10f);

            Assert.AreEqual(Turns, shortest.Duration, "the stack about to go out was left to go out");
            Assert.AreEqual(Turns + 5, longest.Duration, "the refresh went to the long stack and cut it back at that");
        }

        [TestMethod]
        public async Task ARefreshNeverCutsBackAnEffectSomebodyExtended()
        {
            // Extension spends a budget the instance carries and cannot get back. Refreshing to the
            // newcomer's duration rather than to the longer of the two would burn those turns for free,
            // and an ally re-applying his own buff would be shortening it.
            var bearer = new ConditionOwner();
            IEffect standing = await Laid(bearer, maxStacks: 1, payload: 10f);
            int granted = standing.Extend(2);
            Assert.IsTrue(granted > 0, "the extension budget granted nothing, so there is no extension to protect");

            await Laid(bearer, maxStacks: 1, payload: 10f);

            Assert.AreEqual(Turns + granted, standing.Duration, "a plain re-application cut the extension back off");
        }

        [TestMethod]
        public async Task ARefreshAtTheCeilingDoesNotCutBackAnExtensionEither()
        {
            var bearer = new ConditionOwner();
            IEffect standing = await Laid(bearer, maxStacks: 2, payload: 10f);
            await Laid(bearer, maxStacks: 2, payload: 10f);
            int granted = standing.Extend(2);
            Assert.IsTrue(granted > 0, "the extension budget granted nothing, so there is no extension to protect");

            await Laid(bearer, maxStacks: 2, payload: 10f);

            Assert.AreEqual(Turns + granted, standing.Duration, "the capped re-application cut the extension back off");
        }

        private static async Task<IEffect> Laid(ConditionOwner bearer, int maxStacks, float payload)
        {
            var probe = new Probe(maxStacks, payload);
            await probe.Apply(new EffectApplyingContext { Caster = bearer, Target = bearer, Source = "test" });
            return probe;
        }

        /// <summary>An effect wearing an id the canon carries no row for, so the ceiling under test is the
        /// one the walk sets rather than one the shipped file happens to balance. The payload is there to
        /// tell one stack from another after the fact.</summary>
        private sealed class Probe(int maxStacks, float payload) : Effect(ProbeId, Turns, maxStacks)
        {
            public float Payload => payload;

            public override IEffect Copy() => new Probe(MaxStacks, payload);
        }
    }
}
