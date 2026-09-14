namespace Core.Narrative.Dialogues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity.Components;
    using Enums;
    using Events;
    using Facts;
    using Influence;

    public class DialogueService : IDialogueService
    {
        private readonly IDialogueProvider _dialogues;
        private readonly IWorldFactsService _facts;
        private readonly IInfluenceMastery _influence;
        private readonly IRandomNumberGenerator _rnd;
        private readonly HashSet<string> _usedThisConversation = [];
        private DialogueDefinition? _dialogue;
        private DialogueNode? _node;
        private NarrativeContext _context = NarrativeContext.Empty;

        public DialogueService(IDialogueProvider dialogues, IWorldFactsService facts, IInfluenceMastery influence,
            IRandomNumberGenerator rnd, IGameEventBus events)
        {
            _dialogues = dialogues;
            _facts = facts;
            _influence = influence;
            _rnd = rnd;
            // The undead don't wait for you to finish the sentence.
            events.Subscribe<BattleInitializedEvent>(_ => End());
        }

        public bool IsActive => _node != null;

        public DialogueNodeView? Current { get; private set; }

        public event Action? Changed;

        public event Action? Ended;

        public bool CanStart(string npcId, string? npcInstanceId, Fractions faction) =>
            ResolveEntry(npcId, new NarrativeContext(npcInstanceId, faction)) != null;

        private (DialogueDefinition Dialogue, DialogueNode Node)? ResolveEntry(string npcId, NarrativeContext context)
        {
            var dialogue = _dialogues.Get(npcId);
            if (dialogue == null || dialogue.EntryRules.Any(rule => rule.Conditions.Any(condition => !condition.IsPreviewSafe))) return null;
            var rule = dialogue.EntryRules.FirstOrDefault(entry => entry.Conditions.All(condition => condition.IsMet(context)));
            return rule != null && dialogue.Nodes.TryGetValue(rule.NodeId, out var node) ? (dialogue, node) : null;
        }

        public bool Start(string npcId, string? npcInstanceId, Fractions faction)
        {
            if (IsActive) End();

            var context = new NarrativeContext(npcInstanceId, faction);
            if (ResolveEntry(npcId, context) is not { } entry) return false;

            _context = context;
            _dialogue = entry.Dialogue;
            _usedThisConversation.Clear();
            RewardFirstTalk(npcId);
            EnterNode(entry.Node);
            return true;
        }

        public void Choose(string optionId)
        {
            if (_dialogue == null || _node == null) return;
            var option = _node.Options.FirstOrDefault(entry => entry.Id == optionId);
            if (option == null || !IsVisible(option) || !IsEnabled(option)) return;

            MarkUsed(option);

            if (option.SpeechCheck is { } check && !RollSpeechCheck(option, check))
            {
                Execute(check.FailActions);
                Route(check.FailNextNodeId);
                return;
            }

            Execute(option.Actions);
            Route(option.NextNodeId);
        }

        public void End()
        {
            if (_dialogue == null) return;

            _dialogue = null;
            _node = null;
            Current = null;
            _context = NarrativeContext.Empty;
            _usedThisConversation.Clear();
            Ended?.Invoke();
        }

        private void Route(string? nextNodeId)
        {
            if (_dialogue == null) return; // an action ended the conversation (battle, etc.)

            if (nextNodeId == null)
            {
                End();
                return;
            }

            EnterNode(_dialogue.Nodes[nextNodeId]);
        }

        private void EnterNode(DialogueNode node)
        {
            _node = node;
            Execute(node.OnEnter);
            if (_dialogue == null) return; // OnEnter may have ended it

            Refresh();
        }

        private void Refresh()
        {
            if (_dialogue == null || _node == null) return;

            var options = _node.Options
                .Where(IsVisible)
                .Select(option => new DialogueOptionView(option.Id, option.TextKey, IsEnabled(option)))
                .ToList();

            if (options.Count == 0)
            {
                // Every option hidden (once-flags/conditions) — end instead of soft-locking the player.
                Tracker.TrackError($"Dialogue '{_dialogue.NpcId}' node '{_node.Id}' has no visible options left");
                End();
                return;
            }

            Current = new DialogueNodeView(_dialogue.NpcId, _node.Lines, options);
            Changed?.Invoke();
        }

        private bool IsVisible(DialogueOption option)
        {
            if (option.OncePerConversation && _usedThisConversation.Contains(OptionKey(option))) return false;
            if (option.OncePerGame && _facts.IsSet(FactKeys.DialogueOptionUsed(_dialogue!.NpcId, _node!.Id, option.Id))) return false;
            return option.VisibleConditions.All(condition => condition.IsMet(_context));
        }

        private bool IsEnabled(DialogueOption option) => option.EnabledConditions.All(condition => condition.IsMet(_context));

        private void MarkUsed(DialogueOption option)
        {
            if (option.OncePerConversation) _usedThisConversation.Add(OptionKey(option));
            if (option.OncePerGame) _facts.SetFact(FactKeys.DialogueOptionUsed(_dialogue!.NpcId, _node!.Id, option.Id));
        }

        /// <summary>The chance is INVISIBLE by design; a passed check pays Influence exp once per option.</summary>
        private bool RollSpeechCheck(DialogueOption option, DialogueSpeechCheck check)
        {
            bool passed = _rnd.RandFloat() < _influence.GetSpeechCheckChance(check.Difficulty);
            if (!passed) return false;

            string rewardKey = FactKeys.SpeechCheckRewarded(_dialogue!.NpcId, _node!.Id, option.Id);
            if (!_facts.IsSet(rewardKey))
            {
                _facts.SetFact(rewardKey);
                _influence.AddExperience(_influence.SpeechCheckExp);
            }

            return true;
        }

        private void RewardFirstTalk(string npcId)
        {
            if (_facts.IsSet(FactKeys.NpcTalked(npcId))) return;
            _facts.SetFact(FactKeys.NpcTalked(npcId));
            _influence.AddExperience(_influence.FirstTalkExp);
        }

        private string OptionKey(DialogueOption option) => $"{_node!.Id}:{option.Id}";

        private void Execute(IReadOnlyList<Actions.INarrativeAction> actions)
        {
            foreach (var action in actions)
                action.Execute(_context);
        }
    }
}
