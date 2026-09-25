namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>
    /// "Ярость стихий": the bearer's attacks pierce an elemental resistance by exactly the reserve he
    /// himself carries above his own cap for it, and every resistance he owns is cut in return.
    /// <para>Element by element and never pooled — the cold reserve pierces cold and nothing else — and the
    /// reserve is the one <see cref="ResistanceParameters"/> counts, so this keystone, resistance shred and
    /// the keystone paid per unit of reserve are reading a single figure.</para>
    /// <para>The loop is deliberate: the price cuts the very totals the reserves are measured from, so the
    /// keystone is read AFTER it has paid for itself.</para>
    /// </summary>
    [TestClass]
    public class ElementalFuryPassiveTests
    {
        /// <summary>What the bargain costs every resistance, multiplicatively.</summary>
        private const float Price = -0.45f;

        /// <summary>The cap every fighter is born with.</summary>
        private const float DefaultMaximum = ResistanceParameters.DefaultMaximum;

        private const float Blow = 100f;
        private const float Precision = 0.0001f;

        // ---- the full circle ---------------------------------------------------------------------------

        /// <summary>The whole bargain in one line of numbers: 218% of cold answers the price and stands at
        /// 119.9%, the cap takes 75 of that, and the 44.9 left over is what an attack pierces.</summary>
        [TestMethod]
        public void ThePriceIsPaidFirstAndWhatIsLeftOverTheCapIsWhatAnAttackPierces()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, 2.18f);

            carrier.Wear(Fury());

            Assert.AreEqual(1.199f, carrier.Value(EntityParameter.ColdResistance), Precision,
                "the price is a multiplicative −45% of the total");
            Assert.AreEqual(0.449f, Pierced(carrier, EntityParameter.ColdResistancePenetration), Precision,
                "an attack must pierce the reserve that survived the price and nothing else");
        }

        /// <summary>The reserve and not what mitigates: reading the effective value instead would hand the
        /// same carrier the whole 75% cap to pierce with rather than the 44.9 he has over it.</summary>
        [TestMethod]
        public void TheReserveIsCountedFromTheTotalAndNeverFromWhatMitigates()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, 2.18f);
            carrier.Wear(Fury());

            float pierced = Pierced(carrier, EntityParameter.ColdResistancePenetration);

            Assert.AreEqual(ResistanceParameters.Overcap(carrier.Parameters, EntityParameter.ColdResistance), pierced, Precision,
                "the keystone and the resistance rules may not hold two answers to one question");
            Assert.AreEqual(DefaultMaximum, ResistanceParameters.Effective(carrier.Parameters, EntityParameter.ColdResistance), Precision,
                "what mitigates is the cap itself — piercing with THAT would hand the carrier 75 points instead of the 44.9 he owns");
        }

        // ---- element by element ------------------------------------------------------------------------

        /// <summary>Three elements of one carrier, in three different states after the price: cold stands 45
        /// points over the cap, fire 5, and lightning does not reach it. Each is pierced by its own reserve
        /// and by nothing that belongs to another element.</summary>
        [TestMethod]
        public void EachElementIsPiercedByItsOwnReserveAndNeverByAnother()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, BeforeThePrice(1.20f));
            carrier.Set(EntityParameter.FireResistance, BeforeThePrice(0.80f));
            carrier.Set(EntityParameter.LightningResistance, BeforeThePrice(0.60f));

            carrier.Wear(Fury());
            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));

            Assert.AreEqual(0.45f, blow.ResistancePenetrationOf(EntityParameter.ColdResistancePenetration), Precision,
                "120% of cold stands 45 points over the cap");
            Assert.AreEqual(0.05f, blow.ResistancePenetrationOf(EntityParameter.FireResistancePenetration), Precision,
                "80% of fire stands 5 points over the cap — a reserve of another element must not reach it");
            Assert.AreEqual(0f, blow.ResistancePenetrationOf(EntityParameter.LightningResistancePenetration), Precision,
                "60% of lightning holds no reserve at all, so lightning is pierced by nothing");
        }

        /// <summary>Poison is not an element the keystone pierces — it pays the price like the rest and
        /// gives nothing back.</summary>
        [TestMethod]
        public void PoisonIsPaidForAndNeverPierced()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.PoisonResistance, 2f);

            carrier.Wear(Fury());

            Assert.AreEqual(1.1f, carrier.Value(EntityParameter.PoisonResistance), Precision, "poison pays the price too");
            Assert.AreEqual(0f, Pierced(carrier, EntityParameter.PoisonResistancePenetration), Precision,
                "the keystone names the elements, and poison is not one of them");
        }

        // ---- the price ---------------------------------------------------------------------------------

        [TestMethod]
        public void EveryResistanceIsCut()
        {
            var carrier = new KeystoneCarrier();
            foreach (EntityParameter resistance in Resistances) carrier.Set(resistance, 1f);

            carrier.Wear(Fury());

            foreach (EntityParameter resistance in Resistances)
                Assert.AreEqual(1f + Price, carrier.Value(resistance), Precision,
                    $"{resistance} was left out of the bargain the keystone charges for");
        }

        // ---- the living measure ------------------------------------------------------------------------

        /// <summary>Read on the blow: a resistance raised after the keystone was taken is pierced with the
        /// same turn, without the passive being re-attached.</summary>
        [TestMethod]
        public void MoreResistanceIsPiercedWithTheSameTurn()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, BeforeThePrice(1.20f));
            carrier.Wear(Fury());

            carrier.Set(EntityParameter.ColdResistance, BeforeThePrice(1.50f));

            Assert.AreEqual(0.75f, Pierced(carrier, EntityParameter.ColdResistancePenetration), Precision,
                "150% cut to the cap leaves 75 points of reserve, and the next attack must carry them");
        }

        // ---- what mitigation makes of it ---------------------------------------------------------------

        /// <summary>End to end: 45 points of reserve take 45% off a defender's cold resistance, so a 100-cold
        /// blow against a fully capped defender lands for 58.75 instead of 25.</summary>
        [TestMethod]
        public void WhatIsPiercedComesOffTheDefendersResistance()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, BeforeThePrice(1.20f));
            carrier.Wear(Fury());

            var defender = Defender();
            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Cold, Blow));
            Calculations.CalculateMitigation(blow, defender.Fighter, new DefaultRandomNumberGenerator(seed: 1));

            Assert.AreEqual(Blow * (1 - (DefaultMaximum * (1 - 0.45f))), blow.DamageComponents[DamageType.Cold], Precision,
                "the reserve was not taken off the defender's resistance");
        }

        /// <summary>A reserve wider than any resistance still only takes the resistance away: past that,
        /// piercing would turn mitigation into amplification.</summary>
        [TestMethod]
        public void AReserveWiderThanTheResistanceNeverAmplifiesTheBlow()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, BeforeThePrice(3f));
            carrier.Wear(Fury());

            var defender = Defender();
            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Cold, Blow));
            Calculations.CalculateMitigation(blow, defender.Fighter, new DefaultRandomNumberGenerator(seed: 1));

            Assert.AreEqual(Blow, blow.DamageComponents[DamageType.Cold], Precision,
                "an overfed reserve was paid a hit worth more than it was made of");
        }

        // ---- what the bargain does not reach -----------------------------------------------------------

        /// <summary>Attacks are what the bargain names: what an ability deals itself meets the defender's
        /// resistances whole.</summary>
        [TestMethod]
        public void WhatAnAbilityDealsItselfPiercesNothing()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, BeforeThePrice(1.20f));
            carrier.Wear(Fury());

            IDamageContext blow = carrier.Dealt(DamageCause.Ability, (DamageType.Cold, Blow));

            Assert.AreEqual(0f, blow.ResistancePenetrationOf(EntityParameter.ColdResistancePenetration), Precision);
        }

        [TestMethod]
        public void SomebodyElsesAttackIsUntouched()
        {
            var bearer = new KeystoneCarrier();
            bearer.Set(EntityParameter.ColdResistance, BeforeThePrice(1.20f));
            bearer.Wear(Fury());

            var stranger = new KeystoneCarrier();
            var blow = new DamageContext { Source = stranger.Fighter, Cause = DamageCause.Attack };
            blow.Add(DamageType.Cold, Blow);
            bearer.Handler.Apply(blow);

            Assert.AreEqual(0f, blow.ResistancePenetrationOf(EntityParameter.ColdResistancePenetration), Precision,
                "the reserve was not gated on whose blow it is");
        }

        [TestMethod]
        public void TheKeystoneLeavesNothingBehind()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.ColdResistance, 2f);
            ISkill fury = Fury();

            carrier.Wear(fury);
            carrier.TakeOff(fury);

            Assert.AreEqual(2f, carrier.Value(EntityParameter.ColdResistance), Precision, "the price outlived the keystone");
            Assert.AreEqual(0f, Pierced(carrier, EntityParameter.ColdResistancePenetration), Precision,
                "a refunded keystone kept piercing");
        }

        // ---- beside Vicious Bite -----------------------------------------------------------------------

        /// <summary>The two keystones paid per reserve read ONE figure, so the price of this one is felt by
        /// the other: −45% on every resistance beside −60% on poison leaves the poison total at nothing, the
        /// reserve at nothing, and the bite paid nothing.</summary>
        [TestMethod]
        public void ThePriceEatsTheReserveViciousBiteIsPaidFor()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.PoisonResistance, 2f);
            carrier.Wear(new ViciousBitePassiveSkill(0.01f, -0.6f));

            Assert.AreEqual(0.05f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision,
                "200% cut to 80% stands five points over the cap");

            carrier.Wear(Fury());

            Assert.AreEqual(0f, carrier.Value(EntityParameter.PoisonResistance), Precision,
                "two multiplicative prices are summed, and −105% of a total leaves nothing of it");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision,
                "the bite must be paid off the total the fury left, not off a second reading of its own");
        }

        // ---- the registration --------------------------------------------------------------------------

        [TestMethod]
        public void TheRegistryBuildsTheKeystoneFromItsField()
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(ElementalFuryPassiveSkill.PassiveId,
                new RecordProperties(ElementalFuryPassiveSkill.PassiveId,
                    new Dictionary<string, float> { ["resistancePenalty"] = Price }));

            Assert.IsInstanceOfType<ElementalFuryPassiveSkill>(skill, "the registry does not answer to the keystone's id");
        }

        private static EntityParameter[] Resistances =>
        [
            EntityParameter.FireResistance, EntityParameter.ColdResistance,
            EntityParameter.LightningResistance, EntityParameter.PoisonResistance
        ];

        /// <summary>The total a resistance has to start from to stand at <paramref name="after"/> once the
        /// keystone has taken its share — the tests name the state the bargain leaves, not the one it finds.</summary>
        private static float BeforeThePrice(float after) => after / (1 + Price);

        /// <summary>A fighter standing at the cap in every element — the resistance a reserve is measured
        /// against when a blow lands.</summary>
        private static KeystoneCarrier Defender()
        {
            var defender = new KeystoneCarrier();
            foreach (EntityParameter resistance in Resistances) defender.Set(resistance, DefaultMaximum);

            return defender;
        }

        /// <summary>What one attack of the carrier pierces of a resistance.</summary>
        private static float Pierced(KeystoneCarrier carrier, EntityParameter penetration) =>
            carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow)).ResistancePenetrationOf(penetration);

        private static ISkill Fury() => new ElementalFuryPassiveSkill(Price);
    }
}
