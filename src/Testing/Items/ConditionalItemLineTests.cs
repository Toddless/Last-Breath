namespace LastBreathTest.Items
{
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Core.Save;

    /// <summary>A line an item was rolled with may be held up by a condition. It reaches the wearer the way
    /// every item line does — he pulls it while resolving a value — so the whole of the feature is: it counts
    /// while the predicate holds, the flip reaches the parameter, and taking the item off leaves nothing
    /// behind on the fighter.</summary>
    [TestClass]
    public class ConditionalItemLineTests
    {
        private const float MaxHealth = 100f;
        private const float BaseArmor = 100f;
        private const float Bonus = 0.2f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void AWornConditionalLine_CountsOnlyWhileItsConditionHolds()
        {
            var owner = Fighter();
            var item = ItemWearing(Conditional(EntityParameter.Armor, ModifierValueType.Increase, Bonus));
            Wear(owner, item);

            Assert.AreEqual(BaseArmor, owner.Parameters.Armor, Tolerance, "the line counted on a fighter at full health");

            owner.CurrentHealth = MaxHealth * 0.5f;
            Assert.AreEqual(BaseArmor * (1f + Bonus), owner.Parameters.Armor, Tolerance, "the flip never reached the parameter");

            owner.CurrentHealth = MaxHealth;
            Assert.AreEqual(BaseArmor, owner.Parameters.Armor, Tolerance, "the line kept counting after its condition let go");
        }

        [TestMethod]
        public void TakingTheItemOff_StopsTheLineAndReleasesItsPredicate()
        {
            var owner = Fighter();
            var item = ItemWearing(Conditional(EntityParameter.Armor, ModifierValueType.Increase, Bonus));
            var equipment = Wear(owner, item);
            owner.CurrentHealth = MaxHealth * 0.5f;

            equipment.TryUnequip(EquipmentPiece.Body, out _);
            owner.CurrentHealth = MaxHealth * 0.1f;

            Assert.AreEqual(BaseArmor, owner.Parameters.Armor, Tolerance, "the line of an item nobody wears still counted");
            Assert.AreEqual(0, owner.ListenerCount, "the unequipped line left its predicate listening to the fighter");
        }

        [TestMethod]
        public void AWornConditionalLocalLine_StaysOutOfTheFoldedNumberWhileItIsOff()
        {
            // A local line is folded into ONE number before it leaves the item, so nothing downstream can
            // weigh its condition — the item has to.
            var owner = Fighter();
            var local = Conditional(EntityParameter.Armor, ModifierValueType.Increase, Bonus);
            local.Scope = ModifierScope.Local;
            var item = ItemWearing(local);
            item.SetBaseStats([new KeyValuePair<EntityParameter, float>(EntityParameter.Armor, BaseArmor)]);
            Wear(owner, item);

            Assert.AreEqual(BaseArmor * 2f, owner.Parameters.Armor, Tolerance, "base armor of the fighter plus the item's own");

            owner.CurrentHealth = MaxHealth * 0.5f;
            Assert.AreEqual(BaseArmor + (BaseArmor * (1f + Bonus)), owner.Parameters.Armor, Tolerance, "the local line never joined the fold");

            owner.CurrentHealth = MaxHealth;
            Assert.AreEqual(BaseArmor * 2f, owner.Parameters.Armor, Tolerance, "the local line stayed in the fold after its condition let go");
        }

        [TestMethod]
        public void CopyingAnItem_GivesTheCopyAPredicateOfItsOwn()
        {
            var item = ItemWearing(Conditional(EntityParameter.Armor, ModifierValueType.Increase, Bonus));

            var copy = item.Copy<IEquipItem>();

            var original = (SimpleModifier)item.Modifiers.Single();
            var copied = (SimpleModifier)copy.Modifiers.Single();
            Assert.AreEqual(TestConditions.Wounded, copied.ConditionId);
            Assert.IsNotNull(copied.Condition);
            Assert.AreNotSame(original.Condition, copied.Condition, "both items point one predicate at their own wearer");
        }

        [TestMethod]
        public void ASavedConditionalLine_ComesBackHeldUpByTheSameCondition()
        {
            var item = ItemWearing(Conditional(EntityParameter.Armor, ModifierValueType.Increase, Bonus));

            var restored = Converter(TestConditions.Holding()).FromData(Converter(TestConditions.Holding()).ToData(item));

            var line = (SimpleModifier)restored.Modifiers.Single();
            Assert.AreEqual(TestConditions.Wounded, line.ConditionId);
            Assert.IsNotNull(line.Condition, "the restored line was rebuilt without the predicate that gates it");

            var owner = Fighter();
            Wear(owner, restored);
            Assert.AreEqual(BaseArmor, owner.Parameters.Armor, Tolerance);

            owner.CurrentHealth = MaxHealth * 0.5f;
            Assert.AreEqual(BaseArmor * (1f + Bonus), owner.Parameters.Armor, Tolerance, "a restored line has to answer to the fighter it is put on");
        }

        [TestMethod]
        public void ASavedLineWhoseConditionTheCatalogLost_DoesNotComeBackUnconditional()
        {
            var data = Converter(TestConditions.Holding()).ToData(ItemWearing(Conditional(EntityParameter.Armor, ModifierValueType.Increase, Bonus)));

            var restored = Converter(TestConditions.Empty()).FromData(data);

            Assert.AreEqual(0, restored.Modifiers.Count, "a line whose gate is gone came back as a free bonus");
        }

        private static EquipItemSaveConverter Converter(IConditionProvider catalog) =>
            new(new GrantFactory(() => null, () => null, () => null), catalog);

        /// <summary>A rolled line held up by "while wounded", minted the way a pool entry is.</summary>
        private static SimpleModifier Conditional(EntityParameter parameter, ModifierValueType valueType, float value)
        {
            var descriptor = new ParameterDescriptor(parameter, valueType, new ValueRange(value, value), ModifierScope.Global)
            {
                Weight = 10f, Affix = AffixKind.Prefix, Condition = TestConditions.Wounded,
            };
            var sink = new CollectingSink();
            new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 5), grantFactory: null, TestConditions.Holding())
                .Materialize(descriptor, sink, "test");

            return (SimpleModifier)sink.Entities.Single();
        }

        private static EquipItem ItemWearing(SimpleModifier line)
        {
            var item = new EquipItem(EquipmentPiece.Body, "Iron_Chest", []);
            item.AddAdditionalModifier(line);
            return item;
        }

        private static EquipmentComponent Wear(ConditionOwner owner, IEquipItem item)
        {
            var equipment = new EquipmentComponent(owner);
            owner.ParameterModifiers.RegisterSource(equipment);
            equipment.TryEquip(item, out _);
            return equipment;
        }

        private static ConditionOwner Fighter()
        {
            var owner = new ConditionOwner();
            owner.SetMaximum(EntityParameter.Health, MaxHealth);
            owner.SetMaximum(EntityParameter.Armor, BaseArmor);
            owner.CurrentHealth = MaxHealth;
            return owner;
        }
    }
}
