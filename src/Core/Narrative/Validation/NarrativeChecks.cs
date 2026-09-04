namespace Core.Narrative.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.DialogueData;
    using Data.GameData;
    using Data.QuestData;
    using Dialogues;
    using Enums;
    using Facts;
    using Quests;

    /// <summary>What one run of the rules read: what it found, and the fact keys it met on the way. Both
    /// come out of the one walk — a second pass over the same documents for the keys alone would be a
    /// second answer to what a dialogue currently writes.</summary>
    public sealed record NarrativeReading(IReadOnlyList<NarrativeFinding> Findings, FactKeyRegistry Facts);

    /// <summary>
    /// Everything one narrative catalog can only be held to against the others: the ids it names, the
    /// dialogues its quests are taken from, the routes inside a conversation and the wording every line
    /// is read under. The rules live here and nowhere else — the game's tests run them over what it ships
    /// and an authoring tool runs them over the documents open in it, and two readings of one rule are
    /// two answers.
    /// <para>A report and not a gate: nothing here refuses data. What the checks find is handed back for
    /// whoever asked to pin, print or draw.</para>
    /// </summary>
    public static class NarrativeChecks
    {
        /// <summary>Where the run says what it could not read of the wording itself.</summary>
        private const string TextsWhere = "localization";

        private const string NoLocalesText =
            "no locale was read, so no line of the narrative was held against the wording of the game";

        private const string DroppedDialogueFormat = "the loader kept no dialogue under '{0}': the npc says nothing at all";

        private const string DroppedQuestFormat = "the loader kept no quest under '{0}': it is out of the game";

        private const string DuplicateDialogueFormat =
            "two dialogues are written for '{0}': the loader keeps the last of them and the rest are out of the game";

        private const string DuplicateQuestFormat =
            "two quests answer to '{0}': the loader keeps the last of them and the rest are out of the game";

        private const string UnnamedDialogueText = "the dialogue names no npc, so nobody can be made to speak it";

        private const string UnnamedQuestText = "the quest is written without an id and can be neither offered nor saved";

        private const string NoEntryRulesText = "the dialogue writes no entry rule, so no state of the world opens it";

        private const string NoNodesText = "the dialogue writes no node";

        private const string NoStagesText = "the quest writes no stage";

        private const string NoOptionsText = "the node writes no option, so the conversation stops on it with no way out";

        private const string NoObjectivesText = "the stage writes no objective, so nothing can finish it";

        private const string UnnamedNodeText = "the node is written without an id and no route can name it";

        private const string UnnamedStageText = "the stage is written without an id and no route can name it";

        private const string UnnamedOptionText = "the option is written without an id, which is what its once-per-game record is kept under";

        private const string UnnamedOutcomeText = "the ending is written without an id, which is what the save keeps it by";

        private const string NoTextKeyFormat = "the {0} writes no '{1}': there is nothing to read it under";

        private const string NoGiverText = "the quest names no npc to be taken from";

        private const string EmptyRouteFormat = "'{0}' is written empty, where the name of a {1} is meant";

        private const string EmptyIdFormat = "'{0}' is written empty, where a record id is meant";

        private const string DanglingNodeFormat = "'{0}' leads to '{1}', which is no node of this dialogue";

        private const string DanglingStageFormat = "'{0}' leads to '{1}', which is no stage of this quest";

        private const string UnreachableNodeFormat = "no route of the dialogue reaches '{0}'";

        private const string DuplicateNodeFormat =
            "two nodes answer to '{0}': the loader refuses the second one and drops the dialogue whole";

        private const string DuplicateStageFormat = "two stages answer to '{0}'";

        private const string DuplicateOutcomeFormat = "two endings answer to '{0}', so the reward paid would be ambiguous";

        private const string NoDialogueFormat = "'{0}' has no dialogue, so the quest cannot be {1} in one";

        private const string ObjectiveShapeFormat =
            "the objective writes {0} of a condition and a counter, where the quest log reads exactly one";

        private const string OutcomeAndRoutesFormat =
            "the stage writes both '{0}' and '{1}', where an ending leads nowhere: the loader refuses the quest whole";

        private const string FailingOutcomeText =
            "the ending buries the quest, which is written unable to fail: the loader refuses the quest whole";

        private const string LoopingStageFormat =
            "the routes lead back to '{0}': the quest would never end, so the loader refuses it whole";

        private const string TakenWord = "taken";

        private const string HandedInWord = "handed in";

        private const string BothWord = "both";

        private const string NeitherWord = "neither";

        private const string NodeWord = "node";

        private const string StageWord = "stage";

        private const string LineWord = "line";

        private const string OptionWord = "option";

        private const string MissingTextFormat = "'{0}' is in no locale this run read";

        private const string KeyOffPatternFormat = "the {0} is read under '{1}', where its place words '{2}'";

        private const string KeyTwiceFormat = "'{0}' is worded by two places of this dialogue at once";

        private const string NeverWrittenFormat = "'{0}' is asked about and nothing ever writes it, so the clause gated on it can never be met";

        private const string NeverReadFormat = "'{0}' is written and nothing ever reads it back";

        private const string UntranslatedFormat = "'{0}' is missing from the '{1}' locale";

        /// <summary>Where the run says what it found about the facts themselves, which belong to no one
        /// record: the key is the second step, the way a record's id is under its catalog. Public because
        /// such a place is shaped like a record's and opens nothing, and a reader telling the two apart
        /// by spelling the word a second time would drift the day it is spelled differently.</summary>
        public const string FactsWhere = "facts";

        /// <summary>Everywhere an npc is named by the narrative: the whole of the one catalog holding them.</summary>
        private static readonly NarrativeReferenceTarget[] s_npcTargets = [NarrativeReferenceTarget.Whole(DataCatalog.Npc)];

        /// <summary>The members the game parses the record fields marked as enums into. The parsers are
        /// strict, so a word outside these sets takes the whole record out of the game.</summary>
        private static readonly string[] s_speakers = Enum.GetNames<DialogueSpeaker>();

        private static readonly string[] s_factions = Enum.GetNames<Fractions>();

        private static readonly string[] s_declinePolicies = Enum.GetNames<DeclinePolicy>();

        /// <summary>Runs every rule over one reading of the narrative, in the order the records are
        /// written. The findings come back as they were made — sorting them is the reader's business, and
        /// a run whose order changes with the phase of the moon cannot be pinned.</summary>
        public static IReadOnlyList<NarrativeFinding> Run(NarrativeCheckInput input) => Read(input).Findings;

        /// <summary>One run of the rules, with the fact keys the walk met handed back beside what it
        /// found: whoever wants the map of who writes and who reads asks for the reading rather than
        /// walking the documents a second time.</summary>
        public static NarrativeReading Read(NarrativeCheckInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Pass(input).Read();
        }

        /// <summary>One run of the rules. A class rather than a pile of arguments: every rule writes into
        /// the same list and asks the same two sources, and the walk of one record is a dozen steps deep.</summary>
        private sealed class Pass
        {
            private readonly List<NarrativeFinding> _findings = [];
            private readonly HashSet<NarrativeReferenceTarget> _undescribed = [];

            /// <summary>Every fact key the walk met, with the place that wrote or read it. Gathered as the
            /// records are walked and answered for at the end: whether a key is ever written is a question
            /// about the whole narrative, and no single record can answer it.</summary>
            private readonly List<FactKeyUse> _facts = [];

            /// <summary>The ids each catalog has already written a record under. Two records sharing one
            /// leaves the loader keeping one of them, and everything written under the other silently
            /// out of the game.</summary>
            private readonly HashSet<string> _dialogueIds = new(StringComparer.Ordinal);

            private readonly HashSet<string> _questIds = new(StringComparer.Ordinal);

            private readonly NarrativeCheckInput _input;

            private readonly NarrativeVocabularyWalk _vocabulary;

            /// <summary>The npcs a dialogue is written for, whether or not the loader kept it: a quest
            /// whose giver has a dialogue that was dropped is answered by that drop, not by a second
            /// finding saying the npc never had one.</summary>
            private readonly HashSet<string> _speaking;

            /// <summary>A pass is built whole: the vocabulary reader writes into the pass's own list of
            /// findings, so it cannot be made before there is one.</summary>
            internal Pass(NarrativeCheckInput input)
            {
                _input = input;
                _vocabulary = new NarrativeVocabularyWalk(input.Ids, _findings, _undescribed, _facts);
                _speaking = [.. input.Dialogues.Select(dialogue => dialogue.NpcId).Where(npcId => npcId is { Length: > 0 })];
            }

            /// <summary>Every record walked, and then the one question no record can answer on its own:
            /// which of the keys met is written by nobody and which is read by nobody.</summary>
            internal NarrativeReading Read()
            {
                if (_input.Texts.Locales.Count == 0) Add(NarrativeFindingKind.Incomplete, TextsWhere, NoLocalesText);

                for (int index = 0; index < _input.Dialogues.Count; index++) Dialogue(_input.Dialogues[index], index);
                for (int index = 0; index < _input.Quests.Count; index++) Quest(_input.Quests[index], index);

                FactKeyRegistry facts = FactKeyRegistry.Over(_facts);
                Facts(facts);

                return new NarrativeReading(_findings, facts);
            }

            /// <summary>The two ends a fact key can be loose at. Said of the registry rather than of the
            /// places: one key read in five quests and written nowhere is one thing wrong, not five.</summary>
            private void Facts(FactKeyRegistry facts)
            {
                foreach (FactKeyEntry key in facts.Keys)
                {
                    if (key.NeverWritten)
                        Add(NarrativeFindingKind.FactNeverWritten, Under(FactsWhere, key.Key), string.Format(NeverWrittenFormat, key.Key));
                    else if (key.NeverRead)
                        Add(NarrativeFindingKind.FactNeverRead, Under(FactsWhere, key.Key), string.Format(NeverReadFormat, key.Key));
                }
            }

            private static string At(string where, int index) => NarrativeVocabularyWalk.At(where, index);

            private static string Under(string where, string key) => NarrativeVocabularyWalk.Under(where, key);

            /// <summary>The address of a record, a node or a stage: its own id where it has one, and its
            /// place in the file where it has not.</summary>
            private static string Named(string where, string key, string? id, int index) =>
                id is { Length: > 0 } named ? Under(Under(where, key), named) : At(Under(where, key), index);

            private void Add(NarrativeFindingKind kind, string where, string message) =>
                _findings.Add(new NarrativeFinding(kind, where, message));

            private void Dialogue(DialogueEntry entry, int index)
            {
                string where = entry.NpcId is { Length: > 0 } npcId ? Under(DataCatalog.Dialogues, npcId) : At(DataCatalog.Dialogues, index);

                DialogueNpc(entry, where);

                if (entry.EntryRules.Count == 0) Add(NarrativeFindingKind.Incomplete, where, NoEntryRulesText);
                if (entry.Nodes.Count == 0) Add(NarrativeFindingKind.Incomplete, where, NoNodesText);

                HashSet<string> nodes = Nodes(entry, where);

                Openings(entry, where, nodes);

                // The keys this dialogue's own places word, so that two places wording one key are named
                // rather than left to overwrite each other's text in the .po files.
                HashSet<string> worded = new(StringComparer.Ordinal);

                for (int node = 0; node < entry.Nodes.Count; node++)
                    Node(entry.Nodes[node], where, node, nodes, entry.NpcId, worded);

                Unreachable(entry, where, nodes);
            }

            /// <summary>The npc a dialogue belongs to, which is also the name the catalog keeps it under.
            /// Everything else here is asked of a written id: a dialogue with none is already out of the
            /// game, and holding an empty word against the npc catalog would answer for the wrong fact.</summary>
            private void DialogueNpc(DialogueEntry entry, string where)
            {
                if (entry.NpcId is not { Length: > 0 } npcId)
                {
                    Add(NarrativeFindingKind.Incomplete, where, UnnamedDialogueText);
                    return;
                }

                Npc(npcId, Under(where, DialogueEntry.NpcIdKey), DialogueEntry.NpcIdKey);

                if (!_dialogueIds.Add(npcId))
                    Add(NarrativeFindingKind.DuplicateId, where, string.Format(DuplicateDialogueFormat, npcId));

                if (!_input.LoadedDialogues.Contains(npcId))
                    Add(NarrativeFindingKind.Dropped, where, string.Format(DroppedDialogueFormat, npcId));
            }

            /// <summary>The nodes one dialogue writes, and what it costs to write two of them alike: the
            /// loader reads them into a map keyed by id, which refuses the second one outright, so the
            /// whole dialogue is dropped and the npc says nothing at all.</summary>
            private HashSet<string> Nodes(DialogueEntry entry, string where)
            {
                HashSet<string> nodes = new(StringComparer.Ordinal);

                foreach (DialogueNodeEntry node in entry.Nodes)
                    if (node.Id is { Length: > 0 } id && !nodes.Add(id))
                        Add(NarrativeFindingKind.DuplicateId, Under(Under(where, DialogueEntry.NodesKey), id),
                            string.Format(DuplicateNodeFormat, id));

                return nodes;
            }

            private void Openings(DialogueEntry entry, string where, HashSet<string> nodes)
            {
                for (int index = 0; index < entry.EntryRules.Count; index++)
                {
                    DialogueEntryRuleEntry rule = entry.EntryRules[index];
                    string at = At(Under(where, DialogueEntry.EntryRulesKey), index);

                    _vocabulary.Conditions(rule.Conditions, Under(at, DialogueEntryRuleEntry.ConditionsKey));
                    Route(rule.Node, nodes, at, DialogueEntryRuleEntry.NodeKey, required: true);
                }
            }

            /// <summary>One route out of a dialogue. Absent ends the conversation and names nothing;
            /// written empty is a key left half-typed; written and unanswered drops the whole dialogue.</summary>
            private void Route(string? target, HashSet<string> nodes, string where, string key, bool required)
            {
                if (target is null)
                {
                    if (required)
                        Add(NarrativeFindingKind.Incomplete, where, string.Format(EmptyRouteFormat, key, NodeWord));

                    return;
                }

                if (target.Length == 0)
                {
                    Add(NarrativeFindingKind.Incomplete, where, string.Format(EmptyRouteFormat, key, NodeWord));
                    return;
                }

                if (!nodes.Contains(target))
                    Add(NarrativeFindingKind.DanglingNode, where, string.Format(DanglingNodeFormat, key, target));
            }

            private void Node(DialogueNodeEntry node, string where, int index, HashSet<string> nodes, string npcId,
                HashSet<string> worded)
            {
                string at = Named(where, DialogueEntry.NodesKey, node.Id, index);

                if (node.Id is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, at, UnnamedNodeText);
                if (node.Options.Count == 0) Add(NarrativeFindingKind.Incomplete, at, NoOptionsText);

                _vocabulary.Actions(node.OnEnter, Under(at, DialogueNodeEntry.OnEnterKey));

                for (int line = 0; line < node.Lines.Count; line++)
                    Line(node.Lines[line], at, line, DialogueKeys.Expected(npcId, node.Id, line), worded);

                for (int option = 0; option < node.Options.Count; option++)
                    Option(node.Options[option], at, option, nodes, npcId, node.Id, worded);
            }

            private void Line(DialogueLineEntry line, string where, int index, string expected, HashSet<string> worded)
            {
                string at = At(Under(where, DialogueNodeEntry.LinesKey), index);

                _vocabulary.Member(
                    line.Speaker, s_speakers, Under(at, DialogueLineEntry.SpeakerKey), DialogueLineEntry.SpeakerKey);

                Worded(expected, at, worded);

                if (line.Key is not { Length: > 0 })
                {
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(NoTextKeyFormat, LineWord, DialogueLineEntry.TextKey));
                    return;
                }

                // A line is read under the key its own place words and under no other: what an option may
                // share with the rest of the game, a line said by one npc in one node never can.
                if (expected.Length > 0 && !string.Equals(line.Key, expected, StringComparison.Ordinal))
                    Add(NarrativeFindingKind.KeyOffPattern, at, string.Format(KeyOffPatternFormat, LineWord, line.Key, expected));

                Text(line.Key, at);
            }

            private void Option(DialogueOptionEntry option, string where, int index, HashSet<string> nodes, string npcId,
                string nodeId, HashSet<string> worded)
            {
                string at = Named(where, DialogueNodeEntry.OptionsKey, option.Id, index);
                string expected = DialogueKeys.Expected(npcId, nodeId, option.Id);

                if (option.Id is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, at, UnnamedOptionText);

                if (option.Key is not { Length: > 0 })
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(NoTextKeyFormat, OptionWord, DialogueLineEntry.TextKey));
                else Text(option.Key, at);

                OptionKey(option, at, expected, worded);

                _vocabulary.Conditions(option.VisibleConditions, Under(at, DialogueOptionEntry.VisibleConditionsKey));
                _vocabulary.Conditions(option.EnabledConditions, Under(at, DialogueOptionEntry.EnabledConditionsKey));
                _vocabulary.Actions(option.Actions, Under(at, DialogueOptionEntry.ActionsKey));

                Route(option.Next, nodes, at, DialogueOptionEntry.NextKey, required: false);

                if (option.SpeechCheck is not { } check) return;

                string speech = Under(at, DialogueOptionEntry.SpeechCheckKey);

                _vocabulary.Actions(check.FailActions, Under(speech, DialogueSpeechCheckEntry.FailActionsKey));
                Route(check.FailNext, nodes, speech, DialogueSpeechCheckEntry.FailNextKey, required: false);
            }

            /// <summary>The key an option is read under: the one its place words, or one of the options
            /// every conversation shares. A shared key words no place of its own, so it takes none — two
            /// nodes both offering "Leave." are not two places fighting over one word.</summary>
            private void OptionKey(DialogueOptionEntry option, string where, string expected, HashSet<string> worded)
            {
                if (option.Key is { Length: > 0 } key && DialogueKeys.IsShared(key, offered: true)) return;

                Worded(expected, where, worded);

                if (option.Key is not { Length: > 0 } written || expected.Length == 0) return;
                if (string.Equals(written, expected, StringComparison.Ordinal)) return;

                Add(NarrativeFindingKind.KeyOffPattern, where, string.Format(KeyOffPatternFormat, OptionWord, written, expected));
            }

            /// <summary>Claims the key one place of a dialogue words. Two places wording one — a node
            /// written twice, an option named after the number of a line — leave the second one's text
            /// standing over the first's in every locale, with nothing in either file to show it.</summary>
            private void Worded(string expected, string where, HashSet<string> worded)
            {
                if (expected.Length == 0 || worded.Add(expected)) return;

                Add(NarrativeFindingKind.DuplicateId, where, string.Format(KeyTwiceFormat, expected));
            }

            /// <summary>The nodes no opening rule and no option can arrive at. Written out of the same
            /// routes the loader walks, so a node kept out of reach is named rather than shipped unread.</summary>
            private void Unreachable(DialogueEntry entry, string where, HashSet<string> nodes)
            {
                Dictionary<string, DialogueNodeEntry> byId = new(StringComparer.Ordinal);
                foreach (DialogueNodeEntry node in entry.Nodes)
                    if (node.Id is { Length: > 0 } id) byId.TryAdd(id, node);

                HashSet<string> reached = new(StringComparer.Ordinal);
                Queue<string> pending = new(entry.EntryRules
                    .Select(rule => rule.Node)
                    .Where(node => node is { Length: > 0 } && nodes.Contains(node)));

                while (pending.Count > 0)
                {
                    string id = pending.Dequeue();
                    if (!reached.Add(id)) continue;

                    foreach (string next in Successors(byId[id]).Where(nodes.Contains))
                        pending.Enqueue(next);
                }

                foreach (DialogueNodeEntry node in entry.Nodes.Where(node => node.Id is { Length: > 0 } && !reached.Contains(node.Id)))
                    Add(NarrativeFindingKind.UnreachableNode, Under(Under(where, DialogueEntry.NodesKey), node.Id),
                        string.Format(UnreachableNodeFormat, node.Id));
            }

            /// <summary>Every node one node's options can lead to — the choice itself and the fallback of a
            /// speech check gone wrong.</summary>
            private static IEnumerable<string> Successors(DialogueNodeEntry node)
            {
                foreach (DialogueOptionEntry option in node.Options)
                {
                    if (option.Next is { Length: > 0 } next) yield return next;
                    if (option.SpeechCheck?.FailNext is { Length: > 0 } failNext) yield return failNext;
                }
            }

            private void Quest(QuestEntry entry, int index)
            {
                string where = entry.Id is { Length: > 0 } id ? Under(DataCatalog.Quests, id) : At(DataCatalog.Quests, index);

                if (entry.Id is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, where, UnnamedQuestText);
                else
                {
                    if (!_questIds.Add(entry.Id))
                        Add(NarrativeFindingKind.DuplicateId, where, string.Format(DuplicateQuestFormat, entry.Id));

                    if (!_input.LoadedQuests.Contains(entry.Id))
                        Add(NarrativeFindingKind.Dropped, where, string.Format(DroppedQuestFormat, entry.Id));
                }

                Giver(entry, where);
                Members(entry, where);

                _vocabulary.Conditions(entry.AcceptConditions, Under(where, QuestEntry.AcceptConditionsKey));
                _vocabulary.Actions(entry.OnAccept, Under(where, QuestEntry.OnAcceptKey));
                _vocabulary.Actions(entry.OnDecline, Under(where, QuestEntry.OnDeclineKey));
                _vocabulary.Actions(entry.OnFail, Under(where, QuestEntry.OnFailKey));
                Rewards(entry.Rewards, Under(where, QuestEntry.RewardsKey));

                if (entry.Stages.Count == 0) Add(NarrativeFindingKind.Incomplete, where, NoStagesText);

                HashSet<string> stages = Stages(entry, where);

                for (int stage = 0; stage < entry.Stages.Count; stage++)
                    Stage(entry.Stages[stage], where, stage, stages, entry.CanFail);

                Cycles(entry, where);
            }

            /// <summary>The two words of a quest the game reads into an enum. A faction is optional and a
            /// decline policy has a fallback, but a word written and misspelt is refused by the parser
            /// rather than passed over, which takes the quest out of the game.</summary>
            private void Members(QuestEntry entry, string where)
            {
                if (entry.Faction is not null)
                    _vocabulary.Member(entry.Faction, s_factions, Under(where, QuestEntry.FactionKey), QuestEntry.FactionKey);

                _vocabulary.Member(
                    entry.DeclinePolicy, s_declinePolicies, Under(where, QuestEntry.DeclinePolicyKey), QuestEntry.DeclinePolicyKey);
            }

            /// <summary>Who the quest is taken from and handed in to. Both have to be somebody the player
            /// can talk to: a quest offered by an npc with no dialogue is one nobody can ever start.</summary>
            private void Giver(QuestEntry entry, string where)
            {
                if (entry.GiverNpcId is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, where, NoGiverText);
                else Speaker(entry.GiverNpcId, Under(where, QuestEntry.GiverKey), QuestEntry.GiverKey, TakenWord);

                for (int index = 0; index < entry.TurnInNpcIds.Count; index++)
                {
                    string at = At(Under(where, QuestEntry.TurnInKey), index);
                    string? npc = entry.TurnInNpcIds[index];

                    if (npc is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, at, string.Format(EmptyIdFormat, QuestEntry.TurnInKey));
                    else Speaker(npc, at, QuestEntry.TurnInKey, HandedInWord);
                }
            }

            private void Speaker(string npcId, string where, string key, string word)
            {
                Npc(npcId, where, key);

                if (!_speaking.Contains(npcId))
                    Add(NarrativeFindingKind.MissingDialogue, where, string.Format(NoDialogueFormat, npcId, word));
            }

            private void Npc(string npcId, string where, string key) => _vocabulary.Reference(npcId, s_npcTargets, where, key);

            private HashSet<string> Stages(QuestEntry entry, string where)
            {
                HashSet<string> stages = new(StringComparer.Ordinal);
                HashSet<string> outcomes = new(StringComparer.Ordinal);

                for (int index = 0; index < entry.Stages.Count; index++)
                {
                    QuestStageEntry stage = entry.Stages[index];
                    string at = Named(where, QuestEntry.StagesKey, stage.Id, index);

                    if (stage.Id is { Length: > 0 } id && !stages.Add(id))
                        Add(NarrativeFindingKind.DuplicateId, at, string.Format(DuplicateStageFormat, id));

                    if (stage.Outcome is { Id: { Length: > 0 } outcome } && !outcomes.Add(outcome))
                        Add(NarrativeFindingKind.DuplicateId, Under(at, QuestStageEntry.OutcomeKey),
                            string.Format(DuplicateOutcomeFormat, outcome));
                }

                return stages;
            }

            private void Stage(QuestStageEntry stage, string where, int index, HashSet<string> stages, bool canFail)
            {
                string at = Named(where, QuestEntry.StagesKey, stage.Id, index);

                if (stage.Id is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, at, UnnamedStageText);
                if (stage.Objectives.Count == 0) Add(NarrativeFindingKind.Incomplete, at, NoObjectivesText);

                _vocabulary.Actions(stage.OnEnter, Under(at, QuestStageEntry.OnEnterKey));
                _vocabulary.Actions(stage.OnComplete, Under(at, QuestStageEntry.OnCompleteKey));

                for (int objective = 0; objective < stage.Objectives.Count; objective++)
                    Objective(stage.Objectives[objective], at, objective);

                for (int transition = 0; transition < stage.Transitions.Count; transition++)
                    Transition(stage.Transitions[transition], at, transition, stages);

                if (stage.Outcome is { } outcome) Outcome(outcome, stage, at, canFail);
            }

            private void Objective(QuestObjectiveEntry objective, string where, int index)
            {
                string at = Named(where, QuestStageEntry.ObjectivesKey, objective.Id, index);

                bool condition = objective.Condition is not null;
                string? counter = objective.Counter is { Key: { Length: > 0 } written } ? written : null;

                if (condition == (counter is not null))
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(ObjectiveShapeFormat, condition ? BothWord : NeitherWord));

                _vocabulary.Condition(objective.Condition, Under(at, QuestObjectiveEntry.ConditionKey));

                // The one fact key of the narrative written outside the vocabulary: a counter is a field of
                // the record, so the walk over the conditions and the actions never sees it.
                if (counter is { } key)
                    _facts.Add(new FactKeyUse(
                        key,
                        FactKeyUseKind.Read,
                        Under(Under(at, QuestObjectiveEntry.CounterKey), QuestCounterEntry.KeyKey)));
            }

            private void Transition(QuestTransitionEntry transition, string where, int index, HashSet<string> stages)
            {
                string at = At(Under(where, QuestStageEntry.TransitionsKey), index);

                _vocabulary.Conditions(transition.Conditions, Under(at, QuestTransitionEntry.ConditionsKey));

                if (transition.To is not { Length: > 0 })
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(EmptyRouteFormat, QuestTransitionEntry.ToKey, StageWord));
                else if (!stages.Contains(transition.To))
                    Add(NarrativeFindingKind.DanglingStage, at, string.Format(DanglingStageFormat, QuestTransitionEntry.ToKey, transition.To));
            }

            /// <summary>An ending of the quest, and the two ways of writing one the loader refuses the
            /// whole quest over: an ending that also declares routes leads both nowhere and somewhere, and
            /// one that buries a quest written unable to fail asks for a state that cannot be reached.</summary>
            private void Outcome(QuestOutcomeEntry outcome, QuestStageEntry stage, string where, bool canFail)
            {
                string at = Under(where, QuestStageEntry.OutcomeKey);

                if (outcome.Id is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, at, UnnamedOutcomeText);

                if (stage.Transitions.Count > 0)
                    Add(NarrativeFindingKind.Incomplete, where,
                        string.Format(OutcomeAndRoutesFormat, QuestStageEntry.OutcomeKey, QuestStageEntry.TransitionsKey));

                if (outcome.Fails && !canFail)
                    Add(NarrativeFindingKind.Incomplete, Under(at, QuestOutcomeEntry.FailsKey), FailingOutcomeText);

                if (outcome.Rewards is { } rewards) Rewards(rewards, Under(at, QuestEntry.RewardsKey));
            }

            /// <summary>The stages whose routes lead back into themselves. Read off the same successors the
            /// loader walks — a stage's own routes, or the next stage of the list where it declares none —
            /// because a ring in them is what the loader refuses the whole quest over.</summary>
            private void Cycles(QuestEntry entry, string where)
            {
                Dictionary<string, int> byId = new(StringComparer.Ordinal);
                for (int index = 0; index < entry.Stages.Count; index++)
                    if (entry.Stages[index].Id is { Length: > 0 } id) byId.TryAdd(id, index);

                HashSet<string> considered = new(StringComparer.Ordinal);

                foreach (QuestStageEntry stage in entry.Stages)
                {
                    if (stage.Id is not { Length: > 0 } id || !considered.Add(id)) continue;
                    if (!Loops(entry, byId, id)) continue;

                    Add(NarrativeFindingKind.LoopingStage, Under(Under(where, QuestEntry.StagesKey), id),
                        string.Format(LoopingStageFormat, id));
                }
            }

            /// <summary>Whether one stage is reachable from itself. Routes naming no stage of the quest are
            /// walked past: they are dangling and already said so, and following them would be this rule
            /// answering for another one's finding.</summary>
            private static bool Loops(QuestEntry entry, Dictionary<string, int> byId, string from)
            {
                HashSet<string> seen = new(StringComparer.Ordinal);
                Queue<string> pending = new(Successors(entry, byId[from]));

                while (pending.Count > 0)
                {
                    string id = pending.Dequeue();
                    if (string.Equals(id, from, StringComparison.Ordinal)) return true;
                    if (!seen.Add(id) || !byId.TryGetValue(id, out int index)) continue;

                    foreach (string next in Successors(entry, index)) pending.Enqueue(next);
                }

                return false;
            }

            /// <summary>Stages a finished stage can lead to: its own routes, or the next of the list when it
            /// declares none. An ending leads nowhere.</summary>
            private static IEnumerable<string> Successors(QuestEntry entry, int index)
            {
                QuestStageEntry stage = entry.Stages[index];

                if (stage.Outcome is not null) return [];

                if (stage.Transitions.Count > 0)
                    return [.. stage.Transitions.Select(transition => transition.To).Where(to => to is { Length: > 0 })];

                return index + 1 < entry.Stages.Count && entry.Stages[index + 1].Id is { Length: > 0 } next ? [next] : [];
            }

            private void Rewards(QuestRewardsEntry rewards, string where)
            {
                _vocabulary.Actions(rewards.Actions, Under(where, QuestRewardsEntry.ActionsKey));

                for (int index = 0; index < rewards.Items.Count; index++)
                {
                    QuestRewardItemEntry item = rewards.Items[index];
                    string at = At(Under(where, QuestRewardsEntry.ItemsKey), index);

                    if (item.ItemId is not { Length: > 0 }) Add(NarrativeFindingKind.Incomplete, at, string.Format(EmptyIdFormat, ItemReference.Key));
                    else _vocabulary.Reference(item.ItemId, ItemReference.Targets, at, ItemReference.Key);
                }
            }

            /// <summary>One localization key of a line or an option: it has to be in the reference locale,
            /// and every other locale that has not got it is a translation owed.</summary>
            private void Text(string key, string where)
            {
                if (_input.Texts.Locales.Count == 0) return;

                if (!_input.Texts.Has(_input.Texts.ReferenceLocale, key))
                {
                    Add(NarrativeFindingKind.MissingText, where, string.Format(MissingTextFormat, key));
                    return;
                }

                foreach (string locale in _input.Texts.Locales.Where(locale => !string.Equals(locale, _input.Texts.ReferenceLocale, StringComparison.Ordinal)))
                    if (!_input.Texts.Has(locale, key))
                        Add(NarrativeFindingKind.UntranslatedText, where, string.Format(UntranslatedFormat, key, locale));
            }
        }
    }
}
