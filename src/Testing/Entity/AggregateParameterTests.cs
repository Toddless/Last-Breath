namespace LastBreathTest.Entity
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>Aggregate ("all X") parameters fold into every family member at resolution and fan change
    /// events out to the members — the aggregate itself is a bucket, never read or raised directly.</summary>
    [TestClass]
    public class AggregateParameterTests
    {
        [TestMethod]
        public void GetModifiers_ForEveryFamilyMember_FoldsTheAggregateBucket()
        {
            var component = new ParameterModifiersComponent();
            var allResistances = new SimpleModifier(EntityParameter.AllResistance, ModifierValueType.Flat, 0.35f, "test");

            component.AddModifier(allResistances);

            foreach (var member in new[]
                     {
                         EntityParameter.FireResistance, EntityParameter.ColdResistance,
                         EntityParameter.LightningResistance, EntityParameter.PoisonResistance
                     })
                Assert.IsTrue(component.GetModifiers(member).Any(modifier => modifier.InstanceId == allResistances.InstanceId),
                    $"{member} should fold in the AllResistance bucket");
        }

        /// <summary>"All resistances" covers poison too — it is a resistance like the elemental three,
        /// so neither the bucket nor its penetration sibling may quietly skip it.</summary>
        [TestMethod]
        public void GetModifiers_AllResistancePenetration_CoversPoison()
        {
            var component = new ParameterModifiersComponent();
            var allPenetration = new SimpleModifier(EntityParameter.AllResistancePenetration, ModifierValueType.Flat, 0.2f, "test");

            component.AddModifier(allPenetration);

            foreach (var member in new[]
                     {
                         EntityParameter.FireResistancePenetration, EntityParameter.ColdResistancePenetration,
                         EntityParameter.LightningResistancePenetration, EntityParameter.PoisonResistancePenetration
                     })
                Assert.IsTrue(component.GetModifiers(member).Any(modifier => modifier.InstanceId == allPenetration.InstanceId),
                    $"{member} should fold in the AllResistancePenetration bucket");
        }

        [TestMethod]
        public void GetModifiers_ForUnrelatedParameter_DoesNotFoldAggregate()
        {
            var component = new ParameterModifiersComponent();
            var allResistances = new SimpleModifier(EntityParameter.AllResistance, ModifierValueType.Flat, 0.35f, "test");

            component.AddModifier(allResistances);

            Assert.IsFalse(component.GetModifiers(EntityParameter.Health).Any(modifier => modifier.InstanceId == allResistances.InstanceId));
        }

        [TestMethod]
        public void AddModifier_OnAggregate_RaisesForEachMemberAndNotTheAggregate()
        {
            var component = new ParameterModifiersComponent();
            var raised = new List<EntityParameter>();
            component.ModifiersChanged += (_, args) => raised.Add(args.EntityParameter);

            component.AddModifier(new SimpleModifier(EntityParameter.AllDefence, ModifierValueType.Flat, 100f, "test"));

            CollectionAssert.AreEquivalent(new[] { EntityParameter.Evade, EntityParameter.Armor }, raised);
            Assert.IsFalse(raised.Contains(EntityParameter.AllDefence), "The aggregate parameter must never raise directly");
        }

        /// <summary>The families themselves, written out. A bucket declared on the enum but left out of the
        /// map takes modifiers nobody ever reads, and a member quietly dropped from a family stops being
        /// paid — neither shows anywhere else, so the composition is pinned here.</summary>
        [TestMethod]
        public void EveryAggregate_StandsForTheFamilyWrittenHere()
        {
            var families = new Dictionary<EntityParameter, EntityParameter[]>
            {
                [EntityParameter.AllResistance] =
                [
                    EntityParameter.FireResistance, EntityParameter.ColdResistance,
                    EntityParameter.LightningResistance, EntityParameter.PoisonResistance
                ],
                [EntityParameter.AllAttribute] = [EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Intelligence],
                [EntityParameter.AllDefence] = [EntityParameter.Evade, EntityParameter.Armor],
                [EntityParameter.AllResistancePenetration] =
                [
                    EntityParameter.FireResistancePenetration, EntityParameter.ColdResistancePenetration,
                    EntityParameter.LightningResistancePenetration, EntityParameter.PoisonResistancePenetration
                ],
                [EntityParameter.AllResistanceMaximum] =
                [
                    EntityParameter.FireResistanceMaximum, EntityParameter.ColdResistanceMaximum,
                    EntityParameter.LightningResistanceMaximum, EntityParameter.PoisonResistanceMaximum
                ],
                [EntityParameter.AllDoTDamageMultiplier] =
                [
                    EntityParameter.PoisonDamageMultiplier, EntityParameter.BurningDamageMultiplier,
                    EntityParameter.BleedDamageMultiplier
                ],
                [EntityParameter.AllElementalDamage] = [EntityParameter.FireDamage, EntityParameter.ColdDamage, EntityParameter.LightningDamage],
                [EntityParameter.Damage] =
                [
                    EntityParameter.PhysicalDamage, EntityParameter.FireDamage, EntityParameter.ColdDamage,
                    EntityParameter.LightningDamage, EntityParameter.SpellDamage
                ]
            };

            CollectionAssert.AreEquivalent(families.Keys.ToArray(),
                Enum.GetValues<EntityParameter>().Where(AggregateParameters.IsAggregate).ToArray(),
                "the set of buckets moved — a bucket with no family folds into nobody and says nothing about why");

            foreach ((EntityParameter aggregate, EntityParameter[] members) in families)
                CollectionAssert.AreEquivalent(members, AggregateParameters.Members(aggregate).ToArray(), $"the family of '{aggregate}' moved");
        }

        /// <summary>No family holds another bucket: the fold expands an aggregate exactly one step, so a
        /// nested one would hand its modifiers to nobody while looking like it works.</summary>
        [TestMethod]
        public void NoFamily_HoldsAnotherAggregate()
        {
            var nested = Enum.GetValues<EntityParameter>()
                .Where(AggregateParameters.IsAggregate)
                .SelectMany(aggregate => AggregateParameters.Members(aggregate))
                .Where(AggregateParameters.IsAggregate)
                .ToList();

            Assert.AreEqual(0, nested.Count, $"a bucket sits inside a family: {string.Join(", ", nested)}");
        }

        /// <summary>Elemental damage answers to two buckets at once — "all elemental" and "all damage".
        /// Each has to arrive exactly once: a fold counted per family a parameter belongs to would pay the
        /// wider bucket twice, and paying it not at all is the older bug the map exists to prevent.</summary>
        [TestMethod]
        public void GetModifiers_ForAMemberOfTwoFamilies_FoldsEachBucketExactlyOnce()
        {
            var component = new ParameterModifiersComponent();
            var allDamage = new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, 10f, "damage");
            var allElemental = new SimpleModifier(EntityParameter.AllElementalDamage, ModifierValueType.Flat, 5f, "elemental");

            component.AddModifier(allDamage);
            component.AddModifier(allElemental);

            IReadOnlyList<IModifierInstance> folded = component.GetModifiers(EntityParameter.FireDamage);
            Assert.AreEqual(1, folded.Count(modifier => modifier.InstanceId == allDamage.InstanceId), "the wider bucket reached fire damage twice or not at all");
            Assert.AreEqual(1, folded.Count(modifier => modifier.InstanceId == allElemental.InstanceId), "the elemental bucket reached fire damage twice or not at all");
            Assert.AreEqual(0, component.GetModifiers(EntityParameter.PhysicalDamage).Count(modifier => modifier.InstanceId == allElemental.InstanceId),
                "the elemental bucket paid the physical part of the blow");
        }

        /// <summary>The same claim on the resolved number, through the pair of components a fighter wears:
        /// both buckets land on the value once, and the narrower one is not spent on a parameter outside it.</summary>
        [TestMethod]
        public void ResolvedValue_OfAMemberOfTwoFamilies_CarriesBothBucketsOnce()
        {
            var modifiers = new ParameterModifiersComponent();
            var parameters = new EntityParametersComponent();
            parameters.Initialize(modifiers.GetModifiers);
            modifiers.ModifiersChanged += parameters.OnParameterModifiersChange;
            parameters.SetBaseValueForParameter(EntityParameter.FireDamage, 100f);

            modifiers.AddModifier(new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, 10f, "damage"));
            modifiers.AddModifier(new SimpleModifier(EntityParameter.AllElementalDamage, ModifierValueType.Flat, 5f, "elemental"));

            Assert.AreEqual(115f, parameters.GetValueForParameter(EntityParameter.FireDamage), 0.0001f,
                "fire damage does not carry both buckets exactly once");
            Assert.AreEqual(10f, parameters.GetValueForParameter(EntityParameter.PhysicalDamage), 0.0001f,
                "the physical part took the elemental bucket with it");
        }

        /// <summary>A bucket over a member of two families still raises each member once and never itself:
        /// the fan-out expands one step and does not walk back up through the other family.</summary>
        [TestMethod]
        public void AddModifier_OnTheWidestBucket_RaisesEveryMemberOnce()
        {
            var component = new ParameterModifiersComponent();
            var raised = new List<EntityParameter>();
            component.ModifiersChanged += (_, args) => raised.Add(args.EntityParameter);

            component.AddModifier(new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, 10f, "test"));

            CollectionAssert.AreEquivalent(
                new[]
                {
                    EntityParameter.PhysicalDamage, EntityParameter.FireDamage, EntityParameter.ColdDamage,
                    EntityParameter.LightningDamage, EntityParameter.SpellDamage
                },
                raised, "the widest bucket did not reach every member exactly once");
        }
    }
}
