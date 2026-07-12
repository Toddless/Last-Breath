namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Components;
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

            foreach (var member in new[] { EntityParameter.FireResistance, EntityParameter.ColdResistance, EntityParameter.LightningResistance })
                Assert.IsTrue(component.GetModifiers(member).Any(modifier => modifier.InstanceId == allResistances.InstanceId),
                    $"{member} should fold in the AllResistance bucket");
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
