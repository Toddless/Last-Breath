namespace PlaygroundTest.ComponentTesting
{
    using Playground.Components;
    using Playground.Script.Effects;

    [TestClass]
    public class DamageComponentTest
    {
        private DamageComponent _component;
        private readonly List<IModifier> _permanent = [];
        private readonly List<IModifier> _temp = [];

        [TestInitialize]
        public void StartUp()
        {
           // _component = new DamageComponent(new TestDamageStrategy());
        }

        //[TestCleanup]
        //public void CleanUp()
        //{
        //    ClearPerm();
        //    ClearTemp();
        //}

        //[TestMethod]
        //public void Average_Dmg_Bigger_After_Adding_Modifiers_Test()
        //{
        //    var summBeforeModifiers = _component.GetFlatDamage();
        //    AddPermModifiers(Parameter.StrikeDamage);
        //    Assert.IsTrue(_component.GetFlatDamage() > summBeforeModifiers);
        //}

        //[TestMethod]
        //public void Critical_Chance_Calculation_Right_Order_Test()
        //{
        //    AddPermModifiers(Parameter.CriticalStrikeChance);
        //    _component.AddPermanentModifier(new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.MultiplicativeSum, ModifierPriorities.Equipment, 0.1f));
        //    _component.AddPermanentModifier(new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.MultiplicativeSum, ModifierPriorities.Equipment, 0.2f));
        //    var expectedValue = 0.52325f;
        //    var value = _component.GetCriticalChance();
        //    Assert.AreEqual(expectedValue, value);
        //}

        //private void AddPermModifiers(Parameter param)
        //{
        //    switch (param)
        //    {
        //        case Parameter.StrikeDamage:
        //            _permanent.AddRange([
        //            new ModifierTest(Parameter.StrikeDamage, ModifierType.Multiplicative, ModifierPriorities.Equipment, value: 1.1f),
        //            new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive, ModifierPriorities.BaseParameters, value: 45f),
        //            new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive,ModifierPriorities.PassiveAbilities, value: 25)
        //        ]);
        //            break;
        //        case Parameter.CriticalStrikeChance:
        //            _permanent.AddRange([
        //            new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Multiplicative, ModifierPriorities.Equipment, value: 1.15f),
        //            new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive, ModifierPriorities.BaseParameters, value: 0.2f),
        //            new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive,ModifierPriorities.PassiveAbilities, value: 0.1f),
        //        ]);
        //            break;
        //        case Parameter.CriticalStrikeDamage:
        //            _permanent.AddRange([
        //            new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Multiplicative, ModifierPriorities.Equipment, value: 1.5f),
        //            new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive, ModifierPriorities.BaseParameters, value: 0.5f),
        //            new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive,ModifierPriorities.PassiveAbilities, value: 0.3f)
        //        ]);

        //            break;
        //        case Parameter.AdditionalStrikeChance:
        //            _permanent.AddRange([
        //            new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Multiplicative, ModifierPriorities.Equipment, value: 1.35f),
        //            new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive, ModifierPriorities.BaseParameters, value: 0.1f),
        //            new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive,ModifierPriorities.PassiveAbilities, value: 0.2f)
        //        ]);
        //            break;
        //    }

        //    foreach (var item in _permanent)
        //    {
        //        _component.AddPermanentModifier(item);
        //    }
        //}


        //private void AddTempModifiers(Parameter param)
        //{

        //    switch (param)
        //    {
        //        case Parameter.StrikeDamage:
        //            _temp.AddRange([
        //            new ModifierTest(Parameter.StrikeDamage, ModifierType.Multiplicative, ModifierPriorities.Debuffs, value: 0.9f),
        //            new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive,ModifierPriorities.Debuffs, value: -30),
        //            new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive, ModifierPriorities.Buffs, value: 15f)
        //        ]);
        //            break;
        //        case Parameter.CriticalStrikeChance:

        //            _temp.AddRange([
        //            new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Multiplicative, ModifierPriorities.Debuffs, value: 0.7f),
        //            new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive,ModifierPriorities.Debuffs, value: -0.01f),
        //            new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive, ModifierPriorities.Buffs, value: 0.015f)
        //        ]);
        //            break;
        //        case Parameter.CriticalStrikeDamage:
        //            _temp.AddRange([
        //            new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Multiplicative, ModifierPriorities.Debuffs, value: 0.85f),
        //            new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive,ModifierPriorities.Debuffs, value: -0.15f),
        //            new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive, ModifierPriorities.Buffs, value: 0.2f)
        //        ]);

        //            break;
        //        case Parameter.AdditionalStrikeChance:
        //            _temp.AddRange([
        //            new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Multiplicative, ModifierPriorities.Debuffs, value: 0.9f),
        //            new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive,ModifierPriorities.Debuffs, value: -0.01f),
        //            new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive, ModifierPriorities.Buffs, value: 0.15f)
        //        ]);
        //            break;
        //    }

        //    foreach (var item in _temp)
        //    {
        //        _component.AddTemporaryModifier(item);
        //    }
        //}

        //private void ClearTemp()
        //{
        //    foreach (var item in _temp)
        //    {
        //        _component.RemoveTemporaryModifier(item);
        //    }
        //    _temp.Clear();
        //}

        //private void ClearPerm()
        //{
        //    foreach(var item in _permanent)
        //    {
        //        _component.RemovePermanentModifier(item);
        //    }
        //    _permanent.Clear();
        //}
    }
}
