namespace Core.Narrative.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.DialogueData;
    using Data.GameData;
    using Data.QuestData;

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
        /// <summary>Json names of the parts a dialogue is written in. Said here because a finding is
        /// addressed the way the file is written, and the DTO states them as attribute arguments, which
        /// nothing can be shared with.</summary>
        private const string EntryRulesKey = "entryRules";

        private const string NodesKey = "nodes";

        private const string LinesKey = "lines";

        private const string OptionsKey = "options";

        private const string NpcIdKey = "npcId";

        private const string NodeKey = "node";

        private const string NextKey = "next";

        private const string SpeechCheckKey = "speechCheck";

        private const string FailNextKey = "failNext";

        private const string FailActionsKey = "failActions";

        private const string TextKey = "key";

        private const string ConditionsKey = "conditions";

        private const string ConditionKey = "condition";

        private const string VisibleConditionsKey = "visibleConditions";

        private const string EnabledConditionsKey = "enabledConditions";

        private const string ActionsKey = "actions";

        private const string OnEnterKey = "onEnter";

        /// <summary>Json names of the parts a quest is written in.</summary>
        private const string GiverKey = "giverNpcId";

        private const string TurnInKey = "turnInNpcIds";

        private const string StagesKey = "stages";

        private const string ObjectivesKey = "objectives";

        private const string TransitionsKey = "transitions";

        private const string ToKey = "to";

        private const string OutcomeKey = "outcome";

        private const string RewardsKey = "rewards";

        private const string ItemsKey = "items";

        private const string AcceptConditionsKey = "acceptConditions";

        private const string OnCompleteKey = "onComplete";

        private const string OnAcceptKey = "onAccept";

        private const string OnDeclineKey = "onDecline";

        private const string OnFailKey = "onFail";

        /// <summary>Where the run says what it could not read of the wording itself.</summary>
        private const string TextsWhere = "localization";

        private const string NoLocalesText =
            "no locale was read, so no line of the narrative was held against the wording of the game";

        private const string DroppedDialogueFormat = "the loader kept no dialogue under '{0}': the npc says nothing at all";

        private const string DroppedQuestFormat = "the loader kept no quest under '{0}': it is out of the game";

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

        private const string DuplicateNodeFormat = "two nodes answer to '{0}'";

        private const string DuplicateStageFormat = "two stages answer to '{0}'";

        private const string DuplicateOutcomeFormat = "two endings answer to '{0}', so the reward paid would be ambiguous";

        private const string NoDialogueFormat = "'{0}' has no dialogue, so the quest cannot be {1} in one";

        private const string ObjectiveShapeFormat =
            "the objective writes {0} of a condition and a counter, where the quest log reads exactly one";

        private const string TakenWord = "taken";

        private const string HandedInWord = "handed in";

        private const string BothWord = "both";

        private const string NeitherWord = "neither";

        private const string NodeWord = "node";

        private const string StageWord = "stage";

        private const string LineWord = "line";

        private const string OptionWord = "option";

        private const string MissingTextFormat = "'{0}' is in no locale this run read";

        private const string UntranslatedFormat = "'{0}' is missing from the '{1}' locale";

        /// <summary>Everywhere an npc is named by the narrative: the whole of the one catalog holding them.</summary>
        private static readonly NarrativeReferenceTarget[] s_npcTargets = [NarrativeReferenceTarget.Whole(DataCatalog.Npc)];

        /// <summary>Runs every rule over one reading of the narrative, in the order the records are
        /// written. The findings come back as they were made — sorting them is the reader's business, and
        /// a run whose order changes with the phase of the moon cannot be pinned.</summary>
        public static IReadOnlyList<NarrativeFinding> Run(NarrativeCheckInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Pass(input).Walk();
        }

        /// <summary>One run of the rules. A class rather than a pile of arguments: every rule writes into
        /// the same list and asks the same two sources, and the walk of one record is a dozen steps deep.</summary>
        private sealed class Pass(NarrativeCheckInput input)
        {
            private readonly List<NarrativeFinding> _findings = [];
            private readonly HashSet<NarrativeReferenceTarget> _undescribed = [];

            private NarrativeVocabularyWalk _vocabulary = null!;

            /// <summary>The npcs a dialogue is written for, whether or not the loader kept it: a quest
            /// whose giver has a dialogue that was dropped is answered by that drop, not by a second
            /// finding saying the npc never had one.</summary>
            private HashSet<string> _speaking = null!;

            internal IReadOnlyList<NarrativeFinding> Walk()
            {
                _vocabulary = new NarrativeVocabularyWalk(input.Ids, _findings, _undescribed);
                _speaking = [.. input.Dialogues.Select(dialogue => dialogue.NpcId)];

                if (input.Texts.Locales.Count == 0) Add(NarrativeFindingKind.Incomplete, TextsWhere, NoLocalesText);

                for (int index = 0; index < input.Dialogues.Count; index++) Dialogue(input.Dialogues[index], index);
                for (int index = 0; index < input.Quests.Count; index++) Quest(input.Quests[index], index);

                return _findings;
            }

            private static string At(string where, int index) => NarrativeVocabularyWalk.At(where, index);

            private static string Under(string where, string key) => NarrativeVocabularyWalk.Under(where, key);

            /// <summary>The address of a record, a node or a stage: its own id where it has one, and its
            /// place in the file where it has not.</summary>
            private static string Named(string where, string key, string id, int index) =>
                id.Length > 0 ? Under(Under(where, key), id) : At(Under(where, key), index);

            private void Add(NarrativeFindingKind kind, string where, string message) =>
                _findings.Add(new NarrativeFinding(kind, where, message));

            private void Dialogue(DialogueEntry entry, int index)
            {
                string where = entry.NpcId.Length > 0 ? Under(DataCatalog.Dialogues, entry.NpcId) : At(DataCatalog.Dialogues, index);

                if (entry.NpcId.Length == 0) Add(NarrativeFindingKind.Incomplete, where, UnnamedDialogueText);
                else Npc(entry.NpcId, Under(where, NpcIdKey), NpcIdKey);

                if (entry.NpcId.Length > 0 && !input.LoadedDialogues.Contains(entry.NpcId))
                    Add(NarrativeFindingKind.Dropped, where, string.Format(DroppedDialogueFormat, entry.NpcId));

                if (entry.EntryRules.Count == 0) Add(NarrativeFindingKind.Incomplete, where, NoEntryRulesText);
                if (entry.Nodes.Count == 0) Add(NarrativeFindingKind.Incomplete, where, NoNodesText);

                HashSet<string> nodes = Nodes(entry, where);

                Openings(entry, where, nodes);
                for (int node = 0; node < entry.Nodes.Count; node++) Node(entry.Nodes[node], where, node, nodes);

                Unreachable(entry, where, nodes);
            }

            /// <summary>The nodes one dialogue writes, and what it costs to write two of them alike: the
            /// loader keeps whichever comes second and the routes to the first lead nowhere anybody
            /// authored.</summary>
            private HashSet<string> Nodes(DialogueEntry entry, string where)
            {
                HashSet<string> nodes = new(StringComparer.Ordinal);

                foreach (DialogueNodeEntry node in entry.Nodes)
                    if (node.Id.Length > 0 && !nodes.Add(node.Id))
                        Add(NarrativeFindingKind.DuplicateId, Under(Under(where, NodesKey), node.Id),
                            string.Format(DuplicateNodeFormat, node.Id));

                return nodes;
            }

            private void Openings(DialogueEntry entry, string where, HashSet<string> nodes)
            {
                for (int index = 0; index < entry.EntryRules.Count; index++)
                {
                    DialogueEntryRuleEntry rule = entry.EntryRules[index];
                    string at = At(Under(where, EntryRulesKey), index);

                    _vocabulary.Conditions(rule.Conditions, Under(at, ConditionsKey));
                    Route(rule.Node, nodes, at, NodeKey, required: true);
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

            private void Node(DialogueNodeEntry node, string where, int index, HashSet<string> nodes)
            {
                string at = Named(where, NodesKey, node.Id, index);

                if (node.Id.Length == 0) Add(NarrativeFindingKind.Incomplete, at, UnnamedNodeText);
                if (node.Options.Count == 0) Add(NarrativeFindingKind.Incomplete, at, NoOptionsText);

                _vocabulary.Actions(node.OnEnter, Under(at, OnEnterKey));

                for (int line = 0; line < node.Lines.Count; line++) Line(node.Lines[line], at, line);
                for (int option = 0; option < node.Options.Count; option++) Option(node.Options[option], at, option, nodes);
            }

            private void Line(DialogueLineEntry line, string where, int index)
            {
                string at = At(Under(where, LinesKey), index);

                if (line.Key.Length == 0)
                {
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(NoTextKeyFormat, LineWord, TextKey));
                    return;
                }

                Text(line.Key, at);
            }

            private void Option(DialogueOptionEntry option, string where, int index, HashSet<string> nodes)
            {
                string at = Named(where, OptionsKey, option.Id, index);

                if (option.Id.Length == 0) Add(NarrativeFindingKind.Incomplete, at, UnnamedOptionText);

                if (option.Key.Length == 0) Add(NarrativeFindingKind.Incomplete, at, string.Format(NoTextKeyFormat, OptionWord, TextKey));
                else Text(option.Key, at);

                _vocabulary.Conditions(option.VisibleConditions, Under(at, VisibleConditionsKey));
                _vocabulary.Conditions(option.EnabledConditions, Under(at, EnabledConditionsKey));
                _vocabulary.Actions(option.Actions, Under(at, ActionsKey));

                Route(option.Next, nodes, at, NextKey, required: false);

                if (option.SpeechCheck is not { } check) return;

                string speech = Under(at, SpeechCheckKey);

                _vocabulary.Actions(check.FailActions, Under(speech, FailActionsKey));
                Route(check.FailNext, nodes, speech, FailNextKey, required: false);
            }

            /// <summary>The nodes no opening rule and no option can arrive at. Written out of the same
            /// routes the loader walks, so a node kept out of reach is named rather than shipped unread.</summary>
            private void Unreachable(DialogueEntry entry, string where, HashSet<string> nodes)
            {
                Dictionary<string, DialogueNodeEntry> byId = new(StringComparer.Ordinal);
                foreach (DialogueNodeEntry node in entry.Nodes) byId.TryAdd(node.Id, node);

                HashSet<string> reached = new(StringComparer.Ordinal);
                Queue<string> pending = new(entry.EntryRules.Select(rule => rule.Node).Where(nodes.Contains));

                while (pending.Count > 0)
                {
                    string id = pending.Dequeue();
                    if (!reached.Add(id)) continue;

                    foreach (string next in Successors(byId[id]).Where(nodes.Contains))
                        pending.Enqueue(next);
                }

                foreach (DialogueNodeEntry node in entry.Nodes.Where(node => node.Id.Length > 0 && !reached.Contains(node.Id)))
                    Add(NarrativeFindingKind.UnreachableNode, Under(Under(where, NodesKey), node.Id),
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
                string where = entry.Id.Length > 0 ? Under(DataCatalog.Quests, entry.Id) : At(DataCatalog.Quests, index);

                if (entry.Id.Length == 0) Add(NarrativeFindingKind.Incomplete, where, UnnamedQuestText);
                else if (!input.LoadedQuests.Contains(entry.Id))
                    Add(NarrativeFindingKind.Dropped, where, string.Format(DroppedQuestFormat, entry.Id));

                Giver(entry, where);

                _vocabulary.Conditions(entry.AcceptConditions, Under(where, AcceptConditionsKey));
                _vocabulary.Actions(entry.OnAccept, Under(where, OnAcceptKey));
                _vocabulary.Actions(entry.OnDecline, Under(where, OnDeclineKey));
                _vocabulary.Actions(entry.OnFail, Under(where, OnFailKey));
                Rewards(entry.Rewards, Under(where, RewardsKey));

                if (entry.Stages.Count == 0) Add(NarrativeFindingKind.Incomplete, where, NoStagesText);

                HashSet<string> stages = Stages(entry, where);

                for (int stage = 0; stage < entry.Stages.Count; stage++) Stage(entry.Stages[stage], where, stage, stages);
            }

            /// <summary>Who the quest is taken from and handed in to. Both have to be somebody the player
            /// can talk to: a quest offered by an npc with no dialogue is one nobody can ever start.</summary>
            private void Giver(QuestEntry entry, string where)
            {
                if (entry.GiverNpcId.Length == 0) Add(NarrativeFindingKind.Incomplete, where, NoGiverText);
                else Speaker(entry.GiverNpcId, Under(where, GiverKey), GiverKey, TakenWord);

                for (int index = 0; index < entry.TurnInNpcIds.Count; index++)
                {
                    string at = At(Under(where, TurnInKey), index);
                    string npc = entry.TurnInNpcIds[index];

                    if (npc.Length == 0) Add(NarrativeFindingKind.Incomplete, at, string.Format(EmptyIdFormat, TurnInKey));
                    else Speaker(npc, at, TurnInKey, HandedInWord);
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

                foreach (QuestStageEntry stage in entry.Stages)
                {
                    if (stage.Id.Length > 0 && !stages.Add(stage.Id))
                        Add(NarrativeFindingKind.DuplicateId, Under(Under(where, StagesKey), stage.Id),
                            string.Format(DuplicateStageFormat, stage.Id));

                    if (stage.Outcome is { Id.Length: > 0 } outcome && !outcomes.Add(outcome.Id))
                        Add(NarrativeFindingKind.DuplicateId, Under(Under(Under(where, StagesKey), stage.Id), OutcomeKey),
                            string.Format(DuplicateOutcomeFormat, outcome.Id));
                }

                return stages;
            }

            private void Stage(QuestStageEntry stage, string where, int index, HashSet<string> stages)
            {
                string at = Named(where, StagesKey, stage.Id, index);

                if (stage.Id.Length == 0) Add(NarrativeFindingKind.Incomplete, at, UnnamedStageText);
                if (stage.Objectives.Count == 0) Add(NarrativeFindingKind.Incomplete, at, NoObjectivesText);

                _vocabulary.Actions(stage.OnEnter, Under(at, OnEnterKey));
                _vocabulary.Actions(stage.OnComplete, Under(at, OnCompleteKey));

                for (int objective = 0; objective < stage.Objectives.Count; objective++)
                    Objective(stage.Objectives[objective], at, objective);

                for (int transition = 0; transition < stage.Transitions.Count; transition++)
                    Transition(stage.Transitions[transition], at, transition, stages);

                if (stage.Outcome is { } outcome) Outcome(outcome, Under(at, OutcomeKey));
            }

            private void Objective(QuestObjectiveEntry objective, string where, int index)
            {
                string at = Named(where, ObjectivesKey, objective.Id, index);

                bool condition = objective.Condition is not null;
                bool counter = objective.Counter is { Key.Length: > 0 };

                if (condition == counter)
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(ObjectiveShapeFormat, condition ? BothWord : NeitherWord));

                _vocabulary.Condition(objective.Condition, Under(at, ConditionKey));
            }

            private void Transition(QuestTransitionEntry transition, string where, int index, HashSet<string> stages)
            {
                string at = At(Under(where, TransitionsKey), index);

                _vocabulary.Conditions(transition.Conditions, Under(at, ConditionsKey));

                if (transition.To.Length == 0)
                    Add(NarrativeFindingKind.Incomplete, at, string.Format(EmptyRouteFormat, ToKey, StageWord));
                else if (!stages.Contains(transition.To))
                    Add(NarrativeFindingKind.DanglingStage, at, string.Format(DanglingStageFormat, ToKey, transition.To));
            }

            private void Outcome(QuestOutcomeEntry outcome, string where)
            {
                if (outcome.Id.Length == 0) Add(NarrativeFindingKind.Incomplete, where, UnnamedOutcomeText);

                if (outcome.Rewards is { } rewards) Rewards(rewards, Under(where, RewardsKey));
            }

            private void Rewards(QuestRewardsEntry rewards, string where)
            {
                _vocabulary.Actions(rewards.Actions, Under(where, ActionsKey));

                for (int index = 0; index < rewards.Items.Count; index++)
                {
                    QuestRewardItemEntry item = rewards.Items[index];
                    string at = At(Under(where, ItemsKey), index);

                    if (item.ItemId.Length == 0) Add(NarrativeFindingKind.Incomplete, at, string.Format(EmptyIdFormat, ItemReference.Key));
                    else _vocabulary.Reference(item.ItemId, ItemReference.Targets, at, ItemReference.Key);
                }
            }

            /// <summary>One localization key of a line or an option: it has to be in the reference locale,
            /// and every other locale that has not got it is a translation owed.</summary>
            private void Text(string key, string where)
            {
                if (input.Texts.Locales.Count == 0) return;

                if (!input.Texts.Has(input.Texts.ReferenceLocale, key))
                {
                    Add(NarrativeFindingKind.MissingText, where, string.Format(MissingTextFormat, key));
                    return;
                }

                foreach (string locale in input.Texts.Locales.Where(locale => !string.Equals(locale, input.Texts.ReferenceLocale, StringComparison.Ordinal)))
                    if (!input.Texts.Has(locale, key))
                        Add(NarrativeFindingKind.UntranslatedText, where, string.Format(UntranslatedFormat, key, locale));
            }
        }
    }
}
