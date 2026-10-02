namespace LastBreathTest.Descriptors
{
    using System;
    using System.Collections.Generic;
    using LastBreath.Descriptors;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;

    /// <summary>
    /// What editing a quest may not cost. A quest is the one catalog whose records the game drops WHOLE
    /// on a single missing key — an objective without its counter key is a quest nobody can take — and the
    /// editor answers a wrong key with nothing at all until the file is saved.
    /// <para>The rules held here are the ones a session that lost a key would have broken: showing a
    /// record writes nothing, laying a blank down writes no key the author did not ask for, changing the
    /// type of an entry keeps what both types name, and a save is the file's shape and never its
    /// content.</para>
    /// </summary>
    [TestClass]
    public class QuestEditingSafetyTests
    {
        /// <summary>The counter key of the shipped quest, which is the word this whole class is about.</summary>
        private const string CounterKey = "Kill_Count:Npc_Grave_Revenant";

        private const string CounterPointer = "/quests/0/stages/1/objectives/1/counter";

        private const string ObjectivePointer = "/quests/0/stages/1/objectives/1";

        private const string StagePointer = "/quests/0/stages/0";

        private const string ActionPointer = "/quests/0/rewards/actions/0";

        /// <summary>A quest written the way the shipped files are, held here rather than read off disk:
        /// what these rules are about is what an editing session does to a record, and a test that read
        /// the working tree would answer about whatever was last saved over it.</summary>
        private const string Quest = """
        {
            "quests": [
                {
                    "id": "Quest_Field_Of_Bones",
                    "giverNpcId": "Npc_Bandit_Veteran",
                    "faction": "Human",
                    "tier": 1,
                    "turnInNpcIds": [ "Npc_Bandit_Veteran" ],
                    "declinePolicy": "Cooldown",
                    "declineCooldownHours": 24,
                    "canFail": false,
                    "timeLimitHours": 0,
                    "acceptConditions": [],
                    "stages": [
                        {
                            "id": "FindBattlefield",
                            "objectives": [
                                {
                                    "id": "Battlefield",
                                    "condition": { "type": "Fact", "key": "Location_Discovered:Old_Battlefield" }
                                }
                            ]
                        },
                        {
                            "id": "GatherProof",
                            "objectives": [
                                {
                                    "id": "Proof",
                                    "condition": { "type": "HasItem", "itemId": "Coal", "amount": 3 }
                                },
                                {
                                    "id": "Revenants",
                                    "counter": { "key": "Kill_Count:Npc_Grave_Revenant", "amount": 2, "retroactive": true },
                                    "optional": true
                                }
                            ]
                        }
                    ],
                    "rewards": {
                        "influenceExp": 40,
                        "items": [],
                        "actions": [
                            { "type": "TakeItem", "itemId": "Coal", "amount": 3 },
                            { "type": "AddReputation", "faction": "Human", "delta": 150, "reason": "Quest_Field_Of_Bones" }
                        ]
                    }
                }
            ]
        }
        """;

        private CatalogSchema _schema = null!;

        private RecordSchema _quest = null!;

        [TestInitialize]
        public void Setup()
        {
            _schema = new QuestsCatalogDescriptor().Describe(new SchemaReflector());
            _quest = _schema.Sections[0].Record;
        }

        /// <summary>
        /// Everything the inspector asks the pure machinery while it DRAWS a record, asked over every
        /// record of the quest: which shape a record wears, which type an entry names, which vocabulary
        /// answers a key, and what a blank of each would look like. None of it may write.
        /// <para>This is the rule a lost counter key was first blamed on, and it is the one that has to
        /// stay provable: a panel that wrote while it drew would change files nobody edited.</para>
        /// </summary>
        [TestMethod]
        public void AskingWhatToDrawWritesNothingIntoTheDocument()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Quest);
            string before = document.Root.ToString(Formatting.None);

            Drawn(document, _quest, document.Root);

            Assert.AreEqual(before, document.Root.ToString(Formatting.None), "drawing a record wrote into it");
            Assert.AreEqual(0, document.History.Depth, "drawing a record filed a step");
        }

        /// <summary>A save is the shape of a file and never its content: every key the file held is still
        /// there, spelled the same, and no key the author never wrote has arrived.</summary>
        [TestMethod]
        public void SavingRewritesTheShapeAndKeepsEveryKey()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Quest);

            string written = document.Write(new SchemaKeyOrder(_schema), CanonicalJsonOptions.Default);
            JToken saved = JToken.Parse(written);

            Assert.IsTrue(
                JToken.DeepEquals(saved, JToken.Parse(Quest)),
                "a save changed what the file says and not only how it is written");
        }

        /// <summary>The blank a key opens with is the empty value of its kind and never a key of its own:
        /// a list opens as a list, and a record opens without the keys nobody decided. Held because the
        /// blank is what a press of "+ field" writes, and a blank that carried keys would put words into
        /// a record the author only asked to see.</summary>
        [TestMethod]
        public void BlankOfAKeyIsTheEmptyValueOfItsKind()
        {
            FieldSchema stages = Field(_quest, "stages");
            RecordSchema stage = stages.Item!.Record!;

            Assert.AreEqual(JTokenType.Array, Blank(Field(stage, "transitions")).Type);
            Assert.AreEqual(JTokenType.Array, Blank(Field(stage, "onComplete")).Type, "a key read as a list has to open as one");
            Assert.AreEqual(0, ((JArray)Blank(Field(stage, "onComplete"))).Count);
        }

        /// <summary>Changing the type of an entry keeps every key both types name and drops only what the
        /// outgoing one alone was written with. A switch that shed more would take the author's words with
        /// it, and the entry it stands in is the only place they were written.</summary>
        [TestMethod]
        public void SwitchingAnEntryTypeKeepsWhatBothTypesName()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Quest);
            var actions = JsonPointer.Parse(ActionPointer);
            VocabularyBinding binding = NarrativeFieldVocabularies.Resolve(Actions(_quest))!;

            Assert.IsTrue(TypedRecords.Switch(document, actions, binding, "GiveItem"), "the type did not change");

            JToken? entry = document.Resolve(actions);

            Assert.AreEqual("GiveItem", entry?["type"]?.ToString());
            Assert.AreEqual("Coal", entry?["itemId"]?.ToString(), "a key both types name was shed");
            Assert.AreEqual(3, entry?["amount"]?.Value<int>(), "a key both types name was shed");
        }

        /// <summary>Nothing done to a stage or an objective reaches the counter beside it. The one gesture
        /// that may take a key out is the one aimed at that key, and the addresses under a record are
        /// what keeps a press on one row from answering for another.</summary>
        [TestMethod]
        public void WritingKeysIntoAStageLeavesTheCounterBesideItAlone()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Quest);
            FieldSchema stage = Field(_quest, "stages").Item!.Record! is { } record
                ? Field(record, "transitions")
                : throw new InvalidOperationException("the stage record is not described");

            Assert.IsTrue(document.Insert(JsonPointer.Parse(StagePointer), stage.JsonName, Blank(stage)));
            Assert.IsTrue(document.Insert(JsonPointer.Parse(ObjectivePointer), "hidden", new JValue(false)));

            Assert.AreEqual(CounterKey, document.Resolve(JsonPointer.Parse(CounterPointer))?["key"]?.ToString());
        }

        /// <summary>Taking a key out and stepping back puts it where it stood: the word, the value and the
        /// place among its neighbours. A step back that appended it instead would leave a file the author
        /// never wrote in a diff he cannot read.</summary>
        [TestMethod]
        public void SteppingBackPutsAKeyBackWhereItStood()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Quest);
            var counter = JsonPointer.Parse(CounterPointer);

            Assert.IsTrue(document.Remove(counter.Append("key")));
            Assert.IsNull(document.Resolve(counter)?["key"]);

            document.History.Undo();

            // Spelled out rather than compared as a tree: order IS content in a canonical file, and a key
            // put back at the end of its record is a diff the author never wrote.
            Assert.AreEqual(
                """{"key":"Kill_Count:Npc_Grave_Revenant","amount":2,"retroactive":true}""",
                document.Resolve(counter)?.ToString(Formatting.None),
                "the key came back somewhere other than where it stood");
        }

        private JToken Blank(FieldSchema field) => TypedRecords.Blank(NarrativeFieldVocabularies.Resolve(field), field);

        /// <summary>The key the rewards of a quest write their actions under, which is the vocabulary this
        /// class switches an entry of.</summary>
        private static FieldSchema Actions(RecordSchema quest) => Field(Field(quest, "rewards").Record!, "actions");

        private static FieldSchema Field(RecordSchema record, string name)
        {
            foreach (FieldSchema field in record.Fields)
                if (string.Equals(field.JsonName, name, StringComparison.Ordinal))
                    return field;

            throw new InvalidOperationException($"'{record.TypeName}' names no '{name}'");
        }

        /// <summary>Every question the inspector asks about one record while it draws it, and the same
        /// questions about everything standing inside it. The answers are thrown away: what is held here
        /// is that asking them changes nothing.</summary>
        private void Drawn(JsonTreeDocument document, RecordSchema record, JToken token)
        {
            if (record.Variants is { } variants) _ = RecordTemplates.Worn(variants, token);

            foreach (FieldSchema field in record.Fields)
            {
                _ = RecordTemplates.Blank(field);

                VocabularyBinding? binding = NarrativeFieldVocabularies.Resolve(field);

                if (binding is not null) _ = TypedRecords.Blank(binding, field);

                JToken? value = token is JObject holder ? holder[field.JsonName] : null;

                if (value is null) continue;

                foreach (JToken entry in Entries(value))
                {
                    if (binding is not null)
                    {
                        _ = TypedRecords.Standing(binding, entry);

                        if (TypedRecords.Worn(binding, entry) is { } type) Drawn(document, type, entry);

                        continue;
                    }

                    if (Held(field) is { } nested) Drawn(document, nested, entry);
                }
            }
        }

        /// <summary>What a key holds, one record at a time: the elements of a list, or the value itself.</summary>
        private static IEnumerable<JToken> Entries(JToken value) => value is JArray list ? list : [value];

        /// <summary>The record a key holds one of, or null where the schema describes none.</summary>
        private static RecordSchema? Held(FieldSchema field) => field.Record ?? field.Item?.Record;
    }
}
