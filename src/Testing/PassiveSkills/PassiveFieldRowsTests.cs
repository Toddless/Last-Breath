namespace LastBreathTest.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Data.GameData;
    using Core.Enums;

    /// <summary>
    /// The authoring tool shows a passive's numbers as rows of dropdowns instead of as typed keys, and the
    /// file it writes has to come out byte for byte what it always was. Everything the panel does to get
    /// there lives outside the widgets so that the trip — dropdowns to key, key back to dropdowns — can be
    /// held to here rather than by clicking.
    /// <para>Three things are worth the tests. A key spelled from the words must be a key the grammar reads
    /// back as the same words, or the tool authors lines the grant refuses. A key the grammar cannot read
    /// must survive being shown and written again untouched, or opening a hand-written file and saving it
    /// deletes what the tool did not understand. And a field the catalog names must be a field the author
    /// cannot lose.</para>
    /// </summary>
    [TestClass]
    public class PassiveFieldRowsTests
    {
        private const string PoisonedClaws = "Passive_Skill_Poisoned_Claws";

        /// <summary>The round trip a two-word line makes every time the panel is redrawn.</summary>
        [TestMethod]
        public void APlainLineComesBackFromItsKeyWordForWord()
        {
            var written = new StatFieldRow
            {
                Parameter = EntityParameter.HealthRecovery,
                ValueType = ModifierValueType.Multiplicative,
                Value = -0.25f
            };

            List<StatFieldRow> read = PassiveFieldRows.ReadStat(PassiveFieldRows.Write([written]));

            Assert.AreEqual(1, read.Count);
            Assert.IsFalse(read[0].IsRaw, "a key the dropdowns spelled is a key the grammar reads");
            Assert.AreEqual(EntityParameter.HealthRecovery, read[0].Parameter);
            Assert.AreEqual(ModifierValueType.Multiplicative, read[0].ValueType);
            Assert.IsNull(read[0].PerParameter, "no carrier was picked, and none may appear from the key");
            Assert.AreEqual(-0.25f, read[0].Value);
            Assert.AreEqual("HealthRecovery:Multiplicative", read[0].Name);
        }

        /// <summary>The same trip with the carrier picked — the word a two-word key has no room for, and
        /// the one a composer that forgot it would lose in silence.</summary>
        [TestMethod]
        public void ACarriedLineKeepsTheParameterItIsCountedPerUnitOf()
        {
            var written = new StatFieldRow
            {
                Parameter = EntityParameter.PhysicalDamage,
                ValueType = ModifierValueType.Increase,
                PerParameter = EntityParameter.Strength,
                Value = 0.01f
            };

            List<StatFieldRow> read = PassiveFieldRows.ReadStat(PassiveFieldRows.Write([written]));

            Assert.AreEqual("PhysicalDamage:Increase:Strength", read[0].Name);
            Assert.AreEqual(EntityParameter.Strength, read[0].PerParameter,
                "the carrier is a word of the key, and a key written without it is a different line");

            Assert.AreEqual(EntityParameter.PhysicalDamage, read[0].Parameter);
            Assert.AreEqual(ModifierValueType.Increase, read[0].ValueType);
        }

        /// <summary>The trip with the step picked as well — the word that decides how many of the carrier
        /// one value buys, and the one a composer that forgot it would silently turn into a hundredth of
        /// the line the author wrote.</summary>
        [TestMethod]
        public void ASteppedLineKeepsTheCountItIsMeasuredIn()
        {
            var written = new StatFieldRow
            {
                Parameter = EntityParameter.CriticalDamage,
                ValueType = ModifierValueType.Increase,
                PerParameter = EntityParameter.Evade,
                Step = 100,
                Value = 0.02f
            };

            List<StatFieldRow> read = PassiveFieldRows.ReadStat(PassiveFieldRows.Write([written]));

            Assert.AreEqual("CriticalDamage:Increase:Evade:100", read[0].Name);
            Assert.AreEqual(100, read[0].Step, "the step is a word of the key, and a key written without it is a different line");
            Assert.AreEqual(EntityParameter.Evade, read[0].PerParameter);
            Assert.AreEqual(0.02f, read[0].Value);
        }

        /// <summary>A step of one is what every key without one already says, so it is never spelled: an
        /// authored file must not be re-diffed by a word the tool added and the grammar ignores.</summary>
        [TestMethod]
        public void AStepOfOneIsNeverWrittenIntoTheKey()
        {
            List<KeyValuePair<string, float>> record = [new("PhysicalDamage:Increase:Strength", 0.01f)];

            List<StatFieldRow> rows = PassiveFieldRows.ReadStat(record);

            Assert.AreEqual(1, rows[0].Step, "a key naming no step means one unit of the carrier");
            CollectionAssert.AreEqual(record, PassiveFieldRows.Write(rows),
                "the record came back with a word the file never carried");

            Assert.AreEqual("HealthRecovery:Multiplicative",
                new StatFieldRow { Parameter = EntityParameter.HealthRecovery, ValueType = ModifierValueType.Multiplicative }.Name,
                "a line with no carrier at all grew a step it has nowhere to put");
        }

        /// <summary>Two lines over one carrier differing only in their step are two keys, and dropping one
        /// of them onto the other's step is the collision the panel refuses.</summary>
        [TestMethod]
        public void TwoStepsOverOneCarrierAreTwoLines()
        {
            List<StatFieldRow> rows = PassiveFieldRows.ReadStat(
            [
                new KeyValuePair<string, float>("CriticalDamage:Increase:Evade", 0.0002f),
                new KeyValuePair<string, float>("CriticalDamage:Increase:Evade:100", 0.02f)
            ]);

            Assert.AreEqual(2, PassiveFieldRows.Write(rows).Count, "the record folded two lines into one key");

            rows[1].Step = 1;
            Assert.IsTrue(PassiveFieldRows.Holds(rows, rows[1].Name, exceptIndex: 1),
                "the second row stepping onto the first row's key was not recognised as taking it");
        }

        /// <summary>Every line the dropdowns can offer is a line the grant will read. The pairing the
        /// grammar refuses — a line measured per unit of what it feeds — is not on offer at all.</summary>
        [TestMethod]
        public void EveryLineTheDropdownsCanSpellIsOneTheGrammarReads()
        {
            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
                foreach (ModifierValueType valueType in PassiveFieldRows.ValueTypes)
                    foreach (EntityParameter carrier in PassiveFieldRows.ParametersExcept(parameter))
                    {
                        string key = StatPassiveGrammar.Key(parameter, valueType, carrier);
                        Assert.IsTrue(StatPassiveGrammar.TryReadLine(key, 1f, out StatPassiveLine line, out string? refusal),
                            $"'{key}' is offered by the panel and refused by the grant: {refusal}");

                        Assert.AreEqual(parameter, line.Parameter);
                        Assert.AreEqual(valueType, line.ValueType);
                        Assert.AreEqual(carrier, line.PerParameter);
                    }
        }

        /// <summary>The carrier a line feeds is never offered as its own carrier — the one pairing of the
        /// three words the grammar refuses, kept out of the dropdown rather than caught after the fact.</summary>
        [TestMethod]
        public void AParameterIsNeverOfferedAsItsOwnCarrier()
        {
            CollectionAssert.DoesNotContain(PassiveFieldRows.ParametersExcept(EntityParameter.Strength),
                EntityParameter.Strength);

            Assert.AreEqual(Enum.GetValues<EntityParameter>().Length - 1,
                PassiveFieldRows.ParametersExcept(EntityParameter.Strength).Count,
                "one parameter is held back, not two");
        }

        /// <summary>A hand-written key the tool cannot read: shown, kept, and written back exactly as it
        /// was found. The row is what the author corrects; nothing here rewrites or drops it.</summary>
        [TestMethod]
        public void AKeyTheGrammarRefusesSurvivesTheTripUntouched()
        {
            List<KeyValuePair<string, float>> record =
            [
                new("PhysicalDamage:Increase", 0.1f),
                new("PhysicalDamage:Incraese", 0.2f),
                new("HealthRecovery:Flag", 1f),
                new("Strength:Flat:Strength", 3f),
                new("Armor:Flat:Strength:0", 4f),
                new("Armor:Flat:100", 5f)
            ];

            List<StatFieldRow> rows = PassiveFieldRows.ReadStat(record);

            Assert.IsFalse(rows[0].IsRaw);
            Assert.IsTrue(rows[1].IsRaw, "a misspelled parameter is nothing the dropdowns could have spelled");
            Assert.IsTrue(rows[2].IsRaw, "a flag is refused by the grammar, so it is not a line the panel owns");
            Assert.IsTrue(rows[3].IsRaw, "a line measured per unit of itself is refused the same way");
            Assert.IsTrue(rows[4].IsRaw, "a step of zero is no step the dropdowns could have spelled");
            Assert.IsTrue(rows[5].IsRaw, "a step with no carrier to count is refused the same way");

            CollectionAssert.AreEqual(record, PassiveFieldRows.Write(rows),
                "the record comes back key for key, number for number, and in the order it was written");
        }

        /// <summary>A refused key corrected to a readable one stops being raw — the rows are read from the
        /// record every time, so the correction is all it takes.</summary>
        [TestMethod]
        public void ACorrectedKeyBecomesALineAgain()
        {
            List<StatFieldRow> rows = PassiveFieldRows.ReadStat([new KeyValuePair<string, float>("PhysicalDamage:Incraese", 0.2f)]);
            rows[0].RawName = "PhysicalDamage:Increase";

            List<StatFieldRow> reread = PassiveFieldRows.ReadStat(PassiveFieldRows.Write(rows));

            Assert.IsFalse(reread[0].IsRaw);
            Assert.AreEqual(ModifierValueType.Increase, reread[0].ValueType);
            Assert.AreEqual(0.2f, reread[0].Value, "correcting the name does not touch the number");
        }

        /// <summary>A fresh row is a line no row on the node writes yet: a record cannot hold two numbers
        /// under one key, so a button that offered the same default twice would author a row the file
        /// swallows.</summary>
        [TestMethod]
        public void AFreshLineIsNeverOneTheNodeAlreadyWrites()
        {
            List<StatFieldRow> rows = [];

            for (int added = 0; added < 8; added++)
            {
                StatFieldRow fresh = PassiveFieldRows.Fresh(rows);
                Assert.IsFalse(PassiveFieldRows.Holds(rows, fresh.Name, exceptIndex: -1),
                    $"'{fresh.Name}' is already written on the node");

                rows.Add(fresh);
            }

            Assert.AreEqual(8, PassiveFieldRows.Write(rows).Count);
            Assert.AreEqual(8, new HashSet<string>(PassiveFieldRows.Write(rows).Select(row => row.Key)).Count);
        }

        /// <summary>Two rows spelling one key is the gesture the panel refuses; this is what it asks.</summary>
        [TestMethod]
        public void AKeyAnotherRowWritesIsRecognisedAsTaken()
        {
            List<StatFieldRow> rows = PassiveFieldRows.ReadStat(
            [
                new KeyValuePair<string, float>("Armor:Flat", 10f),
                new KeyValuePair<string, float>("Evade:Flat", 5f)
            ]);

            Assert.IsTrue(PassiveFieldRows.Holds(rows, "Armor:Flat", exceptIndex: 1),
                "the second row asking whether it may become the first row's line is told no");

            Assert.IsFalse(PassiveFieldRows.Holds(rows, "Armor:Flat", exceptIndex: 0),
                "a row is never in its own way");
        }

        /// <summary>Naming a passive writes in the fields its factory reads. Zero is a number like any
        /// other: the field is created, not pending, and nothing in the file marks it as unfinished.</summary>
        [TestMethod]
        public void TheFieldsOfANamedPassiveAreWrittenInAtZero()
        {
            List<KeyValuePair<string, float>> written =
                PassiveFieldRows.WithRequired(Fields(PoisonedClaws), []);

            CollectionAssert.AreEqual(new[] { "percentFromDamage", "duration" }, written.Select(row => row.Key).ToArray());
            Assert.IsTrue(written.TrueForAll(row => row.Value == 0f));
        }

        /// <summary>Materialising is additive and never a reordering: what the author already wrote keeps
        /// its place and its number, and the fields he is missing are appended in catalog order.</summary>
        [TestMethod]
        public void WritingInTheMissingFieldsLeavesTheAuthoredOnesWhereTheyStand()
        {
            List<KeyValuePair<string, float>> record =
            [
                new("notes", 2f),
                new("duration", 3f)
            ];

            List<KeyValuePair<string, float>> written = PassiveFieldRows.WithRequired(Fields(PoisonedClaws), record);

            CollectionAssert.AreEqual(new[] { "notes", "duration", "percentFromDamage" },
                written.Select(row => row.Key).ToArray(),
                "an authored field stays on the line it was written on; a missing one is appended");

            Assert.AreEqual(3f, written[1].Value, "a field already carried keeps its number");
        }

        /// <summary>The rows a named passive is shown as: the catalog's fields wear its names and cannot be
        /// removed, anything beyond it stays the free pair the author wrote.</summary>
        [TestMethod]
        public void TheCatalogsOwnFieldsAreMarkedAndTheRestAreFree()
        {
            List<CatalogFieldRow> rows = PassiveFieldRows.ReadFields(Fields(PoisonedClaws),
            [
                new KeyValuePair<string, float>("percentFromDamage", 0.75f),
                new KeyValuePair<string, float>("notes", 2f)
            ]);

            Assert.IsTrue(rows[0].IsRequired, "the factory reads this field by name — the author cannot lose it");
            Assert.IsFalse(rows[1].IsRequired, "a field beyond the catalog is the author's own");
            Assert.AreEqual(2, rows.Count, "showing a record adds nothing to it");
        }

        /// <summary>Reading the record as rows is a reading and nothing more — the same list comes back.</summary>
        [TestMethod]
        public void ShowingANamedRecordDoesNotChangeIt()
        {
            List<KeyValuePair<string, float>> record =
            [
                new("percentFromDamage", 0.75f),
                new("duration", 3f)
            ];

            CollectionAssert.AreEqual(record,
                PassiveFieldRows.Write(PassiveFieldRows.ReadFields(Fields(PoisonedClaws), record)));
        }

        /// <summary>The fields the shipped catalog names for a passive — the tool reads the same file, so
        /// the rows are held to what the game actually ships rather than to a list written twice.</summary>
        private static IReadOnlyList<string> Fields(string id)
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PassiveSkills), "PassiveCatalog.json");
            IReadOnlyList<string> fields = PassiveSkillCatalog.Read(File.ReadAllText(path)).RequiredFields(id);

            Assert.IsTrue(fields.Count > 0, $"'{id}' names no fields in the shipped catalog");
            return fields;
        }
    }
}
