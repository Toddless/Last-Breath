namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities.HeadButt;
    using Core.Localization;

    [TestClass]
    public class AbilityDescriptionTests
    {
        [TestMethod]
        public void HeadButtDescriptionRendersLiveModuleValues()
        {
            var provider = new FakeLocalizationProvider();
            provider.Strings["Ability_Head_Butt_Description"] =
                "Perform {Attacks|lunge|lunges} dealing {Damage} damage. Stuns for {StunDuration|turn|turns}.";
            provider.Plurals["lunge"] = ("lunge", "lunges");
            provider.Plurals["turn"] = ("turn", "turns");
            Localization.Override(new LocalizationService(provider, new ModifierFormatter(provider, new ParameterFormatProvider()), []));

            var ability = new HeadButt([], cooldown: 2, costValue: 10, damage: 40f,
                weaponDamageScale: 0f, spellDamageScale: 0f, stunDuration: 1, attacks: 2);

            string text = ability.Description;

            StringAssert.Contains(text, $"{TextPalette.ColorizeNumber("2")} lunges");
            StringAssert.Contains(text, $"dealing {TextPalette.ColorizeNumber("40")} damage");
            StringAssert.Contains(text, $"{TextPalette.ColorizeNumber("1")} turn.");
        }
    }
}
