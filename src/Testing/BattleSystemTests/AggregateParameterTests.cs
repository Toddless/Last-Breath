namespace LastBreathTest.BattleSystemTests
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
    }
}
