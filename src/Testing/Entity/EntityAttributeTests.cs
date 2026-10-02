namespace LastBreathTest.Entity
{
    using System;
    using System.Linq;
    using Core.Entity.Attribute;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>Every attribute grants exactly one parameter and stamps every modifier it mints with its
    /// own non-empty source, so removal by source drops the contribution of one attribute and nothing else.</summary>
    [TestClass]
    public class EntityAttributeTests
    {
        /// <summary>Parameters attributes used to feed before the design cut — nothing may reach them from
        /// an attribute again (a silent return of the old behaviour is a balance regression).</summary>
        private static readonly EntityParameter[] s_droppedParameters =
        [
            EntityParameter.PhysicalDamage, EntityParameter.Armor, EntityParameter.HealthRecovery,
            EntityParameter.CriticalChance, EntityParameter.CriticalDamage, EntityParameter.AdditionalHitChance,
            EntityParameter.Barrier, EntityParameter.SpellDamage, EntityParameter.ManaRecovery
        ];

        [TestMethod]
        public void Strength_GrantsHealthAndNothingElse() =>
            AssertGrantsOnly(component => new Strength(component), EntityParameter.Strength, EntityParameter.Health);

        [TestMethod]
        public void Dexterity_GrantsEvadeAndNothingElse() =>
            AssertGrantsOnly(component => new Dexterity(component), EntityParameter.Dexterity, EntityParameter.Evade);

        [TestMethod]
        public void Intelligence_GrantsManaAndNothingElse() =>
            AssertGrantsOnly(component => new Intelligence(component), EntityParameter.Intelligence, EntityParameter.Mana);

        [TestMethod]
        public void RemoveModifierBySource_OfOneAttribute_KeepsTheOtherAttributes()
        {
            var component = new ParameterModifiersComponent();
            var strength = Grown(new Strength(component));
            var dexterity = Grown(new Dexterity(component));
            var intelligence = Grown(new Intelligence(component));

            CollectionAssert.AllItemsAreUnique(new[] { SourceOf(strength), SourceOf(dexterity), SourceOf(intelligence) },
                "attributes must not share a modifier source");

            component.RemoveModifierBySource(SourceOf(strength));

            Assert.IsFalse(component.EntityModifiers.ContainsKey(EntityParameter.Health), "Strength's grant is gone");
            Assert.IsFalse(component.EntityModifiers.ContainsKey(EntityParameter.Strength), "Strength's own bucket is gone");
            Assert.IsTrue(component.EntityModifiers.ContainsKey(EntityParameter.Evade), "Dexterity keeps its grant");
            Assert.IsTrue(component.EntityModifiers.ContainsKey(EntityParameter.Mana), "Intelligence keeps its grant");
        }

        [TestMethod]
        public void RemoveModifierBySource_WithAnEmptySource_RemovesNothing()
        {
            var component = new ParameterModifiersComponent();
            Grown(new Strength(component));
            Grown(new Dexterity(component));
            Grown(new Intelligence(component));
            int before = TotalModifiers(component);

            component.RemoveModifierBySource(string.Empty);

            Assert.AreEqual(before, TotalModifiers(component), "an empty source addresses nothing");
        }

        [TestMethod]
        public void AttributeGrant_ScalesLinearlyWithTotal()
        {
            var component = new ParameterModifiersComponent();
            var strength = new Strength(component);

            strength.Total = 1;
            float single = component.EntityModifiers[EntityParameter.Health].Single().Value;
            strength.Total = 10;
            float tenfold = component.EntityModifiers[EntityParameter.Health].Single().Value;

            Assert.IsTrue(single > 0f, "one point of an attribute must give something");
            Assert.AreEqual(single * 10f, tenfold, 0.0001f, "the grant is linear in the attribute value");
        }

        [TestMethod]
        public void RepeatedUpdates_DoNotDuplicateTheAttributeModifiers()
        {
            var component = new ParameterModifiersComponent();
            var dexterity = new Dexterity(component);

            for (int total = 1; total <= 5; total++) dexterity.Total = total;

            Assert.AreEqual(1, component.EntityModifiers[EntityParameter.Evade].Count, "the grant is updated in place, never re-added");
            Assert.AreEqual(1, component.EntityModifiers[EntityParameter.Dexterity].Count, "the attribute's own bucket is updated in place");
        }

        private static void AssertGrantsOnly(Func<IParameterModifiersComponent, IEntityAttribute> create, EntityParameter attributeParameter, EntityParameter granted)
        {
            var component = new ParameterModifiersComponent();
            var attribute = Grown(create(component));

            Assert.AreEqual(1, attribute.Modifiers.Count, $"{attributeParameter} must grant exactly one parameter");
            Assert.AreEqual(granted, attribute.Modifiers.First().EntityParameter, $"{attributeParameter} must grant {granted}");
            Assert.IsFalse(string.IsNullOrEmpty(SourceOf(attribute)), $"{attributeParameter} must mint its modifiers with a named source");

            CollectionAssert.AreEquivalent(new[] { attributeParameter, granted }, ParametersFrom(component, SourceOf(attribute)),
                $"{attributeParameter} touches its own parameter and its single grant, nothing else");

            foreach (var dropped in s_droppedParameters)
                Assert.IsFalse(component.EntityModifiers.ContainsKey(dropped), $"{attributeParameter} must not feed {dropped} anymore");
        }

        private static EntityParameter[] ParametersFrom(IParameterModifiersComponent component, string source) =>
            component.EntityModifiers.Where(entry => entry.Value.Any(modifier => modifier.Source == source))
                .Select(entry => entry.Key).ToArray();

        private static int TotalModifiers(IParameterModifiersComponent component) =>
            component.EntityModifiers.Sum(entry => entry.Value.Count);

        private static string SourceOf(IEntityAttribute attribute) => attribute.Modifiers.First().Source;

        private static T Grown<T>(T attribute) where T : IEntityAttribute
        {
            attribute.Total = 1;
            return attribute;
        }
    }
}
