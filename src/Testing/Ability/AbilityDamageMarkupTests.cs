namespace LastBreathTest.Ability
{
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Every damage figure the stance documents name, transcribed against the shipped ability data. The
    /// owner balances by playing and then writes the result down, so the document leads and the file
    /// follows; without a walk saying so the two drift apart silently for as long as nobody re-reads
    /// both — which is exactly what happened to the Ice Block (230 in the file against 100 in the
    /// document) and to the Ice Shards.
    ///
    /// Only DAMAGE is written out: flat damage and the two scales, for the main blow and for every
    /// secondary or staged blow that carries its own. Everything else an ability tunes lives in
    /// <c>abilityProperties</c> and is not claimed here — a copy of the whole file would prove nothing.
    /// </summary>
    [TestClass]
    public class AbilityDamageMarkupTests
    {
        /// <summary>Ability id, then the figures its stance document names, keyed by the field carrying
        /// them. Blows the document leaves without numbers are absent rather than guessed.</summary>
        private static readonly (string AbilityId, (string Field, float Value)[] Figures)[] s_documented =
        [
            // Dexterity — "Способности стойки ловкости.md"
            ("Ability_Series_Of_Attacks", [("damage", 30f), ("weaponDamageScale", 0.5f), ("spellDamageScale", 0.25f)]),
            ("Ability_Increasing_Pressure", [("damage", 35f), ("weaponDamageScale", 0.15f), ("spellDamageScale", 0.35f)]),

            // Strength — "Способности стойки силы.md"
            ("Ability_Head_Butt", [("damage", 50f), ("weaponDamageScale", 0.35f), ("spellDamageScale", 0.25f)]),
            ("Ability_Double_Strike", [
                ("damage", 120f), ("weaponDamageScale", 0.8f), ("spellDamageScale", 0.65f),
                ("secondDamage", 60f), ("secondWeaponScale", 1.0f), ("secondSpellScale", 0.25f)]),
            ("Ability_Armageddon", [
                ("damage", 300f), ("weaponDamageScale", 0.75f), ("spellDamageScale", 0.75f),
                ("secondDamage", 600f), ("secondWeaponScale", 1.2f), ("secondSpellScale", 1.2f),
                ("thirdDamage", 1000f), ("thirdWeaponScale", 1.8f), ("thirdSpellScale", 1.8f)]),

            // Intelligence — "Способности стойки интеллекта.md"
            ("Ability_Ice_Shards", [
                ("damage", 60f), ("weaponDamageScale", 0.15f), ("spellDamageScale", 0.75f),
                ("secondStageDamage", 100f), ("secondStageWeaponDamageScale", 0.25f), ("secondStageSpellDamageScale", 1.15f),
                ("shrapnelDamage", 30f), ("shrapnelWeaponDamageScale", 0.05f), ("shrapnelSpellDamageScale", 0.35f)]),
            ("Ability_Ice_Block", [("damage", 100f), ("weaponDamageScale", 0.15f), ("spellDamageScale", 1.1f)]),
            ("Ability_Deep_Freeze", [("damage", 80f), ("weaponDamageScale", 0.35f), ("spellDamageScale", 0.65f)]),
            // "поглощенный барьер х 1.5 + 90% урона способностями молнией" — no flat part, no weapon part.
            ("Ability_Discharge", [("damage", 0f), ("weaponDamageScale", 0f), ("spellDamageScale", 0.9f)]),
            ("Ability_Static_Armor", [("detonationDamage", 250f), ("detonationWeaponScale", 0.85f), ("detonationSpellScale", 0.75f)]),
            // Held back from the book but written down with numbers, so it is held to them too.
            ("Ability_Chain_Lightning", [("damage", 140f), ("weaponDamageScale", 0.25f), ("spellDamageScale", 1.2f)]),
        ];

        /// <summary>Abilities that deal damage and whose stance document names no figure for it. Listed so
        /// the completeness walk stays exact: an ability the owner writes numbers for tomorrow has to be
        /// moved out of here rather than staying unwatched.</summary>
        private static readonly string[] s_undocumented =
        [
            // "Накладывает стак Яда" — the jar's own blow carries the poison and is left unnumbered.
            // Its record still holds damage/scales that ExecuteInternal never reads: whether the jar is
            // meant to strike is an open question with the owner ("the jar carries damage the document
            // does not name"), so the figures stand rather than being deleted by a guess. Anyone wiring
            // them up should know the throw is a COUNTED loop now — a blow added here is dealt per jar.
            "Ability_Jar_Of_Poison",
            // "Чем ниже здоровье, тем короче серия" — the series carries plain attacks, the record is zeroed.
            "Ability_Berserk_Fury",
            // "Снимает все стаки яда и наносит суммарный урон" — the damage IS the stacks.
            "Ability_Poison_Explosion",
        ];

        [TestMethod]
        public void EveryDocumentedDamageFigureIsWhatTheShippedDataSays()
        {
            var shipped = Shipped();
            List<string> divergent = [];

            foreach ((string abilityId, var figures) in s_documented)
            {
                if (!shipped.TryGetValue(abilityId, out AbilityBaseData? data))
                {
                    divergent.Add($"{abilityId}: the document names it, the shipped data does not");
                    continue;
                }

                foreach ((string field, float expected) in figures)
                {
                    float? actual = Figure(data, field);
                    if (actual == null) divergent.Add($"{abilityId}.{field}: the document says {expected}, the data carries no such figure");
                    else if (Math.Abs(actual.Value - expected) > 0.0001f)
                        divergent.Add($"{abilityId}.{field}: the document says {expected}, the data says {actual}");
                }
            }

            Assert.AreEqual(0, divergent.Count,
                $"the shipped damage has drifted from the stance documents — the document leads:\n  {string.Join("\n  ", divergent)}");
        }

        [TestMethod]
        public void EveryAbilityThatDealsDamageIsEitherDocumentedOrNamedAsUndocumented()
        {
            // Without this the table above would be a list of the abilities somebody remembered.
            string[] documented = [.. s_documented.Select(entry => entry.AbilityId)];
            List<string> unwatched = [];

            foreach ((string id, AbilityBaseData data) in Shipped())
            {
                bool dealsDamage = data.Damage != 0 || data.WeaponDamageScale != 0 || data.SpellDamageScale != 0;
                if (!dealsDamage || documented.Contains(id, StringComparer.Ordinal) || s_undocumented.Contains(id, StringComparer.Ordinal)) continue;
                if (data.Hidden) continue; // NPC-only helpers carry no design line of their own

                unwatched.Add(id);
            }

            Assert.AreEqual(0, unwatched.Count,
                $"abilities dealing damage that neither the table nor the undocumented list names: [{string.Join(", ", unwatched)}]");
        }

        /// <summary>A top-level damage field or, failing that, the ability's own property of that name.</summary>
        private static float? Figure(AbilityBaseData data, string field) => field switch
        {
            "damage" => data.Damage,
            "weaponDamageScale" => data.WeaponDamageScale,
            "spellDamageScale" => data.SpellDamageScale,
            _ => data.AbilityProperties.TryGetValue(field, out float value) ? value : null
        };

        /// <summary>The shipped records, parsed straight off the file: what is being claimed is that the
        /// FILE says what the document says, and a reading through the provider would add a second thing
        /// that could be wrong.</summary>
        private static Dictionary<string, AbilityBaseData> Shipped()
        {
            Dictionary<string, AbilityBaseData> shipped = new(StringComparer.Ordinal);
            string path = SharedData.Catalog(DataCatalog.Abilities);
            Assert.IsTrue(Directory.Exists(path), $"the shipped ability catalog is missing at {path}");

            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                foreach (JObject entry in (JObject.Parse(File.ReadAllText(file))["abilities"] as JArray ?? []).OfType<JObject>())
                {
                    var data = entry.ToObject<AbilityBaseData>();
                    if (data != null) shipped[data.Id] = data;
                }

            Assert.IsTrue(shipped.Count > 0, "the shipped ability catalog was not read at all, so these walks prove nothing");
            return shipped;
        }
    }
}
