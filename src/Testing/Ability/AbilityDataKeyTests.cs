namespace LastBreathTest.Ability
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;

    /// <summary>
    /// Every number an ability's record carries has to be a number the ability actually reads. The base
    /// registers each <c>abilityProperties</c> key under its PascalCase name whether anybody asks for it
    /// or not, and an ability's own <c>RegisterDefault</c> yields to data — so a key renamed on one side
    /// only leaves the record still parsing, still registering, and quietly steering nothing. What is
    /// worse than the silence is the direction it fails in: the hard-coded fallback becomes the source of
    /// truth and the data file stops mattering, which is the project's data-first rule inverted.
    ///
    /// The orphan is found by building the same ability twice — once as shipped, once with its properties
    /// emptied. What the second one declares is what the ability asks for itself; anything the record adds
    /// beyond that is a key nobody reads.
    /// </summary>
    [TestClass]
    public class AbilityDataKeyTests
    {
        /// <summary>Record keys that are deliberately not read by the ability that carries them. Empty,
        /// and meant to stay so: a legitimate orphan needs a reason written next to it.
        /// <para>The one entry it ever held — the Crit Calculation's <c>additionalDurationAmount</c>, whose
        /// turn was written into the effect instead — left at А-1b: the cast hands the figure to the buff
        /// it lays, so the record steers the extension it always looked like it was steering.</para></summary>
        private static readonly (string AbilityId, string Key)[] s_knowinglyUnread = [];

        [TestMethod]
        public void EveryNumberARecordCarriesIsANumberItsAbilityReads()
        {
            AbilityProvider bare = ShippedAbilityData.AbilitiesOver(ShippedAbilityData.WithoutProperties());
            List<string> orphans = [];

            foreach ((string abilityId, List<string> keys) in ShippedAbilityData.ShippedProperties())
            {
                IAbility stripped = bare.CreateAbility(abilityId);

                foreach (string key in keys)
                {
                    string parameter = ShippedAbilityData.ParameterKey(key);
                    if (stripped.Declares(parameter)) continue;
                    if (s_knowinglyUnread.Contains((abilityId, key))) continue;

                    orphans.Add($"{abilityId}.{key} (registers '{parameter}', which the ability never asks for)");
                }
            }

            Assert.AreEqual(0, orphans.Count,
                "record numbers nothing reads — the data says one thing and the ability follows its own default:\n  "
                + string.Join("\n  ", orphans));
        }

        [TestMethod]
        public void TheWalkWouldSeeAnOrphanIfThereWereOne()
        {
            // The walk above is only worth its green if it can go red. A key nothing declares is added to
            // one shipped record and the same reading is asked for it.
            AbilityProvider bare = ShippedAbilityData.AbilitiesOver(ShippedAbilityData.WithoutProperties());
            IAbility stripped = bare.CreateAbility("Ability_Dark_Shroud");

            Assert.IsFalse(stripped.Declares(ShippedAbilityData.ParameterKey("reviewProbeOrphan")),
                "the probe ability declares a key nobody wrote, so the walk cannot tell an orphan from a reader");
            Assert.IsTrue(stripped.Declares(ShippedAbilityData.ParameterKey("healthRegeneration")),
                "the Dark Shroud stopped asking for its own regeneration key, so the walk above is vacuous");
        }
    }
}
