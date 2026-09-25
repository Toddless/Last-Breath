namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Core.Data.DialogueData;
    using Core.Data.QuestData;
    using Core.Narrative;
    using Core.Narrative.Facts;
    using Core.Narrative.Validation;
    using Newtonsoft.Json;
    using Tooling.Localization;

    /// <summary>
    /// The registry of the world's facts: the families the game's own code keeps, the words the documents
    /// write, and who writes and reads each one. One key is the only thing a dialogue says to another
    /// dialogue and to the game's own trackers, and nothing in a document says who is listening — so what
    /// this holds is that both ends of every key are named, and that neither end is invented twice.
    /// </summary>
    [TestClass]
    public class FactKeyRegistryTests
    {
        /// <summary>The json name every fact key of the vocabulary is written under.</summary>
        private const string KeyName = "key";

        /// <summary>What a builder's argument is filled with while its family is being read off it. A word
        /// no id could be, so a probe cannot be mistaken for a key somebody wrote.</summary>
        private const string Probe = "<probe>";

        /// <summary>The npc every forged record is written for.</summary>
        private const string ForgedNpc = "Npc_Forged";

        /// <summary>Where the forged conversation writes a family's template as it stands.</summary>
        private const string TemplateWhere = "Dialogues/Npc_Forged/nodes/Greeting/options/Ask/visibleConditions[0]/key";

        /// <summary>The folders of the game's own code that keep facts — every project a fact is written
        /// or read in. A test walking source is walking these and nothing else: a tool or a test writing a
        /// key as a literal is writing a fixture and not a fact of the world.</summary>
        private static readonly string[] s_sourceFolders = ["Core", "Main", "Battle", "Crafting"];

        /// <summary>One family as the tool offers it, with its parameter named rather than filled in.</summary>
        private static readonly string KillTemplate = $"{FactKeys.KillCountHead}{FactKeys.Separator}<npcId>";

        /// <summary>A fact key written as a literal straight into one of the service's own calls. What the
        /// builders exist to stop: a word spelt in two places drifts apart on the first rename, and the
        /// registry answers for the half that went through them.</summary>
        private static readonly Regex s_literalKey =
            new("""[Ff]acts\??\.(SetFact|IsSet|GetCount|SetCount|Add)\(\s*"[^"]""", RegexOptions.Compiled);

        /// <summary>Every family the code declares is one a builder actually writes, and every builder has
        /// a family: a key built and not declared is answered by nothing, and a family declared and not
        /// built is a row the tool offers and the game never raises.</summary>
        [TestMethod]
        public void EveryBuilderOfTheCode_IsOneDeclaredFamily()
        {
            foreach (MethodInfo builder in Builders())
            {
                string key = Built(builder);

                FactKeyDeclaration[] covering = [.. FactKeyDeclarations.All.Where(declaration => declaration.Covers(key))];

                Assert.AreEqual(1, covering.Length,
                    $"'{builder.Name}' writes '{key}', which {covering.Length} declared families cover");
            }
        }

        [TestMethod]
        public void EveryDeclaredFamily_IsWrittenBySomeBuilder()
        {
            string[] built = [.. Builders().Select(Built)];

            foreach (FactKeyDeclaration declaration in FactKeyDeclarations.All)
                Assert.IsTrue(built.Any(declaration.Covers),
                    $"'{declaration.Template}' is declared and no builder writes a key of it");
        }

        /// <summary>Every family names the code that writes it. A family nothing writes would be reported
        /// unwritten forever, which is a fact about the declaration and not about the data.</summary>
        [TestMethod]
        public void EveryDeclaredFamily_NamesTheCodeThatWritesIt()
        {
            foreach (FactKeyDeclaration declaration in FactKeyDeclarations.All)
                Assert.AreNotEqual(0, declaration.Writers.Count, $"'{declaration.Template}' names nothing that writes it");
        }

        /// <summary>The marker on the vocabulary, held to the one name the parsers read a fact key under.
        /// A parameter written as plain text is one the registry never sees, so the key an author typed
        /// there would be answered by nobody without a word said.</summary>
        [TestMethod]
        public void EveryParameterWrittenUnderTheFactKeyName_CarriesTheRole()
        {
            foreach ((NarrativeRecordSpec record, NarrativeParameterSpec parameter) in Parameters().Where(pair => pair.Parameter.JsonName == KeyName))
                Assert.AreNotEqual(NarrativeParameterRole.None, parameter.Role,
                    $"'{record.TypeName}' writes a '{KeyName}' the registry cannot see");
        }

        /// <summary>And nothing else carries it: a role on a parameter holding something other than a fact
        /// key would put a word into the registry the game never keeps as one.</summary>
        [TestMethod]
        public void NoOtherParameter_CarriesTheRole()
        {
            foreach ((NarrativeRecordSpec record, NarrativeParameterSpec parameter) in Parameters().Where(pair => pair.Parameter.Role != NarrativeParameterRole.None))
                Assert.AreEqual(KeyName, parameter.JsonName,
                    $"'{record.TypeName}' marks '{parameter.JsonName}' as a fact key");
        }

        /// <summary>The three places a document names a fact — an action raising one, a condition asking
        /// about one, and the counter of a quest objective — each read down to the very key it is written
        /// under, so a row of the registry opens the record the author has to edit.</summary>
        [TestMethod]
        public void TheKeysOfTheDocuments_AreReadWithTheirPlaces()
        {
            FactKeyRegistry registry = Read(ForgedDialogueJson, ForgedQuestJson);

            FactKeyEntry invented = Key(registry, "Fact_Author_Invented");

            CollectionAssert.AreEqual(
                new[] { "Dialogues/Npc_Forged/nodes/Greeting/options/Raise/actions[0]/key" }, invented.Writers.ToArray());
            CollectionAssert.AreEqual(
                new[] { "Dialogues/Npc_Forged/nodes/Greeting/options/Raise/visibleConditions[0]/key" }, invented.Readers.ToArray());

            Assert.IsNull(invented.Family, "a word of the author's own was folded into a family the code keeps");

            FactKeyEntry counted = Key(registry, "Fact_Counted");

            CollectionAssert.AreEqual(
                new[] { "Quests/Quest_Forged/stages/Only/objectives/Count/counter/key" }, counted.Readers.ToArray());
        }

        /// <summary>A key of a family the code keeps is answered by that family: the tracker writes one key
        /// per npc and no code names the npc a quest counts, so a counter read against a family with no
        /// member of its own would be reported unwritten forever.</summary>
        [TestMethod]
        public void AKeyOfADeclaredFamily_IsAnsweredByTheCodeKeepingIt()
        {
            FactKeyRegistry registry = Read(ForgedDialogueJson, ForgedQuestJson);

            FactKeyEntry talked = Key(registry, $"{FactKeys.NpcTalkedHead}{FactKeys.Separator}{ForgedNpc}");

            Assert.AreEqual($"{FactKeys.NpcTalkedHead}{FactKeys.Separator}<npcId>", talked.Family);
            Assert.IsFalse(talked.NeverWritten, "a key of a family the code writes was reported unwritten");
            Assert.IsTrue(talked.Writers.Any(writer => writer.StartsWith(FactKeyDeclarations.CodePrefix, StringComparison.Ordinal)),
                "the code writing the family is not named under the key");
        }

        /// <summary>The two ends a key can be loose at, each reported once however many places name it.</summary>
        [TestMethod]
        public void TheLooseEndsOfAKey_AreFoundAndNamedOnce()
        {
            IReadOnlyList<NarrativeFinding> findings = Checked(ForgedDialogueJson, ForgedQuestJson);

            Assert.AreEqual(1, findings.Count(finding => finding.Kind == NarrativeFindingKind.FactNeverWritten
                                                         && finding.Where == "facts/Fact_Nobody_Writes"),
                Lines(findings));

            Assert.AreEqual(1, findings.Count(finding => finding.Kind == NarrativeFindingKind.FactNeverRead
                                                         && finding.Where == "facts/Fact_Nobody_Reads"),
                Lines(findings));

            Assert.IsFalse(findings.Any(finding => finding.Kind == NarrativeFindingKind.FactNeverWritten
                                                   && finding.Where.Contains("Fact_Author_Invented", StringComparison.Ordinal)),
                "a key written and read in the same conversation was called loose");
        }

        /// <summary>The boundaries a registry must not swallow: a family named without one of its members,
        /// a head with nothing written after it, and a word spelled onto the end of a family of ONE — whose
        /// template IS its key, so nothing at all is written after it and a longer word is somebody
        /// else's.</summary>
        [TestMethod]
        public void TheKeysThatAreNotMembersOfTheirFamily_AreNotFoldedIntoIt()
        {
            FactKeyRegistry registry = Read(BoundaryDialogueJson, NoQuestsJson);

            foreach (string written in new[]
                     {
                         FactKeys.KillCountHead,
                         $"{FactKeys.KillCountHead}{FactKeys.Separator}",
                         $"{FactKeys.ItemEquippedAnyKey}{FactKeys.Separator}Extra"
                     })
            {
                FactKeyEntry key = Key(registry, written);

                Assert.IsNull(key.Family, $"'{written}' was folded into a family it names no member of");
                Assert.IsTrue(key.NeverWritten, $"'{written}' was answered by code that writes another key");
            }
        }

        /// <summary>A family's template written into a document as it stands: the word offered under the
        /// box, picked, and never filled in. The game raises the KEYS of a family and never the spelling of
        /// one, so the word is named where it is written and taken into the registry nowhere — folding it
        /// into the family it is spelled after would read a key nothing ever raises as one the code keeps,
        /// and the author would be told nothing at all.</summary>
        [TestMethod]
        public void AFamilysTemplateWrittenAsItStands_IsNamedAndNotFoldedIntoTheFamily()
        {
            IReadOnlyList<NarrativeFinding> findings = Checked(TemplateDialogueJson, NoQuestsJson);

            Assert.AreEqual(1, findings.Count(finding => finding.Kind == NarrativeFindingKind.Incomplete
                                                        && finding.Where == TemplateWhere
                                                        && finding.Message.Contains(KillTemplate, StringComparison.Ordinal)),
                Lines(findings));

            FactKeyRegistry registry = Read(TemplateDialogueJson, NoQuestsJson);

            Assert.IsFalse(registry.Keys.Any(key => !key.Declared && FactKeyDeclarations.Unfilled(key.Key)),
                "a template nobody filled in was taken into the registry as a key of its own");

            FactKeyEntry family = Key(registry, KillTemplate);

            Assert.IsFalse(family.Readers.Any(reader => reader.Contains(ForgedNpc, StringComparison.Ordinal)),
                "the template was folded into the very family it is spelled after");
        }

        [TestMethod]
        public void AKeyWrittenEmpty_IsNamedAndNotTakenIn()
        {
            IReadOnlyList<NarrativeFinding> findings = Checked(EmptyKeyDialogueJson, NoQuestsJson);

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.MissingParameter
                                                  && finding.Message.Contains("where a fact key is meant", StringComparison.Ordinal)),
                Lines(findings));

            Assert.IsFalse(Read(EmptyKeyDialogueJson, NoQuestsJson).Keys.Any(key => key.Key.Length == 0),
                "an empty word was taken into the registry as a key");
        }

        /// <summary>No code of the game writes a fact key as a literal: every key it keeps is built, which
        /// is what lets the registry name who writes each one. A call the builders do not go through is a
        /// key the tool cannot offer and the checks answer for by halves.
        /// <para>A lint and not an invariant. The walk reads one line at a time and matches the call as it
        /// is spelt, so a key handed over through a local of another name, or a call broken across lines,
        /// goes past it. It catches the way the mistake is actually made and claims nothing more.</para></summary>
        [TestMethod]
        public void NoCodeOfTheGame_WritesAFactKeyAsALiteral()
        {
            string root = SourceRoot();
            List<string> written = [];

            foreach (string file in s_sourceFolders.SelectMany(folder => Sources(Path.Combine(root, folder))))
                written.AddRange(Literals(file));

            Assert.AreEqual(0, written.Count,
                $"a fact key is written as a literal rather than built:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", written)}");
        }

        /// <summary>Every builder of the code, read off the type itself so that one added without a family
        /// beside it is caught rather than quietly unanswered.</summary>
        private static IEnumerable<MethodInfo> Builders() =>
            typeof(FactKeys).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.ReturnType == typeof(string));

        /// <summary>One key as a builder writes it, with every argument filled: a word for the strings, and
        /// the first member for an enum, which is as much as a family needs to be recognised by.</summary>
        private static string Built(MethodInfo builder) =>
            (string)builder.Invoke(null, [.. builder.GetParameters().Select(parameter => Filled(parameter.ParameterType))])!;

        private static object Filled(Type type) => type.IsEnum ? Enum.GetValues(type).GetValue(0)! : Probe;

        /// <summary>Every parameter of the whole vocabulary, under the record writing it.</summary>
        private static IEnumerable<(NarrativeRecordSpec Record, NarrativeParameterSpec Parameter)> Parameters() =>
            NarrativeVocabulary.Conditions.Concat(NarrativeVocabulary.Actions)
                .SelectMany(record => record.Parameters.Select(parameter => (record, parameter)));

        private static FactKeyEntry Key(FactKeyRegistry registry, string key) =>
            registry.Keys.SingleOrDefault(entry => entry.Key == key)
            ?? throw new AssertFailedException(
                $"'{key}' is in no row of the registry:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", registry.Keys.Select(entry => entry.Key))}");

        private static FactKeyRegistry Read(string dialogues, string quests) => Reading(dialogues, quests).Facts;

        private static IReadOnlyList<NarrativeFinding> Checked(string dialogues, string quests) => Reading(dialogues, quests).Findings;

        /// <summary>One forged pair of documents read through the game's own rules, with every id and every
        /// line answered: what the registry holds is the documents' own doing and not the bench's.</summary>
        private static NarrativeReading Reading(string dialogues, string quests)
        {
            var dialogue = Records<DialoguesData>(dialogues);
            var quest = Records<QuestsData>(quests);

            return NarrativeChecks.Read(new NarrativeCheckInput
            {
                Dialogues = dialogue.Dialogues,
                Quests = quest.Quests,
                Npcs = [],
                LoadedDialogues = [.. dialogue.Dialogues.Select(entry => entry.NpcId)],
                LoadedQuests = [.. quest.Quests.Select(entry => entry.Id)],
                Ids = new NarrativeIdSource(_ => true, (_, _) => true),
                Texts = new NarrativeTextSource(LocalizedTexts.ReferenceLocale, [LocalizedTexts.ReferenceLocale], (_, _) => true)
            });
        }

        private static T Records<T>(string json) => JsonConvert.DeserializeObject<T>(json)!;

        private static string Lines(IReadOnlyList<NarrativeFinding> findings) =>
            string.Join($"{Environment.NewLine}  ", findings.Select(finding => $"{finding.Kind}  {finding.Where}  —  {finding.Message}"));

        /// <summary>Where the game's own code is written, walked up to from wherever the test is running.
        /// The folder holding the narrative is what names it: a folder called Core is not rare, and one
        /// holding the narrative beneath it is this repository and no other.</summary>
        private static string SourceRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Core", "Narrative", "Facts"))) return directory.FullName;

                directory = directory.Parent;
            }

            throw new InvalidOperationException($"the game's own source is in no folder above {AppContext.BaseDirectory}");
        }

        /// <summary>Every source file of one folder, less what a build laid down there.</summary>
        private static IEnumerable<string> Sources(string folder) =>
            Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                               && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                               && !path.Contains($"{Path.DirectorySeparatorChar}.godot{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        /// <summary>The lines of one file handing a literal straight to the facts. The builders themselves
        /// are passed over: the heads are spelt there once, which is the whole point of them.</summary>
        private static IEnumerable<string> Literals(string path)
        {
            if (string.Equals(Path.GetFileName(path), $"{nameof(FactKeys)}.cs", StringComparison.Ordinal)) yield break;

            string[] lines = File.ReadAllLines(path);

            for (int index = 0; index < lines.Length; index++)
                if (s_literalKey.IsMatch(lines[index]))
                    yield return $"{Path.GetFileName(path)}:{index + 1}  {lines[index].Trim()}";
        }

        /// <summary>A conversation raising a fact of the author's own and asking about it, raising one
        /// nobody reads, asking about one nobody raises, and asking about a key of a family the code
        /// keeps.</summary>
        private const string ForgedDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dialogue_Forged_Greeting" } ],
                      "onEnter": [ { "type": "SetFact", "key": "Fact_Nobody_Reads" } ],
                      "options": [
                        {
                          "id": "Raise",
                          "key": "Dialogue_Forged_Raise",
                          "visibleConditions": [ { "type": "Fact", "key": "Fact_Author_Invented" } ],
                          "enabledConditions": [
                            { "type": "Fact", "key": "Fact_Nobody_Writes" },
                            { "type": "Fact", "key": "Npc_Talked:Npc_Forged" }
                          ],
                          "actions": [ { "type": "SetFact", "key": "Fact_Author_Invented" } ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A quest whose objective counts a fact of the author's own.</summary>
        private const string ForgedQuestJson =
            """
            {
              "quests": [
                {
                  "id": "Quest_Forged",
                  "giverNpcId": "Npc_Forged",
                  "stages": [
                    {
                      "id": "Only",
                      "objectives": [ { "id": "Count", "counter": { "key": "Fact_Counted", "amount": 1 } } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A conversation asking about the head of a family and about the head with nothing
        /// written after it: neither names a member of that family.</summary>
        private const string BoundaryDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dialogue_Forged_Greeting" } ],
                      "options": [
                        {
                          "id": "Ask",
                          "key": "Dialogue_Forged_Ask",
                          "visibleConditions": [
                            { "type": "Fact", "key": "Kill_Count" },
                            { "type": "Fact", "key": "Kill_Count:" },
                            { "type": "Fact", "key": "Item_Equipped_Any:Extra" }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A conversation asking about a family by the way it is spelled: the template as the tool
        /// offers it, with the parameter left standing where the npc's own name belongs.</summary>
        private const string TemplateDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dialogue_Forged_Greeting" } ],
                      "options": [
                        {
                          "id": "Ask",
                          "key": "Dialogue_Forged_Ask",
                          "visibleConditions": [ { "type": "Fact", "key": "Kill_Count:<npcId>" } ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string EmptyKeyDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dialogue_Forged_Greeting" } ],
                      "options": [
                        {
                          "id": "Raise",
                          "key": "Dialogue_Forged_Raise",
                          "actions": [ { "type": "SetFact", "key": "" } ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string NoQuestsJson = """{ "quests": [] }""";
    }
}
