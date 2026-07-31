namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Godot;
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
            {
                Assert.IsTrue(parser.TryParse(record, out var condition), $"{record} was refused");
                Assert.IsNotNull(condition, $"{record} produced no predicate");
            }
        }

        [TestMethod]
        public void AnUnknownTypeIsRefusedInsteadOfDefaultingToAlwaysOn()
        {
            var parser = ConditionParser.Default();

            bool parsed = parser.TryParse(JObject.FromObject(new { type = "WhileTheMoonIsFull" }), out var condition);

            Assert.IsFalse(parsed, "an unknown condition must drop its line, not let it apply unconditionally");
            Assert.IsNull(condition);
        }

        [TestMethod]
        public void AMissingRecordIsNotAnError()
        {
            Assert.IsTrue(ConditionParser.Default().TryParse(null, out var condition));
            Assert.IsNull(condition, "no record means an unconditional line, not a broken one");
        }

        [TestMethod]
        public void ARecordWithABrokenShareIsRefused()
        {
            var parser = ConditionParser.Default();

            foreach (float share in new[] { 0f, -0.5f, 1.5f })
                Assert.IsFalse(parser.TryParse(JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = nameof(Costs.Health), value = share }), out _),
                    $"a threshold of {share} of the maximum is not a share");
        }

        [TestMethod]
        public void ARecordWithATypoedResourceIsRefused()
        {
            var record = JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = "Stamina", value = Threshold });

            Assert.IsFalse(ConditionParser.Default().TryParse(record, out _));
        }

        [TestMethod]
        public void AThresholdWithNoRoomLeftForItsBandIsRefused()
        {
            var parser = ConditionParser.Default();
            float highest = 1f - Band;

            foreach (float share in new[] { 1f, highest + 0.01f })
                Assert.IsFalse(parser.TryParse(ThresholdRecord(share), out _),
                    $"a line at {share} of the maximum releases above a full resource: it would arm once and never let go again, full health included");

            Assert.IsTrue(parser.TryParse(ThresholdRecord(highest), out _), "the highest share that still leaves room for the band is a legal line");
        }

        [TestMethod]
        public void TheBoundaryRecordAThresholdCannotExpressLetsGoAtFullHealth()
        {
            var owner = Wounded(1f);
            var record = JObject.FromObject(new
            {
                type = ConditionTypes.ResourceState, resource = nameof(Costs.Health), state = nameof(ResourceState.Full), negate = true,
            });
            Assert.IsTrue(ConditionParser.Default().TryParse(record, out var wounded));
            wounded!.Attach(owner);

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
                Assert.IsFalse(parser.TryParse(record, out _),
                    $"'{record[ConditionFields.Resource]}' parses as a flag combination and names no vital: the predicate would follow no signal and read an empty pair, so its line would apply forever");
        }

        [TestMethod]
        public void AFactorySetThatClaimsATypeTwiceStillProducesAParser()
        {
            var doubled = new List<IConditionFactory>(ConditionParser.BuiltInFactories()) { new StanceConditionFactory() };

            var parser = new ConditionParser(doubled);

            Assert.IsTrue(parser.TryParse(JObject.FromObject(new { type = ConditionTypes.Stance, stance = nameof(Stance.Strength) }), out var condition),
                "a doubled registration costs a report, not the host's startup");
            Assert.IsNotNull(condition);
        }

        [TestMethod]
        public void TheInversionFlagOnTheRecordInvertsThePredicate()
        {
            var owner = Wounded(1f);
            var record = JObject.FromObject(new { type = ConditionTypes.Stance, stance = nameof(Stance.Strength), negate = true });

            Assert.IsTrue(ConditionParser.Default().TryParse(record, out var condition));
            condition!.Attach(owner);

            Assert.IsTrue(condition.IsMet, "the book is in the dexterity stance, so 'not in strength' holds");
        }

        [TestMethod]
        public void AParsedThresholdCarriesTheSharedBand()
        {
            var owner = Wounded(1f);
            var record = JObject.FromObject(new { type = ConditionTypes.ResourceThreshold, resource = nameof(Costs.Health), value = Threshold });
            Assert.IsTrue(ConditionParser.Default().TryParse(record, out var condition));
            condition!.Attach(owner);

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
            yield return JObject.FromObject(new { type = ConditionTypes.Effect, scope = nameof(EffectScope.Stacks), id = EffectId, count = 3 });
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

        /// <summary>The fighter as a condition sees it: live resource signals, a real parameters/modifiers
        /// pair, a real effects component and ability book, and a count of who is still listening.</summary>
        private sealed class ConditionOwner : IFightable
        {
            private float _health;
            private float _mana;
            private float _barrier;

            public ConditionOwner()
            {
                Effects = new EffectsComponent(this);
                AbilityBook = new AbilityBookComponent(this);
                Parameters.Initialize(ParameterModifiers.GetModifiers);
                ParameterModifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
            }

            public event Action<float>? CurrentManaChanged;
            public event Action<float>? CurrentBarrierChanged;
            public event Action<float>? CurrentHealthChanged;

            public IEffectsComponent Effects { get; }
            public IAbilityBookComponent AbilityBook { get; }
            public IParameterModifiersComponent ParameterModifiers { get; } = new ParameterModifiersComponent();
            public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();
            public IModifierHandlerComponent ModifierHandler { get; } = new ModifierHandlerComponent();
            public ICombatEventBus CombatEvents { get; } = new RecordingBus();
            public StatusEffects StatusEffects { get; private set; } = StatusEffects.None;

            /// <summary>Everything still subscribed to this fighter, across every channel a condition uses.</summary>
            public int ListenerCount =>
                Listeners(CurrentHealthChanged) + Listeners(CurrentManaChanged) + Listeners(CurrentBarrierChanged)
                + ParameterListeners + EffectListeners + BookListeners + ((RecordingBus)CombatEvents).HandlerCount;

            public float CurrentHealth
            {
                get => _health;
                set
                {
                    _health = value;
                    CurrentHealthChanged?.Invoke(value);
                }
            }

            public float CurrentMana
            {
                get => _mana;
                set
                {
                    _mana = value;
                    CurrentManaChanged?.Invoke(value);
                }
            }

            public float CurrentBarrier
            {
                get => _barrier;
                set
                {
                    _barrier = value;
                    CurrentBarrierChanged?.Invoke(value);
                }
            }

            public void SetMaximum(EntityParameter parameter, float value) => Parameters.SetBaseValueForParameter(parameter, value);

            public void ApplyStatus(StatusEffects status)
            {
                StatusEffects |= status;
                CombatEvents.Publish(new StatusEffectAppliedEvent(status));
            }

            public void RemoveStatus(StatusEffects status)
            {
                StatusEffects &= ~status;
                CombatEvents.Publish(new StatusEffectRemovedEvent(status));
            }

            // The rest of the fighter contract: a condition never reaches for any of it.
            public IPassiveSkillsComponent PassiveSkills => throw new NotSupportedException();
            public IEntityAttribute Dexterity => throw new NotSupportedException();
            public IEntityAttribute Strength => throw new NotSupportedException();
            public IEntityAttribute Intelligence => throw new NotSupportedException();
            public IAnimationsComponent Animations => throw new NotSupportedException();
            public IStance CurrentStance => throw new NotSupportedException();
            public IEntityGroup? Group { get; set; }
            public ITargetChooser? TargetChooser { get; set; }
            public bool IsFighting { get; set; }
            public bool IsAlive => CurrentHealth > 0f;
            public bool CanMove { get; set; }
            public string Id => "Fighter";
            public string InstanceId { get; } = Guid.NewGuid().ToString();
            public Texture2D? Icon => null;
            public string Description => string.Empty;
            public string DisplayName => Id;

            public bool IsSame(string otherId) => InstanceId == otherId;
            public float GetDamage() => 0f;
            public void ConsumeResource(Costs type, float amount) => throw new NotSupportedException();
            public bool TryApplyStatusEffect(StatusEffects statusEffect) => throw new NotSupportedException();
            public bool TryRemoveStatusEffect(StatusEffects statusEffect) => throw new NotSupportedException();
            public IFightable ChoseTarget(List<IFightable> targets) => throw new NotSupportedException();
            public void Kill(bool isDebug = false) => throw new NotSupportedException();
            public void SetupBattleEventBus(IBattleEventBus bus) => throw new NotSupportedException();
            public Task ReceiveAttack(IAttackContext context) => Task.CompletedTask;
            public Task Attack(IAttackContext context) => Task.CompletedTask;
            public Task TakeDamage(IDamageContext context) => Task.CompletedTask;
            public void Heal(IHealContext context) => throw new NotSupportedException();
            public void OnTurnStart() => throw new NotSupportedException();
            public void OnTurnEnd() => throw new NotSupportedException();
            public void AddItemToInventory(IItem item) => throw new NotSupportedException();
            public void InjectServices(IGameServiceProvider provider) => throw new NotSupportedException();

            private int ParameterListeners => Listeners(FieldOf(Parameters, nameof(IEntityParametersComponent.ParameterChanged)));

            private int EffectListeners =>
                Listeners(FieldOf(Effects, nameof(IEffectsComponent.EffectAdded))) + Listeners(FieldOf(Effects, nameof(IEffectsComponent.EffectRemoved)));

            private int BookListeners => Listeners(FieldOf(AbilityBook, nameof(IAbilityBookComponent.ActiveAbilitiesChanged)));

            /// <summary>The components hide their subscriber lists behind field-like events; the leak
            /// tests need the count, and reading the backing field is the only way to it.</summary>
            private static Delegate? FieldOf(object target, string name) =>
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as Delegate;

            private static int Listeners(Delegate? handler) => handler?.GetInvocationList().Length ?? 0;
        }

        /// <summary>A combat bus that can be asked what is still listening.</summary>
        private sealed class RecordingBus : ICombatEventBus
        {
            private readonly List<(Type Event, object Handler)> _handlers = [];

            public int HandlerCount => _handlers.Count;

            public void Publish<T>(T evnt)
                where T : notnull, ICombatEvent
            {
                foreach ((Type type, object handler) in _handlers.ToArray())
                    if (type == typeof(T))
                        ((Action<T>)handler).Invoke(evnt);
            }

            public void Subscribe<T>(Action<T> handler)
                where T : notnull, ICombatEvent => _handlers.Add((typeof(T), handler));

            public void Unsubscribe<T>(Action<T> handler)
                where T : notnull, ICombatEvent => _handlers.RemoveAll(entry => entry.Event == typeof(T) && entry.Handler.Equals(handler));

            public void SubscribeAll(Action<ICombatEvent> handler) => _handlers.Add((typeof(ICombatEvent), handler));

            public void UnsubscribeAll(Action<ICombatEvent> handler) => _handlers.RemoveAll(entry => entry.Handler.Equals(handler));

            public void Dispose() => _handlers.Clear();
        }

        /// <summary>An effect with nothing but the traits a condition reads: an id, a source and a side.</summary>
        private class FakeEffect(string id, bool isHarmful) : IEffect
        {
            public IFightable? Target => null;
            public StatusEffects Status { get; set; } = StatusEffects.None;
            public int Duration { get; set; } = 1;
            public int MaxStacks { get; set; } = 99;
            public string Source => "test";
            public bool IsHarmful => isHarmful;
            public string Id => id;
            public string InstanceId { get; } = Guid.NewGuid().ToString();
            public Texture2D? Icon => null;
            public string Description => string.Empty;
            public string DisplayName => Id;

            public event Action<int>? DurationChanged;

            public bool IsSame(string otherId) => InstanceId == otherId;
            public Task Apply(EffectApplyingContext context) => Task.CompletedTask;
            public void OnStackChanged(int currentStack) => DurationChanged?.Invoke(Duration);
            public void Remove()
            {
            }

            public void TurnStart()
            {
            }

            public void TurnEnd()
            {
            }

            public bool IsStronger(IEffect otherEffect) => false;
            public IEffect Copy() => new FakeEffect(Id, IsHarmful);
        }

        private sealed class FakeShield() : FakeEffect("Effect_Shield", isHarmful: false), IShieldEffect
        {
            public float Strength => 1f;

            public float Absorb(float damage) => damage;
        }
    }
}
