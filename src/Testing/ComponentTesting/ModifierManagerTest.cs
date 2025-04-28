namespace PlaygroundTest.ComponentTesting
{
    using Playground.Components;
    using Playground.Script.Effects;
    using Playground.Script.Enums;

    [TestClass]
    public class ModifierManagerTest
    {
        private ModifierManager _modifierManager;
        private readonly List<IModifier> _permanent = [];
        private readonly List<IModifier> _temp = [];

        [TestInitialize]
        public void StartUp()
        {
            _modifierManager = new ModifierManager();
            // need to test how i delete some modifiers
        }

        [TestMethod]
        public void Deleting_Modifier_Test()
        {
            AddTempModifiers(Parameter.CriticalStrikeChance);
            var expected = 0.0385f;
            var actual = _modifierManager.CalculateFloatValue(0.05f, Parameter.CriticalStrikeChance);
            Assert.AreEqual(expected, actual);
        }

        private void AddPermModifiers(Parameter param)
        {
            switch (param)
            {
                case Parameter.StrikeDamage:
                    _permanent.AddRange([
                    new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive, value : 45f),
                    new ModifierTest(Parameter.StrikeDamage, ModifierType.Multiplicative,  value: 1.1f, ModifierPriorities.Equipment),
                    new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive,value : 25, ModifierPriorities.PassiveAbilities)
                ]);
                    break;
                case Parameter.CriticalStrikeChance:
                    _permanent.AddRange([
                    new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive, value : 0.2f),
                    new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Multiplicative, value : 1.15f, ModifierPriorities.Equipment),
                    new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive,value : 0.1f, ModifierPriorities.PassiveAbilities),
                ]);
                    break;
                case Parameter.CriticalStrikeDamage:
                    _permanent.AddRange([
                    new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive, value : 0.5f),
                    new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Multiplicative, value : 1.5f, ModifierPriorities.Equipment),
                    new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive,value : 0.3f, ModifierPriorities.PassiveAbilities)
                ]);

                    break;
                case Parameter.AdditionalStrikeChance:
                    _permanent.AddRange([
                    new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive, value : 0.1f),
                    new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Multiplicative, value : 1.35f, ModifierPriorities.Equipment),
                    new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive,value : 0.2f, ModifierPriorities.PassiveAbilities)
                ]);
                    break;
            }

            foreach (var item in _permanent)
            {
                _modifierManager.AddPermanentModifier(item);
            }
        }


        private void AddTempModifiers(Parameter param)
        {

            switch (param)
            {
                case Parameter.StrikeDamage:
                    _temp.AddRange([
                    new ModifierTest(Parameter.StrikeDamage, ModifierType.Multiplicative, value : 0.9f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive, value : -30, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.StrikeDamage, ModifierType.Additive, value : 15f, ModifierPriorities.Buffs)
                ]);
                    break;
                case Parameter.CriticalStrikeChance:

                    _temp.AddRange([
                    new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Multiplicative, value : 0.7f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive, value : -0.01f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.CriticalStrikeChance, ModifierType.Additive, value : 0.015f, ModifierPriorities.Buffs)
                ]);
                    break;
                case Parameter.CriticalStrikeDamage:
                    _temp.AddRange([
                    new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Multiplicative, value : 0.85f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive, value : -0.15f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.CriticalStrikeDamage, ModifierType.Additive, value : 0.2f, ModifierPriorities.Buffs)
                ]);

                    break;
                case Parameter.AdditionalStrikeChance:
                    _temp.AddRange([
                    new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Multiplicative, value : 0.9f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive, value : -0.01f, ModifierPriorities.Debuffs),
                    new ModifierTest(Parameter.AdditionalStrikeChance, ModifierType.Additive, value : 0.15f, ModifierPriorities.Buffs)
                ]);
                    break;
            }

            foreach (var item in _temp)
            {
                _modifierManager.AddTemporaryModifier(item);
            }
        }

        private void ClearTemp()
        {
            foreach (var item in _temp)
            {
                _modifierManager.RemoveTemporaryModifier(item);
            }
            _temp.Clear();
        }

        private void ClearPerm()
        {
            foreach (var item in _permanent)
            {
                _modifierManager.RemovePermanentModifier(item);
            }
            _permanent.Clear();
        }
    }
}
