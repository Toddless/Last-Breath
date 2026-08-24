namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;

    /// <summary>
    /// Effectiveness as a property of the EFFECT: the cast decides how strongly what it lays lands, the
    /// number rides along on the application and the effect reads its own figures through it. One place
    /// does the multiplying, so a buff written tomorrow scales because it is a buff and not because its
    /// author remembered — the arrangement this replaces was a hand-written multiplication at the place
    /// each effect was built, which was written twice in one wave and forgotten twice beside it.
    ///
    /// The walks below are on the mechanism rather than on any one ability: what a cast passes in is
    /// covered where the records are, and what is asked here is that the number arrives, that it
    /// multiplies the right figure, and that everything which is NOT a cast is left exactly as it was.
    /// </summary>
    [TestClass]
    public class EffectEffectivenessTests
    {
        /// <summary>The figure the effects below are authored with, and a cast that lands a third harder.</summary>
        private const float Authored = 0.15f;
        private const float Strong = 1.35f;

        [TestMethod]
        public async Task AnEffectLaidByNothingInParticularKeepsTheNumbersItWasWrittenWith()
        {
            // Passives, item grants, boss stages: everything that is not a cast has no effectiveness to
            // hand over, and the default has to be the number that changes nothing. A multiplier that
            // started at the struct default would zero every figure of every such effect in the game.
            var buff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);

            await buff.Apply(Laying(new ConditionOwner()));

            Assert.AreEqual(1f, buff.Effectiveness, "an effect laid without a cast came out at something other than one");
            Assert.AreEqual(Authored, buff.Value, 0.0001f, "an effect laid without a cast no longer carries what it was written with");
        }

        [TestMethod]
        public async Task ACastThatLandsHarderRaisesTheFigureTheEffectCarries()
        {
            var buff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);

            await buff.Apply(Laying(new ConditionOwner(), Strong));

            Assert.AreEqual(Authored * Strong, buff.Value, 0.0001f, "the cast's effectiveness never reached the figure the buff carries");
        }

        [TestMethod]
        public async Task AReductionIsScaledBeforeItIsInvertedAndNotAfter()
        {
            // The trap the type exists for. A debuff written as "how much the parameter loses" reaches
            // its decorator as "how much it keeps", and the two operations have an order: scale the
            // loss and then invert, and a strong cast takes more away. Invert and then scale, and a
            // strong cast hands the victim MORE evade than he started with — a debuff that heals.
            var debuff = new Clumsiness(duration: 3, maxStacks: 5, value: Authored);

            await debuff.Apply(Laying(new ConditionOwner(), Strong));

            Assert.AreEqual(1f - (Authored * Strong), debuff.Value, 0.0001f,
                "the loss was inverted before it was scaled, so the debuff moved the wrong way");
            Assert.IsTrue(debuff.Value < 1f - Authored, "a stronger cast left a weaker debuff");
        }

        [TestMethod]
        public async Task EveryInvertedDebuffOfTheBookStaysADebuffHoweverHardTheCastLands()
        {
            // Written as a sweep and not as one case, because the shape is what goes wrong: a debuff
            // authored as "how much the parameter loses" reaches its decorator as "how much it keeps",
            // and the two that were still converting the figure at the call turned into HEALING at high
            // effectiveness — a withering curse handing the victim more maximum health than he started
            // with. Every inverted debuff in the book is walked so the next one written cannot join
            // them quietly, and each is asked the two things that matter: below one, and further below
            // one the harder the cast lands.
            foreach (ParameterChangeEffect debuff in Inverted())
            {
                await debuff.Apply(Laying(new ConditionOwner(), Strong));

                Assert.IsTrue(debuff.Value < 1f, $"'{debuff.Id}' left the parameter above where it found it — it is healing, not a debuff");
                Assert.AreEqual(1f - (Authored * Strong), debuff.Value, 0.0001f, $"'{debuff.Id}' no longer scales the loss it was written with");
                Assert.IsTrue(debuff.Value < 1f - Authored, $"a stronger cast left '{debuff.Id}' weaker than a plain one");
            }
        }

        [TestMethod]
        public async Task TheCompositeFamilyIsScaledInTheSameShapesAsTheSingleOne()
        {
            // The second family of parameter changes, and the one the sweep above could not see: a
            // composite carries a LIST of changes, each with its own shape, and every one of them has to
            // come out the same way a single change would.
            foreach (CompositeParameterChangeEffect debuff in InvertedComposites())
            {
                await debuff.Apply(Laying(new ConditionOwner(), Strong));

                foreach (ParameterChange change in debuff.Changes)
                {
                    Assert.IsTrue(debuff.ValueOf(change) < 1f,
                        $"'{debuff.Id}' left {change.Parameter} above where it found it — it is healing, not a debuff");
                    Assert.AreEqual(1f - (Authored * Strong), debuff.ValueOf(change), 0.0001f,
                        $"'{debuff.Id}' no longer scales the loss it was written with");
                }
            }

            var blessing = new AresBlessingEffect(duration: 3, healthBonus: Authored, recoveryBonus: Authored);
            await blessing.Apply(Laying(new ConditionOwner(), Strong));

            foreach (ParameterChange change in blessing.Changes)
                Assert.AreEqual(1f + (Authored * Strong), blessing.ValueOf(change), 0.0001f,
                    $"the blessing scaled the multiplier instead of the share it was written with");
        }

        [TestMethod]
        public async Task ADebuffThatInvertsOutsideTheParameterFamilyIsScaledTheSameWay()
        {
            // Weakness carries its loss to a damage modifier instead of a parameter decorator, so
            // neither sweep above sees it — and it was the last place in the book still inverting by
            // hand. Same claim, asked of the number it actually hands over.
            var weakness = new Weakness(duration: 3, maxStacks: 3, value: Authored);

            await weakness.Apply(Laying(new ConditionOwner(), Strong));

            Assert.IsTrue(weakness.DamageLeft < 1f, "Weakness left the bearer hitting harder than before — it is a buff");
            Assert.AreEqual(1f - (Authored * Strong), weakness.DamageLeft, 0.0001f,
                "Weakness no longer scales the loss it was written with");
        }

        [TestMethod]
        public async Task StackingAnInvertedDebuffOnlyEverTakesMoreAway()
        {
            // What a record buying stacks buys. The decorator compounds a multiplicative change over
            // the stacks standing, so a figure that came out above one would compound into a bigger and
            // bigger GIFT with every stack the player paid for — the same bug, louder.
            var curse = new WitheringCurseEffect(duration: 3, maxStacks: 3, value: Authored);
            var bearer = new ConditionOwner();
            await curse.Apply(Laying(bearer, Strong));

            Assert.IsTrue(MathF.Pow(curse.Value, curse.MaxStacks) < curse.Value,
                "a second stack of the curse took less away than the first, so stacks are worth buying backwards");
        }

        [TestMethod]
        public async Task AnInvertedFigureStopsAtNothingLeftAndDoesNotGoPastIt()
        {
            // A loss the effectiveness has driven past the whole of the parameter takes all of it. One
            // step further is a NEGATIVE multiplier, and a bearer whose armor changes sign is not more
            // debuffed — he is in a state nothing else in the game knows how to read.
            var debuff = new WitheringCurseEffect(duration: 3, maxStacks: 1, value: 0.8f);

            await debuff.Apply(Laying(new ConditionOwner(), effectiveness: 2f));

            Assert.AreEqual(0f, debuff.Value, 0.0001f, "the loss went past everything there was to lose");
        }

        [TestMethod]
        public async Task ACopyIsScaledOnceAndNotTwice()
        {
            // Bonus stacks and transfers are copies, and a copy built from the figure this instance
            // CAME TO would be scaled again the moment it was applied. What a copy carries is what its
            // original was written with.
            var buff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);
            await buff.Apply(Laying(new ConditionOwner(), Strong));

            IEffect copy = buff.Copy();
            await copy.Apply(Laying(new ConditionOwner(), Strong));

            Assert.AreEqual(buff.Value, ((CritMitigationEffect)copy).Value, 0.0001f,
                "the copy came out at a different figure than the effect it was made of");
        }

        [TestMethod]
        public void AnEffectShippingUnderSeveralNamesCarriesItsNameIntoItsCopies()
        {
            // What a class serving more than one canonical row has to answer for. Copy() rebuilding at
            // the constructor's DEFAULT id would rename the effect halfway through a fight: a stack of
            // the Ice Aegis' protection would come back as the Porcupine's, wearing its wording and
            // stacking against the wrong ceiling.
            var named = new CritMitigationEffect(duration: 3, maxStacks: 1, value: Authored, id: "Effect_Ice_Crit_Mitigation");

            Assert.AreEqual("Effect_Ice_Crit_Mitigation", named.Copy().Id,
                "a copy came back under the class's default name instead of the one its original ships as");
        }

        [TestMethod]
        public async Task EveryGainingBuffOfTheBookStaysABuffHoweverHardTheCastLands()
        {
            // The mirror of the inversion sweep, and it exists because the mirror image of that bug had
            // been shipping unwatched: a buff authored as "how much the parameter GAINS" reaches a
            // multiplicative decorator as "what the parameter becomes", and one written without the
            // shape multiplied by the share instead of by one plus it. Light Step took the bearer's
            // evade to 15% of itself per stack while its own text promised +15%, and the Critical
            // Calculation buff did the same to critical chance. Both were green the whole time.
            foreach (ParameterChangeEffect buff in Gaining())
            {
                await buff.Apply(Laying(new ConditionOwner(), Strong));

                Assert.IsTrue(buff.Value > 1f, $"'{buff.Id}' left the parameter below where it found it — it is a debuff, not a buff");
                Assert.AreEqual(1f + (Authored * Strong), buff.Value, 0.0001f, $"'{buff.Id}' no longer scales the share it was written with");
                Assert.IsTrue(buff.Value > 1f + Authored, $"a stronger cast left '{buff.Id}' weaker than a plain one");
            }

            foreach (CompositeParameterChangeEffect buff in GainingComposites())
            {
                await buff.Apply(Laying(new ConditionOwner(), Strong));

                foreach (ParameterChange change in buff.Changes)
                {
                    Assert.IsTrue(buff.ValueOf(change) > 1f,
                        $"'{buff.Id}' left {change.Parameter} below where it found it — it is a debuff, not a buff");
                    Assert.AreEqual(1f + (Authored * Strong), buff.ValueOf(change), 0.0001f,
                        $"'{buff.Id}' no longer scales the share it was written with");
                }
            }
        }

        [TestMethod]
        public async Task AFigureThatIsNeitherGainedNorLostIsHandedOverUntouchedApartFromTheCast()
        {
            // The third shape. These move a parameter by an amount rather than by a share of it, so
            // there is nothing to invert and nothing to add one to — and a shape wrongly declared here
            // would turn an amount into a multiplier.
            foreach (ParameterChangeEffect effect in Plain())
            {
                await effect.Apply(Laying(new ConditionOwner(), Strong));

                Assert.AreEqual(Authored * Strong, effect.Value, 0.0001f,
                    $"'{effect.Id}' was reshaped on the way out — its figure is an amount, not a share");
            }
        }

        [TestMethod]
        public void EveryParameterChangingEffectOfTheBookIsWalkedInExactlyOneShape()
        {
            // What keeps the three sweeps above from being "the effects somebody remembered". A class
            // written tomorrow belongs to one of the three shapes; until it is named in one of the
            // lists, this fails and says so. Four classes were sitting outside every list when it was
            // written, and all four carried the shape bug.
            HashSet<Type> walked =
            [
                .. Inverted().Select(effect => effect.GetType()),
                .. InvertedComposites().Select(effect => effect.GetType()),
                .. Gaining().Select(effect => effect.GetType()),
                .. GainingComposites().Select(effect => effect.GetType()),
                .. Plain().Select(effect => effect.GetType()),
            ];

            var declared = typeof(ParameterChangeEffect).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsClass: true })
                .Where(type => typeof(ParameterChangeEffect).IsAssignableFrom(type) || typeof(CompositeParameterChangeEffect).IsAssignableFrom(type))
                .ToList();

            Assert.IsTrue(declared.Count > 0, "no parameter-changing effects were found at all, so this proves nothing");

            string[] unwalked = [.. declared.Except(walked).Select(type => type.Name).Order(StringComparer.Ordinal)];
            string[] strangers = [.. walked.Except(declared).Select(type => type.Name).Order(StringComparer.Ordinal)];

            Assert.AreEqual(0, unwalked.Length,
                $"parameter-changing effects no shape sweep walks — say which shape each is: [{string.Join(", ", unwalked)}]");
            Assert.AreEqual(0, strangers.Length, $"walked types that are not parameter-changing effects: [{string.Join(", ", strangers)}]");
        }

        /// <summary>Every buff of the book whose authored figure is what the parameter GAINS, so the
        /// number the decorator wants is one plus it.</summary>
        private static IEnumerable<ParameterChangeEffect> Gaining()
        {
            yield return new ArmorBuffEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new DamageBuffEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new SpellSurgeEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new GiantsBlessingEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new WeakRegenerationEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new LightStep(duration: 3, maxStacks: 5, value: Authored);
            yield return new AccuracyBuff(duration: 3, maxStacks: 3, value: Authored);
            yield return new CriticalChanceBuffEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new CritCalculationBuff(duration: 3, maxStacks: 3, value: Authored);
            yield return new AttackChanceCalculationBuff(duration: 3, maxStacks: 3, value: Authored);
            yield return new AdditionalHitChanceEffect(duration: 3, maxStacks: 3, value: Authored);
        }

        /// <summary>Composite buffs of the book whose changes are shares GAINED.</summary>
        private static IEnumerable<CompositeParameterChangeEffect> GainingComposites()
        {
            yield return new AresBlessingEffect(duration: 3, healthBonus: Authored, recoveryBonus: Authored);
            yield return new DarkShroudEffect(duration: 3, evadeValue: Authored);
        }

        /// <summary>Effects that move a parameter by an AMOUNT rather than by a share of it — added to
        /// it or taken off it, never multiplied — so their figure passes through unreshaped.</summary>
        private static IEnumerable<ParameterChangeEffect> Plain()
        {
            yield return new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new CriticalDamageBuffEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new ColdResistanceShredEffect(duration: 3, maxStacks: 1, value: Authored);
        }

        /// <summary>Every debuff of the book whose authored figure is what the parameter LOSES, so the
        /// number the decorator wants is one minus it. All of them at the same authored figure, because
        /// what is being asked is the shape and not the balance.</summary>
        private static IEnumerable<ParameterChangeEffect> Inverted()
        {
            yield return new WitheringCurseEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new RotEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new Clumsiness(duration: 3, maxStacks: 5, value: Authored);
            yield return new ArmorReductionEffect(duration: 3, maxStacks: 3, reduceBy: Authored);
            yield return new BlindEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new FatigueEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new FeeblenessEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new MindDrainEffect(duration: 3, maxStacks: 3, value: Authored);
        }

        /// <summary>Composite debuffs of the book whose changes are shares LOST.</summary>
        private static IEnumerable<CompositeParameterChangeEffect> InvertedComposites()
        {
            yield return new DecayEffect(duration: 3, maxStacks: 3, value: Authored);
            yield return new VulnerabilityEffect(duration: 3, maxStacks: 3, value: Authored);
        }

        /// <summary>An application on a fighter, with the effectiveness of the cast that lays it.</summary>
        private static EffectApplyingContext Laying(ConditionOwner bearer, float effectiveness = 1f) =>
            new() { Caster = bearer, Target = bearer, Source = "test", Effectiveness = effectiveness };
    }
}
