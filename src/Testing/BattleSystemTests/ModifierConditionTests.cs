namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Predicates over the owner's own state: what they answer, when they announce a flip, and what they
    /// leave behind when they let go of a fighter. The threshold family carries a band on purpose — a
    /// fight trades damage and healing across a line dozens of times and the parameter behind the line
    /// must not blink along.
    /// </summary>
    [TestClass]
    public class ModifierConditionTests
    {
        private const float MaxHealth = 100f;
        private const float MaxMana = 50f;
        private const float Threshold = 0.4f;
        private const float Tolerance = 0.001f;
        private const string EffectId = "Effect_Test";

        private static readonly float Band = ConditionTuning.Default.ResourceThresholdHysteresis;

        [TestMethod]
        public void AttachingToAWoundedFighter_ArmsTheLineWithoutWaitingForASignal()
        {
            var owner = Wounded(0.3f);

            var condition = Attached(HealthThreshold(), owner);

            Assert.IsTrue(condition.IsMet, "the line has to count from the moment it lands, not from the next hit");
        }

        [TestMethod]
        public void AttachingToAHealthyFighter_LeavesTheLineDisarmed()
        {
            var owner = Wounded(1f);

            Assert.IsFalse(Attached(HealthThreshold(), owner).IsMet);
        }

        [TestMethod]
        public void HealingBackAcrossTheLineButInsideTheBand_KeepsTheLineArmed()
        {
            var owner = Wounded(0.3f);
            var condition = Attached(HealthThreshold(), owner);

            owner.CurrentHealth = MaxHealth * (Threshold + Band / 2f);

            Assert.IsTrue(condition.IsMet, "the band is what stops the line from blinking; crossing back is not enough to disarm it");
        }

        [TestMethod]
        public void ClimbingClearOfTheBand_DisarmsTheLine()
        {
            var owner = Wounded(0.3f);
            var condition = Attached(HealthThreshold(), owner);

            owner.CurrentHealth = MaxHealth * (Threshold + Band + 0.01f);

            Assert.IsFalse(condition.IsMet);
        }

        [TestMethod]
        public void TradingDamageAndHealingAroundTheLine_ProducesOneFlipInsteadOfASeries()
        {
            var owner = Wounded(1f);
            var condition = Attached(HealthThreshold(), owner);
            int flips = 0;
            condition.StateChanged += met => flips++;

            // Every second value sits above the threshold — an unbanded predicate flips on all eight.
            foreach (float share in new[] { 0.39f, 0.41f, 0.38f, 0.42f, 0.39f, 0.41f, 0.38f, 0.42f })
                owner.CurrentHealth = MaxHealth * share;

            Assert.AreEqual(1, flips, "chatter around the threshold reached the parameter as a stream of refreshes");
            Assert.IsTrue(condition.IsMet);
        }

        [TestMethod]
        public void AFullSwingAcrossTheBand_FlipsBothWays()
        {
            var owner = Wounded(1f);
            var condition = Attached(HealthThreshold(), owner);
            var flips = new List<bool>();
            condition.StateChanged += met => flips.Add(met);

            owner.CurrentHealth = MaxHealth * 0.2f;
            owner.CurrentHealth = MaxHealth;

            CollectionAssert.AreEqual(new[] { true, false }, flips);
        }

        [TestMethod]
        public void RaisingTheMaximum_ArmsTheLineWithoutTheResourceMoving()
        {
            var owner = Wounded(0.5f);
            var condition = Attached(HealthThreshold(), owner);

            // Same 50 points of health, twice the pool: half full becomes a quarter full.
            owner.SetMaximum(EntityParameter.Health, MaxHealth * 2f);

            Assert.IsTrue(condition.IsMet, "the share is measured against a maximum that gear moves too");
        }

        [TestMethod]
        public void AThresholdOnMana_ReadsManaAndIgnoresHealth()
        {
            var owner = Wounded(0.1f);
            var condition = Attached(new ResourceThresholdCondition(Costs.Mana, Threshold, Band), owner);

            Assert.IsFalse(condition.IsMet, "full mana on a nearly dead fighter must not arm a mana line");

            owner.CurrentMana = MaxMana * 0.1f;

            Assert.IsTrue(condition.IsMet);
        }

        [TestMethod]
        public void AThresholdOnAVitalTheFighterDoesNotHave_WaitsForItInsteadOfArming()
        {
            var owner = Wounded(1f); // no barrier at all: the maximum is zero
            var thin = Attached(new ResourceThresholdCondition(Costs.Barrier, Threshold, Band), owner);

            Assert.IsFalse(thin.IsMet, "'while the barrier is thin' handed itself to every fighter who never had a barrier");

            owner.SetMaximum(EntityParameter.Barrier, MaxHealth);
            owner.CurrentBarrier = MaxHealth * 0.2f;
            Assert.IsTrue(thin.IsMet, "a barrier can appear mid-fight, and the line follows the maximum too");

            owner.SetMaximum(EntityParameter.Barrier, 0f);
            Assert.IsFalse(thin.IsMet, "the barrier gone takes the line with it");
        }

        [TestMethod]
        public void AnInvertedThresholdOnAVitalTheFighterDoesNotHave_WaitsForItToo()
        {
            var owner = Wounded(1f);
            owner.SetMaximum(EntityParameter.Mana, 0f); // a strength fighter carries no mana pool either
            var barrierHolds = Attached(new ResourceThresholdCondition(Costs.Barrier, Threshold, Band) { Negate = true }, owner);
            var manaHolds = Attached(new ResourceThresholdCondition(Costs.Mana, Threshold, Band) { Negate = true }, owner);
            var flips = new List<bool>();
            barrierHolds.StateChanged += met => flips.Add(met);

            Assert.IsFalse(barrierHolds.IsMet, "'while the barrier is above the line' handed itself to every fighter who never had a barrier");
            Assert.IsFalse(manaHolds.IsMet, "the hole is any vital the fighter does not have, not the barrier");

            owner.SetMaximum(EntityParameter.Barrier, MaxHealth);
            owner.CurrentBarrier = MaxHealth;
            Assert.IsTrue(barrierHolds.IsMet, "a barrier can appear mid-fight, and a full one is above the line");

            owner.SetMaximum(EntityParameter.Barrier, 0f);
            Assert.IsFalse(barrierHolds.IsMet, "the barrier gone takes the inverted line with it");
            CollectionAssert.AreEqual(new[] { true, false }, flips, "the vital appearing and vanishing never reached the parameter behind the line");
        }

        [TestMethod]
        public void APredicateBuiltOnAResourceNothingFollows_RefusesToExistAtAll()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ResourceThresholdCondition(Costs.Health | Costs.Mana, Threshold, Band),
                "a combination names no vital: the predicate would follow no signal and answer from an empty pair forever");
        }

        [TestMethod]
        public void EveryVitalTheFamilySaysItFollowsIsWiredEndToEnd()
        {
            foreach (var resource in ResourceCondition.Followable)
            {
                var owner = Wounded(1f);
                owner.SetMaximum(MaximumOf(resource), MaxHealth);
                Spend(owner, resource, MaxHealth);
                var full = new ResourceStateCondition(resource, ResourceState.Full);
                full.Attach(owner);

                Assert.IsTrue(full.IsMet, $"{resource}: the predicate reads the wrong pair of values");

                Spend(owner, resource, MaxHealth / 2f);
                Assert.IsFalse(full.IsMet, $"{resource}: spending it reached no predicate — the resource signal is followed by nobody");

                Spend(owner, resource, MaxHealth);
                owner.SetMaximum(MaximumOf(resource), MaxHealth * 2f);
                Assert.IsFalse(full.IsMet, $"{resource}: the maximum moved and the predicate never heard it");

                full.Detach();
                Assert.AreEqual(0, owner.ListenerCount, $"{resource}: the predicate kept a subscription on a fighter it let go of");
            }
        }

        [TestMethod]
        public void TheBarrierStateFollowsTheBarrierBreakingAndComingBack()
        {
            var owner = Wounded(1f);
            owner.SetMaximum(EntityParameter.Barrier, MaxHealth);
            owner.CurrentBarrier = MaxHealth;
            var broken = Attached(new ResourceStateCondition(Costs.Barrier, ResourceState.Empty), owner);

            Assert.IsFalse(broken.IsMet);

            owner.CurrentBarrier = 0f;
            Assert.IsTrue(broken.IsMet);

            owner.CurrentBarrier = 1f;
            Assert.IsFalse(broken.IsMet, "a single point of barrier is a barrier");
        }

        [TestMethod]
        public void AFighterWithoutABarrierAtAll_ReadsAsBrokenAndNeverAsFull()
        {
            var owner = Wounded(1f);

            Assert.IsTrue(Attached(new ResourceStateCondition(Costs.Barrier, ResourceState.Empty), owner).IsMet);
            Assert.IsFalse(Attached(new ResourceStateCondition(Costs.Barrier, ResourceState.Full), owner).IsMet);
        }

        [TestMethod]
        public void TheFullStateFollowsTheFirstScratchAndTheLastPointHealed()
        {
            var owner = Wounded(1f);
            var full = Attached(new ResourceStateCondition(Costs.Health, ResourceState.Full), owner);

            Assert.IsTrue(full.IsMet);

            owner.CurrentHealth = MaxHealth - 1f;
            Assert.IsFalse(full.IsMet);

            owner.CurrentHealth = MaxHealth;
            Assert.IsTrue(full.IsMet);
        }

        [TestMethod]
        public void AnAggregateMaskAnswersForEveryMemberOfTheGroup()
        {
            var owner = Wounded(1f);
            var underControl = Attached(new StatusCondition(StatusMasks.Control), owner);

            Assert.IsFalse(underControl.IsMet);

            owner.ApplyStatus(StatusEffects.Paralysis);
            Assert.IsTrue(underControl.IsMet);

            owner.RemoveStatus(StatusEffects.Paralysis);
            Assert.IsFalse(underControl.IsMet);

            owner.ApplyStatus(StatusEffects.Freeze);
            Assert.IsTrue(underControl.IsMet, "the group is a mask, not a list of near-identical predicates");
        }

        [TestMethod]
        public void AStatusOutsideTheMaskLeavesThePredicateAlone()
        {
            var owner = Wounded(1f);
            var burning = Attached(new StatusCondition(StatusMasks.DamageOverTime), owner);
            int flips = 0;
            burning.StateChanged += met => flips++;

            owner.ApplyStatus(StatusEffects.Stun);

            Assert.IsFalse(burning.IsMet);
            Assert.AreEqual(0, flips, "a signal that does not move the predicate must not refresh the parameter");
        }

        [TestMethod]
        public void TheInvertedDebuffMaskReadsAsClean()
        {
            var owner = Wounded(1f);
            var clean = Attached(new StatusCondition(StatusMasks.Debuff) { Negate = true }, owner);

            Assert.IsTrue(clean.IsMet);

            owner.ApplyStatus(StatusEffects.Poison);
            Assert.IsFalse(clean.IsMet);

            owner.RemoveStatus(StatusEffects.Poison);
            Assert.IsTrue(clean.IsMet);

            owner.ApplyStatus(StatusEffects.Blind);
            Assert.IsTrue(clean.IsMet, "the mask covers what the fight resolves; a status stamped by an effect and read back by nobody must not answer for 'clean'");
        }

        [TestMethod]
        public void TheDebuffMaskCoversExactlyWhatItsContractClaims()
        {
            var outside = Enum.GetValues<StatusEffects>()
                .Where(status => status != StatusEffects.None && (StatusMasks.Debuff & status) == 0)
                .ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    StatusEffects.Blind, StatusEffects.Regeneration, StatusEffects.Rust, StatusEffects.Cursed,
                    StatusEffects.Fury, StatusEffects.Confused, StatusEffects.Charmed, StatusEffects.Vanished,
                },
                outside,
                "the mask and the contract written on it drifted apart: either 'clean' now answers for a status the fight never resolves against its bearer, "
                + "or a status was declared and nobody decided whether the aggregate takes it");
        }

        [TestMethod]
        public void TheShieldScopeFollowsTheAbsorptionLayer()
        {
            var owner = Wounded(1f);
            var shielded = Attached(new EffectCondition(EffectScope.Shield, string.Empty, 1), owner);
            var shield = new FakeShield();

            Assert.IsFalse(shielded.IsMet);

            owner.Effects.AddEffect(shield);
            Assert.IsTrue(shielded.IsMet);

            owner.Effects.RemoveEffect(shield);
            Assert.IsFalse(shielded.IsMet);
        }

        [TestMethod]
        public void TheStacksScopeCountsInstancesOfOneEffect()
        {
            var owner = Wounded(1f);
            var threeStacks = Attached(new EffectCondition(EffectScope.Stacks, EffectId, 3), owner);

            for (int stack = 0; stack < 2; stack++) owner.Effects.AddEffect(Stack());
            Assert.IsFalse(threeStacks.IsMet);

            owner.Effects.AddEffect(Stack());
            Assert.IsTrue(threeStacks.IsMet);
        }

        [TestMethod]
        public void TheDebuffScopeCountsOnlyWhatWorksAgainstTheOwner()
        {
            var owner = Wounded(1f);
            var twoDebuffs = Attached(new EffectCondition(EffectScope.Debuff, string.Empty, 2), owner);

            owner.Effects.AddEffect(new FakeEffect("Effect_Buff", isHarmful: false));
            owner.Effects.AddEffect(new FakeEffect("Effect_Curse", isHarmful: true));
            Assert.IsFalse(twoDebuffs.IsMet);

            owner.Effects.AddEffect(new FakeEffect("Effect_Rot", isHarmful: true));
            Assert.IsTrue(twoDebuffs.IsMet);
        }

        [TestMethod]
        public void TheInvertedAnyScopeReadsAsCarryingNothing()
        {
            var owner = Wounded(1f);
            var untouched = Attached(new EffectCondition(EffectScope.Any, string.Empty, 1) { Negate = true }, owner);
            var effect = new FakeEffect("Effect_Buff", isHarmful: false);

            Assert.IsTrue(untouched.IsMet);

            owner.Effects.AddEffect(effect);
            Assert.IsFalse(untouched.IsMet);

            owner.Effects.RemoveEffect(effect);
            Assert.IsTrue(untouched.IsMet);
        }

        [TestMethod]
        public void TheStanceConditionFollowsTheBook()
        {
            var owner = Wounded(1f);
            var inStrength = Attached(new StanceCondition(Stance.Strength), owner);

            Assert.IsFalse(inStrength.IsMet);

            owner.AbilityBook.SetStance(Stance.Strength);
            Assert.IsTrue(inStrength.IsMet);

            owner.AbilityBook.SetStance(Stance.Intelligence);
            Assert.IsFalse(inStrength.IsMet);
        }

        [TestMethod]
        public void RearrangingTheLoadoutDoesNotFlipTheStanceCondition()
        {
            var owner = Wounded(1f);
            var inDexterity = Attached(new StanceCondition(Stance.Dexterity), owner);
            int flips = 0;
            inDexterity.StateChanged += met => flips++;

            // The book announces the whole visible loadout, so a stance predicate hears more than stance
            // changes — and must stay silent on the rest.
            owner.AbilityBook.Unequip(Stance.Dexterity, 0);

            Assert.AreEqual(0, flips);
            Assert.IsTrue(inDexterity.IsMet);
        }

        [TestMethod]
        public void DetachReleasesEverySubscriptionEveryConditionTook()
        {
            foreach (var condition in EveryKind())
            {
                var owner = Wounded(1f);
                condition.Attach(owner);
                condition.Detach();
                int flips = 0;
                condition.StateChanged += met => flips++;

                Poke(owner);

                Assert.AreEqual(0, flips, $"{condition.GetType().Name} kept a subscription on a fighter it let go of");
                Assert.AreEqual(0, owner.ListenerCount, $"{condition.GetType().Name} leaked a subscription");
            }
        }

        [TestMethod]
        public void ADetachedConditionIsMetByNothing()
        {
            var owner = Wounded(0.1f);
            var condition = Attached(HealthThreshold(), owner);
            Assert.IsTrue(condition.IsMet);

            condition.Detach();

            Assert.IsFalse(condition.IsMet, "a line that lost its owner must stop counting");
        }

        [TestMethod]
        public void ReattachingMovesTheConditionInsteadOfDoublingIt()
        {
            var first = Wounded(1f);
            var second = Wounded(0.1f);
            var condition = HealthThreshold();

            condition.Attach(first);
            condition.Attach(second);

            Assert.AreEqual(0, first.ListenerCount, "the previous owner was dropped without being released");
            Assert.IsTrue(condition.IsMet);
        }

        [TestMethod]
        public void CopyReturnsAFreshUnattachedConditionWithItsOwnState()
        {
            var owner = Wounded(0.1f);
            var original = Attached(HealthThreshold(), owner);
            int originalFlips = 0;
            original.StateChanged += met => originalFlips++;

            var copy = original.Copy();

            Assert.IsFalse(copy.IsMet, "a copy that answers before it is attached shares the original's owner");

            var other = Wounded(1f);
            copy.Attach(other);
            other.CurrentHealth = MaxHealth * 0.1f;

            Assert.IsTrue(copy.IsMet);
            Assert.AreEqual(0, originalFlips, "the copy pushed its flips through the original's subscribers");
        }

        [TestMethod]
        public void CopyKeepsTheInversionFlag()
        {
            var owner = Wounded(1f);
            var copy = new StatusCondition(StatusMasks.Debuff) { Negate = true }.Copy();

            copy.Attach(owner);

            Assert.IsTrue(copy.IsMet);
        }

        [TestMethod]
        public void EveryBuiltInTypeBuildsFromItsRecord()
        {
            var parser = ConditionParser.Default();

            foreach (var record in EveryRecord())
                Assert.IsNotNull(parser.Parse(record), $"{record} produced no predicate");
        }

        [TestMethod]
        public void AnUnknownTypeIsRefusedInsteadOfDefaultingToAlwaysOn()
        {
            var parser = ConditionParser.Default();

            Assert.IsNull(parser.Parse(JObject.FromObject(new { type = "WhileTheMoonIsFull" })),
                "an unknown condition must drop its line, not let it apply unconditionally");
        }

        [TestMethod]
        public void ARecordWithABrokenShareIsRefused()
        {
            var parser = ConditionParser.Default();

            foreach (float share in new[] { 0f, -0.5f, 1.5f })
                Assert.IsNull(parser.Parse(JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = nameof(Costs.Health), value = share })),
                    $"a threshold of {share} of the maximum is not a share");
        }

        [TestMethod]
        public void ARecordWithATypoedResourceIsRefused()
        {
            var record = JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = "Stamina", value = Threshold });

            Assert.IsNull(ConditionParser.Default().Parse(record));
        }

        [TestMethod]
        public void AThresholdWithNoRoomLeftForItsBandIsRefused()
        {
            var parser = ConditionParser.Default();
            float highest = 1f - Band;

            foreach (float share in new[] { 1f, highest + 0.01f })
                Assert.IsNull(parser.Parse(ThresholdRecord(share)),
                    $"a line at {share} of the maximum releases above a full resource: it would arm once and never let go again, full health included");

            Assert.IsNotNull(parser.Parse(ThresholdRecord(highest)), "the highest share that still leaves room for the band is a legal line");
        }

        [TestMethod]
        public void TheBoundaryRecordAThresholdCannotExpressLetsGoAtFullHealth()
        {
            var owner = Wounded(1f);
            var record = JObject.FromObject(new
            {
                type = ConditionTypes.ResourceState, resource = nameof(Costs.Health), state = nameof(ResourceState.Full), negate = true,
            });
            var wounded = ConditionParser.Default().Parse(record);
            Assert.IsNotNull(wounded);
            wounded.Attach(owner);

            Assert.IsFalse(wounded.IsMet);

            owner.CurrentHealth = MaxHealth - 1f;
            Assert.IsTrue(wounded.IsMet);

            owner.CurrentHealth = MaxHealth;
            Assert.IsFalse(wounded.IsMet, "'while wounded' is the record the refused threshold points at — it has to answer without a band");
        }

        [TestMethod]
        public void ARecordNamingAResourceNoPredicateCanFollowIsRefused()
        {
            var parser = ConditionParser.Default();
            string combination = $"{nameof(Costs.Health)}, {nameof(Costs.Mana)}";

            foreach (var record in new[]
                     {
                         JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = combination, value = Threshold }),
                         JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = "5", value = Threshold }),
                         JObject.FromObject(new { type = ConditionTypes.ResourceState, resource = combination, state = nameof(ResourceState.Full) }),
                     })
                Assert.IsNull(parser.Parse(record),
                    $"'{record[ConditionFields.Resource]}' parses as a flag combination and names no vital: the predicate would follow no signal and read an empty pair, so its line would apply forever");
        }

        [TestMethod]
        public void AFactorySetThatClaimsATypeTwiceStillProducesAParser()
        {
            var doubled = new List<IConditionFactory>(ConditionParser.BuiltInFactories()) { new StanceConditionFactory() };

            var parser = new ConditionParser(doubled);

            Assert.IsNotNull(parser.Parse(JObject.FromObject(new { type = ConditionTypes.Stance, stance = nameof(Stance.Strength) })),
                "a doubled registration costs a report, not the host's startup");
        }

        [TestMethod]
        public void TheInversionFlagOnTheRecordInvertsThePredicate()
        {
            var owner = Wounded(1f);
            var record = JObject.FromObject(new { type = ConditionTypes.Stance, stance = nameof(Stance.Strength), negate = true });

            var condition = ConditionParser.Default().Parse(record);
            Assert.IsNotNull(condition);
            condition.Attach(owner);

            Assert.IsTrue(condition.IsMet, "the book is in the dexterity stance, so 'not in strength' holds");
        }

        [TestMethod]
        public void AParsedThresholdCarriesTheSharedBand()
        {
            var owner = Wounded(1f);
            var record = JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = nameof(Costs.Health), value = Threshold });
            var condition = ConditionParser.Default().Parse(record);
            Assert.IsNotNull(condition);
            condition.Attach(owner);

            owner.CurrentHealth = MaxHealth * (Threshold - 0.01f);
            owner.CurrentHealth = MaxHealth * (Threshold + Band / 2f);

            Assert.IsTrue(condition.IsMet, "the band is a decision for the whole family — a parsed line must get it too");
        }

        [TestMethod]
        public void AConditionalModifierCountsTowardsTheParameterOnlyWhileItsConditionHolds()
        {
            var owner = Wounded(1f);
            owner.SetMaximum(EntityParameter.Armor, MaxHealth);
            var modifier = new ConditionalModifier(weight: 1f, ModifierValueType.Increase, EntityParameter.Armor, 0.2f, HealthThreshold(), "test");

            modifier.ApplyTo(owner);
            Assert.AreEqual(MaxHealth, owner.Parameters.Armor, Tolerance, "the line counted while its condition was off");

            owner.CurrentHealth = MaxHealth * 0.2f;
            Assert.AreEqual(MaxHealth * 1.2f, owner.Parameters.Armor, Tolerance, "the flip never reached the parameter");

            owner.CurrentHealth = MaxHealth;
            Assert.AreEqual(MaxHealth, owner.Parameters.Armor, Tolerance, "the line kept counting after its condition went off");
        }

        [TestMethod]
        public void RemovingAConditionalModifierReleasesItsCondition()
        {
            var owner = Wounded(1f);
            owner.SetMaximum(EntityParameter.Armor, MaxHealth);
            var modifier = new ConditionalModifier(weight: 1f, ModifierValueType.Increase, EntityParameter.Armor, 0.2f, HealthThreshold(), "test");

            modifier.ApplyTo(owner);
            modifier.RemoveFrom(owner);
            owner.CurrentHealth = MaxHealth * 0.2f;

            Assert.AreEqual(MaxHealth, owner.Parameters.Armor, Tolerance);
            Assert.AreEqual(0, owner.ListenerCount, "the unequipped line left its condition listening to the fighter");
        }

        private static ResourceThresholdCondition HealthThreshold() => new(Costs.Health, Threshold, Band);

        private static JObject ThresholdRecord(float share) =>
            JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = nameof(Costs.Health), value = share });

        private static ICondition Attached(ICondition condition, ConditionOwner owner)
        {
            condition.Attach(owner);
            return condition;
        }

        private static ConditionOwner Wounded(float healthShare)
        {
            var owner = new ConditionOwner();
            owner.SetMaximum(EntityParameter.Health, MaxHealth);
            owner.SetMaximum(EntityParameter.Mana, MaxMana);
            owner.CurrentHealth = MaxHealth * healthShare;
            owner.CurrentMana = MaxMana;
            return owner;
        }

        private static FakeEffect Stack() => new(EffectId, isHarmful: true);

        /// <summary>The maximum behind a vital, restated here on purpose: the test knows the wiring
        /// independently, so a vital added to the family and not to this pair fails instead of passing
        /// quietly.</summary>
        private static EntityParameter MaximumOf(Costs resource) => resource switch
        {
            Costs.Health => EntityParameter.Health,
            Costs.Mana => EntityParameter.Mana,
            Costs.Barrier => EntityParameter.Barrier,
            _ => throw new NotSupportedException($"the family follows {resource} and the test does not know the maximum behind it"),
        };

        private static void Spend(ConditionOwner owner, Costs resource, float value)
        {
            switch (resource)
            {
                case Costs.Health: owner.CurrentHealth = value; break;
                case Costs.Mana: owner.CurrentMana = value; break;
                case Costs.Barrier: owner.CurrentBarrier = value; break;
                default: throw new NotSupportedException($"the family follows {resource} and the test does not know how to move it");
            }
        }

        /// <summary>One live instance of every predicate, so lifecycle rules are proven for all of them
        /// instead of for whichever one the test happened to name.</summary>
        private static IEnumerable<ICondition> EveryKind()
        {
            yield return HealthThreshold();
            yield return new ResourceStateCondition(Costs.Barrier, ResourceState.Empty);
            yield return new StatusCondition(StatusMasks.Control);
            yield return new EffectCondition(EffectScope.Any, string.Empty, 1);
            yield return new StanceCondition(Stance.Strength);
        }

        /// <summary>One record of every shipped type — the format as a consumer writes it.</summary>
        private static IEnumerable<JObject> EveryRecord()
        {
            yield return JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = nameof(Costs.Health), value = Threshold });
            yield return JObject.FromObject(new { type = ConditionTypes.ResourceState, resource = nameof(Costs.Barrier), state = nameof(ResourceState.Empty), negate = true });
            yield return JObject.FromObject(new { type = ConditionTypes.Status, statuses = new[] { nameof(StatusMasks.DamageOverTime) } });
            yield return JObject.FromObject(new { type = ConditionTypes.Status, statuses = new[] { nameof(StatusEffects.Stun), nameof(StatusEffects.Freeze) } });
            yield return JObject.FromObject(new { type = ConditionTypes.Effect, scope = nameof(EffectScope.Shield) });
            yield return JObject.FromObject(new { type = ConditionTypes.Effect, scope = nameof(EffectScope.Stacks), effectId = EffectId, count = 3 });
            yield return JObject.FromObject(new { type = ConditionTypes.Stance, stance = nameof(Stance.Strength) });
        }

        /// <summary>Fires every signal a condition can listen to, so a leaked subscription shows up
        /// whichever one it was taken on.</summary>
        private static void Poke(ConditionOwner owner)
        {
            owner.CurrentHealth = 0f;
            owner.CurrentMana = 0f;
            owner.CurrentBarrier = 0f;
            owner.SetMaximum(EntityParameter.Health, MaxHealth * 2f);
            owner.ApplyStatus(StatusEffects.Stun);
            owner.Effects.AddEffect(new FakeEffect("Effect_Poke", isHarmful: true));
            owner.AbilityBook.SetStance(Stance.Strength);
        }
    }
}

