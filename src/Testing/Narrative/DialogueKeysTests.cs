namespace LastBreathTest.Narrative
{
    using Core.Data.DialogueData;
    using Core.Data.GameData;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Validation;
    using LastBreath.Descriptors;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;
    using Tooling.Localization;

    /// <summary>
    /// The one rule saying what a line and an option of a conversation are read under, and the walk the
    /// authoring tool words the shipped files by. One rule: the tool that writes the keys, the checks that
    /// hold the files to them and the migration that named the old ones again all ask here.
    /// </summary>
    [TestClass]
    public class DialogueKeysTests
    {
        private const string Npc = "Npc_Bandit_Veteran";

        private const string Node = "Greeting";

        /// <summary>The name an npc of the forged run is renamed to, and what the step is called on the
        /// history — the data editor names it after the word the author typed.</summary>
        private const string Peasant = "Npc_Peasant";

        private const string RenameStepText = "renamed to " + Peasant;

        /// <summary>Where the one forged conversation of a case is written.</summary>
        private const string ConversationAt = "/dialogues/0";

        /// <summary>How a name a wording waits under is written in a locale file, read as the whole of a
        /// key and not as a mark somewhere in a sentence. No key of the game is written with it, so a file
        /// still holding one is a pass that stopped half way.</summary>
        private const string ParkedId = "msgid \"~";

        /// <summary>A name left in a locale by exactly such a pass: the third wording of a node, parked on
        /// its way and never named again.</summary>
        private const string StoppedPassKey = "~2~Dlg_Forged_Greeting_2";

        /// <summary>The folders of locale files this test wrote, taken back out once it is over.</summary>
        private readonly List<string> _folders = [];

        [TestCleanup]
        public void TakeTheWrittenLocalesBackOut()
        {
            foreach (string folder in _folders.Where(Directory.Exists))
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                }
                catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
                {
                    // A run that cannot take its own files back out is not a run that failed.
                }
            }
        }

        [TestMethod]
        public void ALineIsReadUnderItsNpcItsNodeAndItsPlaceInTheNode()
        {
            Assert.AreEqual("Dlg_Bandit_Veteran_Greeting_1", DialogueKeys.Expected(Npc, Node, 0));
            Assert.AreEqual("Dlg_Bandit_Veteran_Greeting_2", DialogueKeys.Expected(Npc, Node, 1));
        }

        /// <summary>The word saying an id belongs to an npc is left out: the key already says which
        /// catalog it is of, and an id written without that word is taken as it stands.</summary>
        [TestMethod]
        public void TheNpcWordIsNotWrittenTwice()
        {
            Assert.AreEqual("Bandit_Veteran", DialogueKeys.Speaker(Npc));
            Assert.AreEqual("Stranger", DialogueKeys.Speaker("Stranger"));
        }

        [TestMethod]
        public void AnOptionIsReadUnderTheNameItsRoutesAlreadyCallItBy() =>
            Assert.AreEqual("Dlg_Bandit_Veteran_Greeting_AskWork", DialogueKeys.Expected(Npc, Node, "AskWork"));

        /// <summary>A place nobody has named words no key. Said as nothing rather than as half a key: the
        /// rules answer for the unnamed place itself, and a key of one part would be a key the tool wrote.</summary>
        [TestMethod]
        public void APlaceWithNoNameWordsNoKey()
        {
            Assert.AreEqual(string.Empty, DialogueKeys.Expected(string.Empty, Node, 0));
            Assert.AreEqual(string.Empty, DialogueKeys.Expected(Npc, string.Empty, 0));
            Assert.AreEqual(string.Empty, DialogueKeys.Expected(Npc, Node, string.Empty));
        }

        [TestMethod]
        public void ALineBeforeTheFirstOneIsRefused() =>
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => DialogueKeys.Expected(Npc, Node, -1));

        /// <summary>The options no structure words, and everything else: a key that merely begins with the
        /// shared word is not one of them — the list is names and not a prefix, or a conversation could
        /// take a key out of the shared set by spelling one.</summary>
        [TestMethod]
        public void OnlyTheOptionsNamedAreShared()
        {
            Assert.IsTrue(DialogueKeys.IsShared(DialogueKeys.Leave));
            Assert.IsTrue(DialogueKeys.IsShared(DialogueKeys.Back));
            Assert.IsTrue(DialogueKeys.IsShared(DialogueKeys.TrialHint));
            Assert.IsTrue(DialogueKeys.IsShared(DialogueKeys.TrialTurnIn));

            Assert.IsFalse(DialogueKeys.IsShared("Dlg_Opt_Whatever"));
            Assert.IsFalse(DialogueKeys.IsShared("Dlg_Bandit_Veteran_Greeting_1"));
            Assert.IsFalse(DialogueKeys.IsShared(string.Empty));
        }

        /// <summary>Every shared option is written in the shared namespace, which is what tells an author
        /// reading a file that the sentence belongs to no one conversation.</summary>
        [TestMethod]
        public void EverySharedOptionIsWrittenUnderTheSharedWord()
        {
            foreach (string key in DialogueKeys.SharedOptions)
                Assert.IsTrue(key.StartsWith(DialogueKeys.SharedPrefix, StringComparison.Ordinal),
                    $"'{key}' is listed as shared and is not written under '{DialogueKeys.SharedPrefix}'");
        }

        /// <summary>What the shipped conversations are actually read under: every key of every line and
        /// option is the one its place words, or one of the shared options. The pin of the whole card —
        /// the migration is done when this holds and nothing in the data says otherwise.</summary>
        [TestMethod]
        public void EveryShippedLineAndOptionIsReadUnderTheKeyItsPlaceWords()
        {
            List<string> off = [];

            foreach (DialogueEntry dialogue in ShippedDialogues())
                foreach (DialogueNodeEntry node in dialogue.Nodes)
                    Wandered(off, dialogue.NpcId, node);

            Assert.AreEqual(0, off.Count,
                $"keys of the shipped conversations do not follow their places:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", off));
        }

        /// <summary>No two places of one conversation word the same key: the second one's text would
        /// stand over the first one's in every locale with nothing in either file to show it.</summary>
        [TestMethod]
        public void NoTwoShippedPlacesWordOneKey()
        {
            foreach (DialogueEntry dialogue in ShippedDialogues())
            {
                HashSet<string> worded = new(StringComparer.Ordinal);

                foreach (DialogueNodeEntry node in dialogue.Nodes)
                {
                    for (int line = 0; line < node.Lines.Count; line++)
                        Assert.IsTrue(worded.Add(DialogueKeys.Expected(dialogue.NpcId, node.Id, line)),
                            $"two places of '{dialogue.NpcId}' word one key at '{node.Id}' line {line}");

                    foreach (DialogueOptionEntry option in node.Options.Where(option => !DialogueKeys.IsShared(option.Key)))
                        Assert.IsTrue(worded.Add(DialogueKeys.Expected(dialogue.NpcId, node.Id, option.Id)),
                            $"two places of '{dialogue.NpcId}' word one key at '{node.Id}/{option.Id}'");
                }
            }
        }

        /// <summary>Every shared option in the list is actually offered in more than one place. A key
        /// listed as shared and used once is a line the structure could word and does not, which is the
        /// whole of what the list is for.</summary>
        [TestMethod]
        public void EverySharedOptionIsOfferedMoreThanOnce()
        {
            Dictionary<string, int> offered = DialogueKeys.SharedOptions.ToDictionary(key => key, _ => 0, StringComparer.Ordinal);

            foreach (DialogueOptionEntry option in ShippedDialogues()
                         .SelectMany(dialogue => dialogue.Nodes)
                         .SelectMany(node => node.Options))
            {
                if (offered.ContainsKey(option.Key)) offered[option.Key]++;
            }

            foreach ((string key, int count) in offered)
                Assert.IsTrue(count > 1, $"'{key}' is listed as shared and is offered {count} time(s)");
        }

        /// <summary>The mutation the check exists for: a line read under a key its place does not word is
        /// named, and the option every conversation shares standing beside it is not.</summary>
        [TestMethod]
        public void AKeyOffThePattern_IsFoundAndTheSharedOneIsNot()
        {
            IReadOnlyList<NarrativeFinding> findings = Checked(OffPatternDialogueJson);

            string[] off =
            [
                .. findings.Where(finding => finding.Kind == NarrativeFindingKind.KeyOffPattern).Select(finding => finding.Where)
            ];

            CollectionAssert.AreEquivalent(
                new[] { "Dialogues/Npc_Forged/nodes/Greeting/lines[0]", "Dialogues/Npc_Forged/nodes/Greeting/options/Ask" },
                off,
                string.Join($"{Environment.NewLine}  ", findings.Select(finding => $"{finding.Kind}  {finding.Where}  —  {finding.Message}")));
        }

        /// <summary>Two places wording one key: an option named after the number of a line takes the
        /// line's key, and the pair would share one entry in every locale.</summary>
        [TestMethod]
        public void TwoPlacesWordingOneKey_AreNotPassedOver()
        {
            IReadOnlyList<NarrativeFinding> findings = Checked(TwiceWordedDialogueJson);

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.DuplicateId
                                                  && finding.Message.Contains("Dlg_Forged_Greeting_1", StringComparison.Ordinal)),
                string.Join($"{Environment.NewLine}  ", findings.Select(finding => $"{finding.Kind}  {finding.Where}  —  {finding.Message}")));
        }

        // ── the walk the tool writes the keys by ────────────────────────────────────────────────

        /// <summary>A line the author has just added carries no key, and the tool writes the one its
        /// place words rather than leaving the author to spell it.</summary>
        [TestMethod]
        public void ALineWithNoKey_IsGivenTheOneItsPlaceWords()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(FreshLineDialogueJson);

            DialogueKeyResult result = Followed(document);

            Assert.AreEqual(1, result.Written);
            Assert.AreEqual(0, result.Moved);
            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/1/key"));
        }

        /// <summary>A node renamed carries the wording of its lines and options with it — the keys in the
        /// file and the entries of every locale, as one gesture.</summary>
        [TestMethod]
        public void ANodeRenamed_TakesItsWordingWithIt()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(RenamedNodeDialogueJson);
            (LocalizedTexts texts, _) = Locales(("Dlg_Forged_Greeting_1", "Hello."), ("Dlg_Forged_Greeting_Ask", "What now?"));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(2, result.Moved, "the lines and options of the renamed node did not follow it");
            Assert.AreEqual("Dlg_Forged_Hello_1", Key(document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Dlg_Forged_Hello_Ask", Key(document, "/dialogues/0/nodes/0/options/0/key"));

            Assert.AreEqual("Hello.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Hello_1"),
                "the text stayed under the key the node used to word");
            Assert.IsNull(texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_1"),
                "the old key was left standing beside the new one");
        }

        /// <summary>The option every conversation shares is worded by no place and moves with none: a node
        /// renamed leaves "Leave." exactly where it was.</summary>
        [TestMethod]
        public void ASharedOption_IsLeftWhereItStands()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(SharedOptionDialogueJson);

            Assert.AreEqual(0, DialogueKeyPlan.OffPattern(document, JsonPointer.Parse("/dialogues/0")).Count);
            Assert.AreEqual(DialogueKeys.Leave, Key(document, "/dialogues/0/nodes/0/options/0/key"));
        }

        /// <summary>A move onto a key some locale already writes is refused, and the place left as it
        /// stands: writing it would hand this line the words of whatever wrote that key first.</summary>
        [TestMethod]
        public void AMoveOntoAKeyAlreadyWritten_IsRefusedAndSaid()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(RenamedNodeDialogueJson);
            (LocalizedTexts texts, _) = Locales(("Dlg_Forged_Greeting_1", "Hello."), ("Dlg_Forged_Hello_1", "Somebody else's."));

            DialogueKeyResult result = Followed(document, texts);

            CollectionAssert.AreEqual(new[] { "Dlg_Forged_Hello_1" }, result.Taken.ToArray());
            Assert.AreEqual("Dlg_Forged_Greeting_1", Key(document, "/dialogues/0/nodes/0/lines/0/key"),
                "the line was pointed at a key holding somebody else's words");
            Assert.AreEqual("Somebody else's.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Hello_1"));
        }

        /// <summary>Two lines of one node swapped: each wants the key the other is leaving. Written place
        /// by place in file order neither could move — the first would find its key held by the second and
        /// the second by the first — and the node would be left reading the wrong way round for good.</summary>
        [TestMethod]
        public void TwoLinesSwapped_TradeTheirKeysAndTheirTexts()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(SwappedLinesDialogueJson);
            (LocalizedTexts texts, string folder) = Locales(("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Second."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(2, result.Moved, "the lines did not trade their keys");
            Assert.AreEqual(0, result.Taken.Count, string.Join(", ", result.Taken));

            Assert.AreEqual("Dlg_Forged_Greeting_1", Key(document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/1/key"));

            Assert.AreEqual("Second.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_1"),
                "the text did not follow the line it was written for");
            Assert.AreEqual("First.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));

            Assert.IsFalse(Parked(texts, folder),
                "a wording was parked out of the way of the swap and left there");
        }

        /// <summary>Three lines of a node moved round: each wants the key the next one is leaving and the
        /// last wants the first's. Nothing here can be done one place at a time — every one of the three
        /// is in the way of another — so the whole ring is parked before any of it is named again.</summary>
        [TestMethod]
        public void ThreeLinesInACycle_TradeTheirKeysAndTheirTexts()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(CycledLinesDialogueJson);
            (LocalizedTexts texts, string folder) = Locales(
                ("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Second."), ("Dlg_Forged_Greeting_3", "Third."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(3, result.Moved, "the ring of lines did not trade its keys");
            Assert.AreEqual(0, result.Taken.Count, string.Join(", ", result.Taken));

            Assert.AreEqual("Dlg_Forged_Greeting_1", Key(document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/1/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_3", Key(document, "/dialogues/0/nodes/0/lines/2/key"));

            Assert.AreEqual("Second.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_1"));
            Assert.AreEqual("Third.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));
            Assert.AreEqual("First.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_3"));

            Assert.IsFalse(Parked(texts, folder),
                "a wording was parked out of the way of the ring and left there");
        }

        /// <summary>
        /// A line added in the middle of a node whose last line cannot move: the wording of a line long
        /// gone is written under the key that last one would take, so it stays where it is — and so must
        /// everything in front of it, all the way back to the fresh line.
        /// <para>The refusal is decided before a key is written, and this is what that is for: the fresh
        /// line would otherwise be given the key of a neighbour that turns out to be staying, and the two
        /// of them would be read under one text with the report saying nothing about it.</para>
        /// </summary>
        [TestMethod]
        public void AFreshLineBeforeARefusedTail_LeavesEveryPlaceBehindItWhereItStands()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(FreshLineBeforeAShiftedTailDialogueJson);
            (LocalizedTexts texts, _) = Locales(
                ("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Second."),
                ("Dlg_Forged_Greeting_3", "Third."), ("Dlg_Forged_Greeting_4", "Of a line long gone."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(0, result.Written);
            Assert.AreEqual(0, result.Moved);
            Assert.IsFalse(texts.IsDirty, "a pass that refused every place still moved a wording");

            // Every place of the cascade is named, in the order of the places they stopped: the author is
            // told which text to take out, and the pass that stopped is the one that told him.
            CollectionAssert.AreEqual(
                new[] { "Dlg_Forged_Greeting_2", "Dlg_Forged_Greeting_3", "Dlg_Forged_Greeting_4" },
                result.Taken.ToArray());

            Assert.IsNull(Key(document, "/dialogues/0/nodes/0/lines/1/key"),
                "the fresh line was given a key the line below it still reads");
            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/2/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_3", Key(document, "/dialogues/0/nodes/0/lines/3/key"));

            Assert.AreEqual("Second.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));
        }

        /// <summary>A node whose lines move and a node whose last line cannot, in one conversation: the
        /// refusal stops the places behind it and nothing else. What moved is written, what stopped is
        /// named, and no wording is left waiting under a name nothing reads.</summary>
        [TestMethod]
        public void ARefusalInOneNode_LeavesTheOtherNodeMovedAndNothingParked()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(TwoNodesDialogueJson);
            (LocalizedTexts texts, string folder) = Locales(
                ("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Second."),
                ("Dlg_Forged_Parting_1", "Bye."), ("Dlg_Forged_Parting_2", "Of a line long gone."),
                ("Dlg_Forged_Parting_3", "Third."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(2, result.Moved, "the lines of the node that could move did not");
            CollectionAssert.AreEqual(new[] { "Dlg_Forged_Parting_2" }, result.Taken.ToArray());

            Assert.AreEqual("Second.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_1"));
            Assert.AreEqual("First.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));

            Assert.AreEqual("Dlg_Forged_Parting_3", Key(document, "/dialogues/0/nodes/1/lines/1/key"),
                "the line whose key was refused was pointed somewhere anyway");
            Assert.AreEqual("Third.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Parting_3"));

            Assert.IsFalse(Parked(texts, folder),
                "a wording was parked on its way and left there");
        }

        /// <summary>A line taken out of a node while the wording it was read under is still written: every
        /// line after it wants the key of the one before, and the first of them cannot have it. The whole
        /// cascade stops rather than half of it moving — a node read one line out of step is worse than a
        /// node the author is told to take one stale text out of.</summary>
        [TestMethod]
        public void ALineTakenOutWhileItsWordingStands_StopsTheWholeCascade()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(ShortenedLongNodeDialogueJson);
            (LocalizedTexts texts, _) = Locales(
                ("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Of a line long gone."),
                ("Dlg_Forged_Greeting_3", "Third."), ("Dlg_Forged_Greeting_4", "Fourth."),
                ("Dlg_Forged_Greeting_5", "Fifth."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(0, result.Written);
            Assert.AreEqual(0, result.Moved);
            CollectionAssert.AreEqual(
                new[] { "Dlg_Forged_Greeting_2", "Dlg_Forged_Greeting_3", "Dlg_Forged_Greeting_4" },
                result.Taken.ToArray());

            Assert.AreEqual("Dlg_Forged_Greeting_3", Key(document, "/dialogues/0/nodes/0/lines/1/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_4", Key(document, "/dialogues/0/nodes/0/lines/2/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_5", Key(document, "/dialogues/0/nodes/0/lines/3/key"));

            Assert.IsFalse(texts.IsDirty, "a pass that refused every place still moved a wording");
        }

        /// <summary>
        /// A locale still holding a name from a pass that stopped half way — which is the name the third
        /// line's wording would have waited under. Whether a wording can be taken out of the way is part
        /// of what a pass settles before it writes anything, so that place is refused where it stands, and
        /// the cascade behind it follows: the second line goes on reading the key the third one wanted,
        /// the first goes on reading the key the second wanted, and the node is left exactly as it was.
        /// <para>A pass that asked the question while writing would have moved the first line onto the key
        /// the second is still reading, and left two places of one node under one name with nothing in
        /// either file to show it.</para>
        /// </summary>
        [TestMethod]
        public void AWordingThatCannotBeParked_LeavesTheWholeNodeStandingAndIsSaid()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(ShiftedLinesDialogueJson);
            (LocalizedTexts texts, _) = Locales(
                ("Dlg_Forged_Greeting_9", "Ninth."), ("Dlg_Forged_Greeting_1", "First."),
                ("Dlg_Forged_Greeting_2", "Second."), (StoppedPassKey, "Left by a pass that stopped."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(0, result.Written);
            Assert.AreEqual(0, result.Moved);
            Assert.AreEqual(0, result.Parked.Count, string.Join(", ", result.Parked));
            Assert.IsFalse(texts.IsDirty, "a pass that refused every place still moved a wording");

            // Every place of the cascade is named, and the last of them by the name that actually stopped
            // it: the stale text the author has to take out is the one from the pass that stopped.
            CollectionAssert.AreEqual(
                new[] { "Dlg_Forged_Greeting_1", "Dlg_Forged_Greeting_2", StoppedPassKey }, result.Taken.ToArray());

            Assert.AreEqual("Dlg_Forged_Greeting_9", Key(document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_1", Key(document, "/dialogues/0/nodes/0/lines/1/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/2/key"));

            Assert.AreEqual("First.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_1"));
            Assert.AreEqual("Second.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));
        }

        /// <summary>Two places wording one key, and that key holding the words of a line long gone: both
        /// are refused and the name in the way is one thing for the author to go and look at. Said twice
        /// it would read as two stale texts, and he would go looking for a second that is not there.</summary>
        [TestMethod]
        public void OneNameStoppingTwoPlaces_IsSaidOnce()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(TwoPlacesWantingOneKeyDialogueJson);
            (LocalizedTexts texts, _) = Locales(
                ("Dlg_Forged_Greeting_9", "Ninth."), ("Dlg_Forged_Greeting_8", "Eighth."),
                ("Dlg_Forged_Greeting_1", "Of a line long gone."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(0, result.Written);
            Assert.AreEqual(0, result.Moved);

            CollectionAssert.AreEqual(new[] { "Dlg_Forged_Greeting_1" }, result.Taken.ToArray());

            Assert.AreEqual("Dlg_Forged_Greeting_9", Key(document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_8", Key(document, "/dialogues/0/nodes/0/options/0/key"));
            Assert.IsFalse(texts.IsDirty, "a pass that refused every place still moved a wording");
        }

        /// <summary>A line taken out of the middle of a node: everything after it is read one place
        /// earlier, and each of them keeps the words written for it.</summary>
        [TestMethod]
        public void ALineTakenOutOfTheMiddle_ShiftsTheTailAndItsTexts()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(ShortenedNodeDialogueJson);
            (LocalizedTexts texts, _) = Locales(("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_3", "Third."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(1, result.Moved);
            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/1/key"));

            Assert.AreEqual("First.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_1"));
            Assert.AreEqual("Third.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));
            Assert.IsNull(texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_3"));
        }

        /// <summary>A line added in the middle of a node takes the key of the line it now stands before,
        /// and that one moves on with its words: the fresh line is read under a key holding nothing, which
        /// is what an author who has just added a line expects to find.</summary>
        [TestMethod]
        public void ALineAddedInTheMiddle_TakesTheKeyTheNextOneLeaves()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(LengthenedNodeDialogueJson);
            (LocalizedTexts texts, _) = Locales(("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Second."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(1, result.Written);
            Assert.AreEqual(1, result.Moved);

            Assert.AreEqual("Dlg_Forged_Greeting_2", Key(document, "/dialogues/0/nodes/0/lines/1/key"));
            Assert.AreEqual("Dlg_Forged_Greeting_3", Key(document, "/dialogues/0/nodes/0/lines/2/key"));

            Assert.AreEqual("Second.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_3"),
                "the line that moved down did not take its words with it");
            Assert.IsNull(texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"),
                "the fresh line was handed the words of the line below it");
        }

        /// <summary>A line added where a line taken out earlier left its wording behind: the key its place
        /// words is already written, and writing it would show the author the removed line's text with
        /// nothing on screen saying where it came from. Refused and named, and the stale text is the
        /// author's to take out.</summary>
        [TestMethod]
        public void AFreshLineOntoALeftoverKey_IsRefusedAndSaid()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(FreshLineDialogueJson);
            (LocalizedTexts texts, _) = Locales(("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Of a line long gone."));

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(0, result.Written);
            CollectionAssert.AreEqual(new[] { "Dlg_Forged_Greeting_2" }, result.Taken.ToArray());

            Assert.IsNull(Key(document, "/dialogues/0/nodes/0/lines/1/key"),
                "the fresh line was pointed at words written for another one");
            Assert.AreEqual("Of a line long gone.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Forged_Greeting_2"));
        }

        /// <summary>A line standing on one of the sentences every conversation shares is worded like any
        /// other line — and the sentence stays where it is: it belongs to the options offering it, and a
        /// line reading it was reading words that answer to no place at all.</summary>
        [TestMethod]
        public void ALineOnASharedKey_IsWordedAndTheSharedTextStays()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(SharedKeyOnALineDialogueJson);
            (LocalizedTexts texts, _) = Locales((DialogueKeys.Leave, "Leave."));

            Assert.AreEqual(1, DialogueKeyPlan.OffPattern(document, JsonPointer.Parse("/dialogues/0")).Count,
                "a line was left reading a sentence its place does not word");

            DialogueKeyResult result = Followed(document, texts);

            Assert.AreEqual(1, result.Written);
            Assert.AreEqual("Dlg_Forged_Greeting_1", Key(document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual(DialogueKeys.Leave, Key(document, "/dialogues/0/nodes/0/options/0/key"));

            Assert.AreEqual("Leave.", texts.Read(LocalizedTexts.ReferenceLocale, DialogueKeys.Leave),
                "the line carried the shared sentence away from the options offering it");
        }

        // ── the whole run walked after an npc is renamed ────────────────────────────────────────

        /// <summary>An npc renamed in the data editor: his id is written into the conversation he speaks,
        /// and every line and option of it goes on being read under keys worded from the name he no longer
        /// has. The walk over the run brings them back — in the file and in every locale at once.</summary>
        [TestMethod]
        public void AnNpcRenamed_CarriesTheKeysOfHisLinesWithHim()
        {
            (CatalogWorkspace workspace, LocalizedTexts texts, EditHistory history) = Forged();
            JsonTreeDocument npcs = Renamed(workspace, history);

            DialogueKeysFollowed followed = DialogueKeyFollow.All(workspace, texts, history, npcs);

            Assert.AreEqual(1, followed.Conversations);
            Assert.AreEqual(2, followed.Keys.Moved, "the lines and options of the renamed npc did not follow him");
            Assert.AreEqual(0, followed.Keys.Taken.Count, string.Join(", ", followed.Keys.Taken));

            JsonTreeDocument spoken = Conversations(workspace)[0].File.Document;

            Assert.AreEqual("Dlg_Peasant_Greeting_1", Key(spoken, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Dlg_Peasant_Greeting_Ask", Key(spoken, "/dialogues/0/nodes/0/options/0/key"));

            Assert.AreEqual("Good day.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Peasant_Greeting_1"));
            Assert.AreEqual("What is the news?", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Peasant_Greeting_Ask"));
            Assert.IsNull(texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Villager_Greeting_1"),
                "the old key was left standing beside the new one");
        }

        /// <summary>The sentence every conversation shares stays where it is, and so does the conversation
        /// of the npc nobody renamed: the walk reads every record of the catalog and writes only the places
        /// whose own structure has stopped wording the key they carry.</summary>
        [TestMethod]
        public void ARenameLeavesTheSharedOptionAndTheOtherConversationsAlone()
        {
            (CatalogWorkspace workspace, LocalizedTexts texts, EditHistory history) = Forged();
            JsonTreeDocument npcs = Renamed(workspace, history);

            DialogueKeysFollowed followed = DialogueKeyFollow.All(workspace, texts, history, npcs);

            Assert.AreEqual(1, followed.Conversations, "a conversation nobody renamed was written into");

            JsonTreeDocument spoken = Conversations(workspace)[0].File.Document;

            Assert.AreEqual(DialogueKeys.Leave, Key(spoken, "/dialogues/0/nodes/0/options/1/key"));
            Assert.AreEqual("Leave.", texts.Read(LocalizedTexts.ReferenceLocale, DialogueKeys.Leave));

            Assert.AreEqual("Dlg_Hunter_Greeting_1", Key(spoken, "/dialogues/1/nodes/0/lines/0/key"));
            Assert.AreEqual("Quiet today.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Hunter_Greeting_1"));
        }

        /// <summary>The rename and the wording that followed it are one thing to take back: a step of its
        /// own would leave one press of undo showing an npc under his old name with his lines read under
        /// the new one, which is a state nobody asked for and nothing on screen explains.</summary>
        [TestMethod]
        public void TheKeysFollowingARename_AreOneStepWithIt()
        {
            (CatalogWorkspace workspace, LocalizedTexts texts, EditHistory history) = Forged();
            JsonTreeDocument npcs = Renamed(workspace, history);

            DialogueKeyFollow.All(workspace, texts, history, npcs);

            Assert.AreEqual(1, history.Depth, "the wording was filed beside the rename instead of with it");

            history.Undo();

            JsonTreeDocument spoken = Conversations(workspace)[0].File.Document;

            Assert.AreEqual("Dlg_Villager_Greeting_1", Key(spoken, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Good day.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Villager_Greeting_1"));
            Assert.AreEqual(0, history.Depth);
        }

        /// <summary>
        /// The chain the tool actually calls: the name carried everywhere by the catalogs, and the wording
        /// brought after it by the pass. One step of the history for both — the pass groups with the newest
        /// step instead of filing one of its own — so a record stepped back to its old name is read under
        /// that name by its conversation too.
        /// </summary>
        [TestMethod]
        public void ARenameCarriedByTheCatalogs_IsOneStepWithTheKeysThatFollowIt()
        {
            (CatalogWorkspace workspace, LocalizedTexts texts, EditHistory history) = Forged();
            CatalogView npcs = workspace.Catalogs.Single(view => view.Catalog == DataCatalog.Npc);
            CatalogRecord npc = npcs.Records[0];
            JsonTreeDocument document = npc.File.Document;
            string was = npc.CurrentId;

            // The id in the record's own file is written by whoever renamed it — the box the author types
            // in — and taken into the step the catalogs open around everything naming him.
            document.SetValue(npc.Pointer.Append(NpcCatalogDescriptor.IdField), new JValue(Peasant));

            CatalogRenameResult carried = CatalogEditing.RenameEverywhere(
                new ReferenceUses(workspace, [new NarrativeReferenceUses()]), npcs, npc, was, Peasant, texts);

            Assert.IsNull(carried.Refused, carried.Refused);
            Assert.AreNotEqual(0, carried.Uses, "the conversation was left naming an npc under his old name");

            DialogueKeysFollowed followed = DialogueKeyFollow.Over(Conversations(workspace), texts, history, document);

            Assert.AreEqual(1, followed.Conversations, "the lines were left reading keys worded from the old name");
            Assert.AreEqual("Dlg_Peasant_Greeting_1",
                Key(Conversations(workspace)[0].File.Document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual(1, history.Depth, "the rename and the wording that followed it are two steps to take back");

            history.Undo();

            Assert.AreEqual(was, npc.CurrentId, "the name itself was not taken back with the wording");
            Assert.AreEqual("Dlg_Villager_Greeting_1",
                Key(Conversations(workspace)[0].File.Document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual("Good day.", texts.Read(LocalizedTexts.ReferenceLocale, "Dlg_Villager_Greeting_1"));
            Assert.AreEqual(0, history.Depth);
        }

        /// <summary>Whether a settled pass is going to write anything is what decides that a step of the
        /// history is opened at all. A pass that refused every place changed no file, and a step opened for
        /// it would take the author's own edit into a gesture of the tool's making and leave files called
        /// changed by a pass that wrote nothing.</summary>
        [TestMethod]
        public void APassRefusingEveryPlace_SaysItWillWriteNothing()
        {
            JsonTreeDocument stopped = JsonTreeDocument.Parse(ShiftedLinesDialogueJson);
            (LocalizedTexts stalled, _) = Locales(
                ("Dlg_Forged_Greeting_9", "Ninth."), ("Dlg_Forged_Greeting_1", "First."),
                ("Dlg_Forged_Greeting_2", "Second."), (StoppedPassKey, "Left by a pass that stopped."));

            Assert.IsFalse(Planned(stopped, stalled).Writes,
                "a pass with every place refused would still have opened a step of the history");

            JsonTreeDocument swapped = JsonTreeDocument.Parse(SwappedLinesDialogueJson);
            (LocalizedTexts texts, _) = Locales(
                ("Dlg_Forged_Greeting_1", "First."), ("Dlg_Forged_Greeting_2", "Second."));

            Assert.IsTrue(Planned(swapped, texts).Writes, "a pass trading two keys said it would write nothing");
        }

        /// <summary>The name a wording waits under while the places of a node trade names is written
        /// outside the word every key of a conversation begins with. Nothing else keeps the two apart: a
        /// mark a real key could carry would let a pass park a wording under a name some line is read by,
        /// and the author would lose that line's text to the parking.</summary>
        [TestMethod]
        public void TheNameAWordingWaitsUnder_IsSpelledOutsideTheKeysOfEveryConversation()
        {
            Assert.IsFalse(DialogueKeys.Prefix.StartsWith(DialogueKeyPlan.ParkingWord, StringComparison.Ordinal),
                "a key of a conversation is spelled the way a parked wording is");

            Assert.IsFalse(DialogueKeys.Leave.StartsWith(DialogueKeyPlan.ParkingWord, StringComparison.Ordinal),
                "the option every conversation shares is spelled the way a parked wording is");
        }

        /// <summary>Nothing is done without the locales: the keys would be written into the files alone
        /// and every line would be left pointing at a text no locale holds.</summary>
        [TestMethod]
        public void WithoutTheLocales_NothingIsWritten()
        {
            (CatalogWorkspace workspace, _, EditHistory history) = Forged();
            JsonTreeDocument npcs = Renamed(workspace, history);

            DialogueKeysFollowed followed = DialogueKeyFollow.All(workspace, texts: null, history, npcs);

            Assert.AreEqual(0, followed.Conversations);
            Assert.AreEqual("Dlg_Villager_Greeting_1",
                Key(Conversations(workspace)[0].File.Document, "/dialogues/0/nodes/0/lines/0/key"));
            Assert.AreEqual(1, history.Depth, "a walk that wrote nothing still filed a step");
        }

        /// <summary>A run holding the conversations of the whole game: an npc catalog and a dialogue
        /// catalog on one history, with the locales the lines are read in beside them.</summary>
        private (CatalogWorkspace Workspace, LocalizedTexts Texts, EditHistory History) Forged()
        {
            var history = new EditHistory();
            string root = Path.Combine(Path.GetTempPath(), "LastBreath", "DialogueKeysTests", Guid.NewGuid().ToString("N"));

            _folders.Add(root);

            Written(root, DataCatalog.Npc, ForgedNpcsJson);
            Written(root, DataCatalog.Dialogues, ForgedDialoguesJson);

            CatalogWorkspace workspace = CatalogWorkspace.Load(
                root, [new NpcCatalogDescriptor(), new DialoguesCatalogDescriptor()], history);

            (LocalizedTexts texts, _) = Locales(history,
                ("Dlg_Villager_Greeting_1", "Good day."), ("Dlg_Villager_Greeting_Ask", "What is the news?"),
                ("Dlg_Hunter_Greeting_1", "Quiet today."), (DialogueKeys.Leave, "Leave."));

            return (workspace, texts, history);
        }

        /// <summary>The npc renamed as the data editor renames him: his own id, and the word every record
        /// naming him is written with — one step of the history, and the conversation left reading keys
        /// worded from the name he had. Answers with his document, which is where the step was filed.</summary>
        private static JsonTreeDocument Renamed(CatalogWorkspace workspace, EditHistory history)
        {
            CatalogRecord npc = Records(workspace, DataCatalog.Npc)[0];
            CatalogRecord conversation = Conversations(workspace)[0];
            JsonTreeDocument npcs = npc.File.Document;

            using (history.Group(RenameStepText))
            {
                npcs.SetValue(npc.Pointer.Append(NpcCatalogDescriptor.IdField), new JValue(Peasant));
                conversation.File.Document.SetValue(
                    conversation.Pointer.Append(DialoguesCatalogDescriptor.IdField), new JValue(Peasant));
            }

            return npcs;
        }

        private static IReadOnlyList<CatalogRecord> Conversations(CatalogWorkspace workspace) =>
            Records(workspace, DataCatalog.Dialogues);

        private static IReadOnlyList<CatalogRecord> Records(CatalogWorkspace workspace, string catalog) =>
            workspace.Catalogs.Single(view => view.Catalog == catalog).Records;

        private static void Written(string root, string catalog, string json)
        {
            string folder = Path.Combine(root, catalog);

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, catalog + ".json"), json);
        }

        private static DialogueKeyResult Followed(JsonTreeDocument document, LocalizedTexts? texts = null) =>
            DialogueKeyPlan.Follow(document, Off(document), texts);

        /// <summary>What a pass over the conversation would do, settled and not written.</summary>
        private static DialogueKeyPass Planned(JsonTreeDocument document, LocalizedTexts? texts) =>
            DialogueKeyPlan.Plan(Off(document), texts);

        private static IReadOnlyList<DialogueTextPlace> Off(JsonTreeDocument document) =>
            DialogueKeyPlan.OffPattern(document, JsonPointer.Parse(ConversationAt));

        /// <summary>Whether any locale, once written back, still holds a name a wording waits under. The
        /// pass is done when the files hold the keys of the conversation and nothing else: a name a wording
        /// was parked under on its way and left standing is a text the game reads under nothing, in
        /// whichever file it was left — the wording moves in all of them at once or in none.</summary>
        private static bool Parked(LocalizedTexts texts, string folder)
        {
            texts.SaveAll();

            return texts.Locales.Select(locale => File.ReadAllText(Path.Combine(folder, locale + PoCatalogSet.FileExtension)))
                .Any(saved => saved.Contains(ParkedId, StringComparison.Ordinal));
        }

        private static string? Key(JsonTreeDocument document, string pointer) =>
            document.Resolve(JsonPointer.Parse(pointer))?.Value<string>();

        /// <summary>Two locale files in a folder of their own, written with the keys a case needs, and the
        /// folder they were written into — what a pass wrote back is read from there.</summary>
        private (LocalizedTexts Texts, string Folder) Locales(params (string Key, string Text)[] written) =>
            Locales(history: null, written);

        /// <summary>The same, on the stack a run of the tool keeps: what the locales record joins whatever
        /// step the gesture that moved the keys is being filed as.</summary>
        private (LocalizedTexts Texts, string Folder) Locales(
            EditHistory? history, params (string Key, string Text)[] written)
        {
            string folder = Path.Combine(Path.GetTempPath(), "LastBreath", "DialogueKeysTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            _folders.Add(folder);

            string body = string.Join(Environment.NewLine,
                written.Select(entry => $"msgid \"{entry.Key}\"{Environment.NewLine}msgstr \"{entry.Text}\"{Environment.NewLine}"));

            foreach (string locale in new[] { LocalizedTexts.AuthoringLocale, LocalizedTexts.ReferenceLocale })
                File.WriteAllText(Path.Combine(folder, locale + ".po"), body);

            return (LocalizedTexts.Load(folder, history), folder);
        }

        /// <summary>One forged conversation run through the rules with every id and every key answered:
        /// what is found is the record's own doing.</summary>
        private static IReadOnlyList<NarrativeFinding> Checked(string json)
        {
            var read = JsonConvert.DeserializeObject<DialoguesData>(json)!;

            return NarrativeChecks.Run(new NarrativeCheckInput
            {
                Dialogues = read.Dialogues,
                Quests = [],
                Npcs = [],
                LoadedDialogues = [.. read.Dialogues.Select(entry => entry.NpcId)],
                LoadedQuests = [],
                Ids = new NarrativeIdSource(_ => true, (_, _) => true),
                Texts = new NarrativeTextSource(
                    LocalizedTexts.ReferenceLocale, [LocalizedTexts.ReferenceLocale], (_, _) => true)
            });
        }

        private static IReadOnlyList<DialogueEntry> ShippedDialogues() =>
            [
                .. SharedData.Files(DataCatalog.Dialogues)
                    .Select(path => JsonConvert.DeserializeObject<DialoguesData>(File.ReadAllText(path))!)
                    .SelectMany(data => data.Dialogues)
            ];

        private static void Wandered(List<string> off, string npcId, DialogueNodeEntry node)
        {
            for (int line = 0; line < node.Lines.Count; line++)
            {
                string expected = DialogueKeys.Expected(npcId, node.Id, line);

                if (!string.Equals(node.Lines[line].Key, expected, StringComparison.Ordinal))
                    off.Add($"{npcId}/{node.Id} line {line}: '{node.Lines[line].Key}' where the place words '{expected}'");
            }

            foreach (DialogueOptionEntry option in node.Options)
            {
                string expected = DialogueKeys.Expected(npcId, node.Id, option.Id);

                if (DialogueKeys.IsShared(option.Key)) continue;
                if (string.Equals(option.Key, expected, StringComparison.Ordinal)) continue;

                off.Add($"{npcId}/{node.Id}/{option.Id}: '{option.Key}' where the place words '{expected}'");
            }
        }

        /// <summary>A conversation whose line and whose option are read under keys their places do not
        /// word, beside an option every conversation shares.</summary>
        private const string OffPatternDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Forged_Hello" } ],
                      "options": [
                        { "id": "Ask", "key": "Dlg_Forged_Opt_Ask" },
                        { "id": "Leave", "key": "Dlg_Opt_Leave" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>An option named after the number of a line takes the line's own key.</summary>
        private const string TwiceWordedDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" } ],
                      "options": [ { "id": "1", "key": "Dlg_Forged_Greeting_1" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A conversation whose second line has just been added and carries no key.</summary>
        private const string FreshLineDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" },
                        { "speaker": "Npc" }
                      ],
                      "options": [ { "id": "Leave", "key": "Dlg_Opt_Leave" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node whose two lines have just been swapped, each carrying the other's key.</summary>
        private const string SwappedLinesDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_2" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node whose three lines have just been moved round: each carries the key of the line
        /// before it, and the first carries the last one's.</summary>
        private const string CycledLinesDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_2" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_3" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node with a line just added after the first, the two behind it carrying the keys
        /// they were read under before it arrived.</summary>
        private const string FreshLineBeforeAShiftedTailDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" },
                        { "speaker": "Npc" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_2" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_3" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>Two nodes of one conversation: one whose lines have been swapped, one whose second
        /// line has just been taken out.</summary>
        private const string TwoNodesDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_2" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" }
                      ]
                    },
                    {
                      "id": "Parting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Parting_1" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Parting_3" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node the second of whose five lines has just been taken out.</summary>
        private const string ShortenedLongNodeDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_3" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_4" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_5" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node with a line added in front of the two it had: each of the three wants the key
        /// of the one before it.</summary>
        private const string ShiftedLinesDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_9" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_2" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node whose line and whose option both word the first key of the node: the line by
        /// standing first, the option by being named after the number of a line.</summary>
        private const string TwoPlacesWantingOneKeyDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Forged_Greeting_9" } ],
                      "options": [ { "id": "1", "key": "Dlg_Forged_Greeting_8" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node the second of whose three lines has just been taken out.</summary>
        private const string ShortenedNodeDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_3" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node with a line just added between the two it had.</summary>
        private const string LengthenedNodeDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" },
                        { "speaker": "Npc" },
                        { "speaker": "Npc", "key": "Dlg_Forged_Greeting_2" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A line read under the sentence the option beside it offers.</summary>
        private const string SharedKeyOnALineDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Opt_Leave" } ],
                      "options": [ { "id": "Leave", "key": "Dlg_Opt_Leave" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node whose id has just been retyped, its wording still written under the old one.</summary>
        private const string RenamedNodeDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Hello",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" } ],
                      "options": [ { "id": "Ask", "key": "Dlg_Forged_Greeting_Ask" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>The npc catalog of the forged run: the one record an author renames.</summary>
        private const string ForgedNpcsJson =
            """
            {
              "npcs": [ { "id": "Npc_Villager" } ]
            }
            """;

        /// <summary>The conversations of the forged run: the one the renamed npc speaks — a line, an
        /// option of its own and the option every conversation shares — and one belonging to somebody
        /// nobody renamed.</summary>
        private const string ForgedDialoguesJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Villager",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Villager_Greeting_1" } ],
                      "options": [
                        { "id": "Ask", "key": "Dlg_Villager_Greeting_Ask" },
                        { "id": "Leave", "key": "Dlg_Opt_Leave" }
                      ]
                    }
                  ]
                },
                {
                  "npcId": "Npc_Hunter",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Hunter_Greeting_1" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A node offering nothing but the option every conversation shares.</summary>
        private const string SharedOptionDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [],
                      "options": [ { "id": "Leave", "key": "Dlg_Opt_Leave" } ]
                    }
                  ]
                }
              ]
            }
            """;
    }
}
