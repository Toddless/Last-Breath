namespace LastBreathTest.PassiveSkills
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Effects;
    using Battle.Source.PassiveSkills;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using LastBreathTest.Combat;
    using Moq;

    /// <summary>
    /// Keystones taken TOGETHER. Every walk here is about a pair whose halves were written apart and meet
    /// on one fighter, where the answer is not either half's own claim: one keystone empties the very pool
    /// the other is paid out of, or gates the very roll the other stacks for. What a pair is worth is
    /// nobody's file to state, so it is stated here.
    /// <para>Each pair is pinned at its numbers rather than at "nothing fell over": a combination that
    /// silently drifts from a bargain to a bonus is exactly the failure a walk about pairs exists to catch.
    /// The pairs that already live beside their own keystone are not repeated — the mutual conversion pair
    /// belongs to <see cref="PoolConversionPassiveTests"/>, Trinity beside the Gift of Nature to
    /// <see cref="TrinityPassiveTests"/>, and Elemental Fury beside Vicious Bite to
    /// <see cref="ElementalFuryPassiveTests"/>.</para>
    /// </summary>
    [TestClass]
    public class KeystoneCombinationTests
    {
        /// <summary>Iron Will: the whole of evasion becomes armor and evasion is emptied. Registered by the
        /// pair it names rather than by a class, so the id is all there is to name it by.</summary>
        private const string IronWillId = "Passive_Skill_Iron_Will";

        /// <summary>Death Dance as the shipped tree writes it: critical damage counted per step of evasion,
        /// which is a line the family reads off a record and never a class.</summary>
        private const string DeathDanceId = "Passive_Skill_Stats_Death_Dance";

        private const string DeathDanceField = "CriticalDamage:Flat:Evade:200";
        private const float DeathDanceValue = 0.02f;
        private const int DeathDanceStep = 200;

        private const float EvadeBase = 6000f;
        private const float ArmorBase = 100f;
        private const float CriticalDamageBase = 2f;

        /// <summary>What the dance is worth on its own at <see cref="EvadeBase"/>: 6000 evasion is thirty
        /// steps of two hundred, and each step is +2% critical damage.</summary>
        private const float DanceGift = EvadeBase / DeathDanceStep * DeathDanceValue;

        private const float AgnosticCostScale = 0.25f;
        private const float SpiritPercentOfMaxMana = 0.05f;
        private const float SpiritRecoveryPenalty = -0.25f;
        private const float MaxMana = 200f;
        private const float MaxHealth = 1000f;
        private const float ManaRecoveryBase = 100f;

        /// <summary>A keystone that always pays: a walk about WHAT a blow is worth never has to arrange a draw.</summary>
        private const float Always = 1f;

        /// <summary>The evasion the stoic's stacking source is measured on, and the share one stack of
        /// "Лёгкий шаг" gains him.</summary>
        private const float StoicEvadeBase = 8000f;

        private const float LightStepShare = 0.15f;
        private const int LightStepStacks = 3;

        /// <summary>The curve constant evasion is read through — mirrored from Calculations, the same way
        /// <see cref="ChanceRollTests"/> mirrors it.</summary>
        private const float EvasionScalingFactor = 10000f;

        private const float Precision = 0.0001f;

        // ---- Железная воля + Танец смерти ----------------------------------------------------------------
        //
        // The dance is a line paid per step of evasion and the will empties evasion outright, so the pair is
        // a keystone spent on nothing. This is the contract of a conversion read on the one shipped keystone
        // that is actually priced in the emptied pool.

        /// <summary>The whole pair in one line of numbers: 6000 evasion is thirty steps of two hundred and
        /// buys the dance +60% critical damage, and a conversion of evasion takes every one of those steps
        /// away — 2.6 becomes 2.0 again while the armor takes the whole 6000.</summary>
        [TestMethod]
        public void TheDanceIsPaidPerStepOfEvasionAndIronWillLeavesNoStepsToCount()
        {
            var carrier = Dancer();
            carrier.Wear(DeathDance());

            Assert.AreEqual(CriticalDamageBase + DanceGift, carrier.Value(EntityParameter.CriticalDamage), Precision,
                "the dance is not measured off evasion at all, so the claim below would prove nothing");

            carrier.Wear(IronWill());

            Assert.AreEqual(CriticalDamageBase, carrier.Value(EntityParameter.CriticalDamage), Precision,
                "a line paid per step of an emptied pool kept being paid");
            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision,
                "the conversion did not hand the whole pool over");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
        }

        /// <summary>The order the two keystones are taken in cannot change the answer: the dance re-measures
        /// what it is counted off for as long as it is worn, so arriving after the pool was given away is the
        /// same as watching it go.</summary>
        [TestMethod]
        public void TheOrderTheWillAndTheDanceAreTakenInDoesNotChangeWhatTheDanceIsWorth()
        {
            var danceFirst = Dancer();
            danceFirst.Wear(DeathDance());
            danceFirst.Wear(IronWill());

            var willFirst = Dancer();
            willFirst.Wear(IronWill());
            willFirst.Wear(DeathDance());

            Assert.AreEqual(CriticalDamageBase, willFirst.Value(EntityParameter.CriticalDamage), Precision,
                "a dance taken after the conversion found a pool the conversion had already given away");
            Assert.AreEqual(danceFirst.Value(EntityParameter.CriticalDamage), willFirst.Value(EntityParameter.CriticalDamage),
                Precision, "the pair answers to the order it was taken in");
        }

        /// <summary>Nothing is left behind either way: refunding the conversion hands the pool back and the
        /// dance is worth what it always was, from that moment on.</summary>
        [TestMethod]
        public void RefundingTheWillGivesTheDanceItsStepsBack()
        {
            var carrier = Dancer();
            ISkill ironWill = IronWill();
            carrier.Wear(DeathDance());
            carrier.Wear(ironWill);

            carrier.TakeOff(ironWill);

            Assert.AreEqual(CriticalDamageBase + DanceGift, carrier.Value(EntityParameter.CriticalDamage), Precision,
                "the pool came back and the line paid per step of it did not");
            Assert.AreEqual(EvadeBase, carrier.Value(EntityParameter.Evade), Precision);
            Assert.AreEqual(ArmorBase, carrier.Value(EntityParameter.Armor), Precision, "the gift outlived the keystone");
        }

        // ---- Агностик + Сила духа --------------------------------------------------------------------
        //
        // The spirit is paid a share of the MAXIMUM mana pool and the agnostic converts that pool into
        // health, so the bearer keeps the price of the bargain and receives none of it.

        /// <summary>The pair as the owner reads it: five percent of a maximum that is now nothing is
        /// nothing, and the recovery the spirit charges for it is cut all the same — 100 stands at 75 while
        /// a blow that would have handed back 10 mana hands back none.</summary>
        [TestMethod]
        public void BesideTheAgnosticTheSpiritIsAllPriceAndNoReturn()
        {
            ConditionOwner bearer = Believer();
            IPassiveSkillsComponent roster = Wear(bearer, Spirit());
            roster.AddSkill(Agnostic());

            Strike(bearer);

            Assert.AreEqual(0f, bearer.Parameters.MaxMana, Precision,
                "the conversion left a pool behind and the walk proves nothing");
            Assert.AreEqual(MaxHealth + MaxMana, bearer.Parameters.MaxHealth, Precision,
                "the mana never arrived in the health pool");
            Assert.AreEqual(0f, bearer.CurrentMana, Precision, "a share of an emptied pool was paid out anyway");
            Assert.AreEqual(ManaRecoveryBase * (1 + SpiritRecoveryPenalty), bearer.Parameters.ManaRecovery, Precision,
                "the price of the bargain quietly left with its return");
        }

        /// <summary>Neither keystone waits for the other: taken in either order the pair settles the same,
        /// because the share is read off the pool at the moment the blow lands and not when the passive
        /// was attached.</summary>
        [TestMethod]
        public void TheOrderTheAgnosticAndTheSpiritAreTakenInDoesNotChangeWhatABlowReturns()
        {
            ConditionOwner spiritFirst = Believer();
            IPassiveSkillsComponent first = Wear(spiritFirst, Spirit());
            first.AddSkill(Agnostic());

            ConditionOwner agnosticFirst = Believer();
            IPassiveSkillsComponent second = Wear(agnosticFirst, Agnostic());
            second.AddSkill(Spirit());

            Strike(spiritFirst);
            Strike(agnosticFirst);

            Assert.AreEqual(0f, agnosticFirst.CurrentMana, Precision,
                "a spirit taken after the conversion found a pool the conversion had already given away");
            Assert.AreEqual(spiritFirst.CurrentMana, agnosticFirst.CurrentMana, Precision,
                "the pair answers to the order it was taken in");
        }

        /// <summary>The counterpart, so the zero above is the conversion and not a keystone that never pays:
        /// refund the agnostic and the very next blow is worth its documented five percent — 10 of 200.</summary>
        [TestMethod]
        public void RefundingTheAgnosticGivesTheSpiritItsPoolBackAndTheNextBlowPays()
        {
            ConditionOwner bearer = Believer();
            IPassiveSkillsComponent roster = Wear(bearer, Spirit());
            ISkill agnostic = Agnostic();
            roster.AddSkill(agnostic);

            Strike(bearer);
            Assert.AreEqual(0f, bearer.CurrentMana, Precision, "the conversion is not in force and the claim below proves nothing");

            roster.RemoveSkill(agnostic);
            Strike(bearer);

            Assert.AreEqual(MaxMana, bearer.Parameters.MaxMana, Precision, "the refunded conversion kept the pool");
            Assert.AreEqual(MaxMana * SpiritPercentOfMaxMana, bearer.CurrentMana, Precision,
                "the pool came back and the keystone paid out of it did not");
        }

        // ---- Стоицизм + уклонение-стак -------------------------------------------------------------------
        //
        // The stoic buys his immunity with the VERDICT of the evasion roll and never with the figure, so a
        // live stacking source of evasion is a source he keeps paying for and never wins on. The draw is
        // burnt whatever the verdict: a gate that skipped it would pull every later roll of the fight one
        // step forward, and the same seed would play out differently for no reason but what a defender wears.

        /// <summary>Three stacks of "Лёгкий шаг" on 8000 evasion is 12167 of it — better than an even chance
        /// against no accuracy — and no draw in the whole span wins the stoic a single evasion.</summary>
        [TestMethod]
        public async Task NoDrawWinsAStoicAnEvasionWhileHisStackedSourceIsLive()
        {
            ConditionOwner stoic = Dodger();
            await Stack(stoic);
            Wear(stoic, new StoicismPassiveSkill());

            Assert.AreEqual(StackedEvade, stoic.Parameters.Evade, 0.01f,
                "the stacking source is not on the fighter, so nothing below is measuring a denial");
            Assert.IsTrue(EvasionChance(StackedEvade) > 0.5f,
                "the stacked evasion is not worth an even chance, so a swing that lands proves nothing");

            for (int step = 0; step <= 20; step++)
            {
                float roll = step / 20f;

                Assert.AreEqual(AttackResults.Succeed, Resolve(stoic, new Draws(roll)),
                    $"a stoic with {LightStepStacks} stacks of evasion got away on a draw of {roll}");
            }
        }

        /// <summary>The same fighter without the keystone, so the sweep above is the denial and not a
        /// source that never worked: the lowest draw wins him the evasion outright.</summary>
        [TestMethod]
        public async Task TheSameStackedSourceEvadesForAnybodyButAStoic()
        {
            ConditionOwner dodger = Dodger();
            await Stack(dodger);

            Assert.AreEqual(AttackResults.Evaded, Resolve(dodger, new Draws(0f)),
                "the stacking source never won an evasion at all");
        }

        /// <summary>The discipline of the burnt draw, across the whole pair: the stoic, the same fighter
        /// without the keystone, and a fighter carrying no stacks at all each take exactly two draws off the
        /// fight's stream — the evasion roll and the block roll behind it.</summary>
        [TestMethod]
        public async Task TheStoicAndHisStackBurnTheSameDrawsAsAnybodyElse()
        {
            ConditionOwner stoic = Dodger();
            await Stack(stoic);
            Wear(stoic, new StoicismPassiveSkill());

            ConditionOwner stacked = Dodger();
            await Stack(stacked);

            ConditionOwner bare = Dodger();

            var denied = new Draws(1f);
            var evading = new Draws(1f);
            var plain = new Draws(1f);

            Assert.AreEqual(AttackResults.Succeed, Resolve(stoic, denied));
            Assert.AreEqual(AttackResults.Succeed, Resolve(stacked, evading));
            Assert.AreEqual(AttackResults.Succeed, Resolve(bare, plain));

            Assert.AreEqual(2, plain.Taken, "the resolve stopped burning the evade and block rolls");
            Assert.AreEqual(plain.Taken, evading.Taken, "a stack of evasion shifted the stream behind the evasion roll");
            Assert.AreEqual(plain.Taken, denied.Taken,
                "the stoic's denial shifted the stream — what a defender wears may not decide anybody's rolls");
        }

        /// <summary>The denial is read at the moment of the roll: refund the keystone and the stacks the
        /// bearer never stopped carrying win him the very next swing.</summary>
        [TestMethod]
        public async Task RefundingTheKeystoneHandsTheStacksBackTheirVerdictOnTheNextSwing()
        {
            ConditionOwner stoic = Dodger();
            await Stack(stoic);
            ISkill stoicism = new StoicismPassiveSkill();
            IPassiveSkillsComponent roster = Wear(stoic, stoicism);

            Assert.AreEqual(AttackResults.Succeed, Resolve(stoic, new Draws(0f)));

            roster.RemoveSkill(stoicism);

            Assert.AreEqual(AttackResults.Evaded, Resolve(stoic, new Draws(0f)),
                "the price outlived the keystone that charged it");
            Assert.AreEqual(StackedEvade, stoic.Parameters.Evade, 0.01f, "the refund reached the figure instead of the verdict");
        }

        // ---- fixtures ------------------------------------------------------------------------------------

        /// <summary>What three stacks of the shipped "Лёгкий шаг" make of <see cref="StoicEvadeBase"/>: the
        /// share is gained per stack and the stacks multiply, so it is 1.15 to the third.</summary>
        private static float StackedEvade => StoicEvadeBase * MathF.Pow(1 + LightStepShare, LightStepStacks);

        private static float EvasionChance(float evade) => evade / (evade + EvasionScalingFactor);

        private static KeystoneCarrier Dancer()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.Evade, EvadeBase);
            carrier.Set(EntityParameter.Armor, ArmorBase);
            carrier.Set(EntityParameter.CriticalDamage, CriticalDamageBase);

            return carrier;
        }

        private static ConditionOwner Believer()
        {
            var bearer = new ConditionOwner();
            bearer.SetMaximum(EntityParameter.Mana, MaxMana);
            bearer.SetMaximum(EntityParameter.Health, MaxHealth);
            bearer.SetMaximum(EntityParameter.ManaRecovery, ManaRecoveryBase);
            bearer.CurrentMana = 0f;

            return bearer;
        }

        private static ConditionOwner Dodger()
        {
            var bearer = new ConditionOwner();
            bearer.SetMaximum(EntityParameter.Health, MaxHealth);
            bearer.CurrentHealth = MaxHealth;
            bearer.SetMaximum(EntityParameter.Evade, StoicEvadeBase);

            return bearer;
        }

        /// <summary>The keystone as the registry builds it — the road a node's grant actually travels.</summary>
        private static ISkill IronWill() => Build(IronWillId, []);

        /// <summary>The dance as its tree node writes it: a stat record read through the open family, so
        /// what is worn here is the shipped shape of the keystone and not a modifier invented for a walk.</summary>
        private static ISkill DeathDance() =>
            Build(DeathDanceId, new Dictionary<string, float> { [DeathDanceField] = DeathDanceValue });

        private static ISkill Agnostic() => new AgnosticPassiveSkill(AgnosticCostScale);

        private static ISkill Spirit() =>
            new StrengthOfSpiritPassiveSkill(Always, SpiritPercentOfMaxMana, SpiritRecoveryPenalty);

        private static ISkill Build(string id, Dictionary<string, float> properties)
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(id, new RecordProperties(id, properties));

            Assert.IsNotNull(skill, $"the registry does not build '{id}'");
            return skill;
        }

        /// <summary>The roster a fighter's passives actually live on, so wearing and refunding here is the
        /// attach the game makes and not a call written for the walk.</summary>
        private static IPassiveSkillsComponent Wear(ConditionOwner bearer, ISkill skill)
        {
            var roster = new PassiveSkillsComponent(bearer);
            roster.AddSkill(skill);

            return roster;
        }

        /// <summary>Lays the shipped stacking evasion buff on the fighter, stack by stack, through the
        /// effect's own application — the ceiling, the stacking rules and the decorator are all the game's.</summary>
        private static async Task Stack(ConditionOwner bearer)
        {
            for (int stack = 0; stack < LightStepStacks; stack++)
                await new LightStep(duration: 3, maxStacks: LightStepStacks, value: LightStepShare)
                    .Apply(new EffectApplyingContext { Caster = bearer, Target = bearer, Source = "Test_Light_Step" });
        }

        private static void Strike(ConditionOwner bearer)
        {
            var blow = new DamageContext { Source = new ConditionOwner(), Cause = DamageCause.Attack };
            blow.Add(DamageType.Physical, 25f);

            bearer.CombatEvents.Publish(new DamageTakenEvent(blow, bearer, VitalsSnapshot.From(bearer)));
        }

        /// <summary>One swing at the fighter, settled on a scripted stream: no accuracy behind it and
        /// neither mark set, so what decides it is the defender alone.</summary>
        private static AttackResults Resolve(IFightable defender, Draws draws)
        {
            var swing = new Mock<IAttackContext>();
            swing.SetupGet(attack => attack.Target).Returns(defender);
            swing.SetupGet(attack => attack.RawAccuracy).Returns(0f);
            swing.SetupGet(attack => attack.IsUnevadable).Returns(false);
            swing.SetupGet(attack => attack.IsUnblockable).Returns(false);

            return Calculations.ResolveAttackOutcome(swing.Object, draws.Next);
        }

        /// <summary>One draw handed out as often as it is asked for, counting the asks: which rolls a resolve
        /// burns is part of the contract — a verdict a keystone gates must not shift anybody else's stream.</summary>
        private sealed class Draws(float roll)
        {
            public int Taken { get; private set; }

            public float Next()
            {
                Taken++;
                return roll;
            }
        }
    }
}
