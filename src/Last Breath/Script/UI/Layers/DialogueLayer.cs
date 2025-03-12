namespace Playground.Script.UI.Layers
{
    using Godot;
    using System;
    using Playground.Script.NPC;
    using Playground.Localization;
    using Playground.Script.UI.View;
    using Playground.Script.QuestSystem;
    using Godot.Collections;
    using Playground.Resource.Quests;
    using Playground.Resource;

    public partial class DialogueLayer : CanvasLayer
    {
        private DialogueWindow? _dialogWindow;
        private DialogueNode? _currentNode, _previousNode;
        private QuestManager? _questManager;
        private Player? _player;
        private int _currentRelation;
        private BaseSpeakingNPC? _speaking;
        private IDialogueStrategy? _dialogueStrategy;

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
            _player = GameManager.Instance.Player;
            _questManager = DiContainer.GetService<QuestManager>();
            // dialogue layer initialize one time for the entire life cycle
            _dialogWindow.QuitPressed += () => DialogueEnded?.Invoke();
            _dialogWindow.QuestsPressed += QuestsPressed;
        }

        public void InitializeCutScene(string firstNode)
        {
            _dialogueStrategy = new MonologueStrategy(_player ??= GameManager.Instance.Player);
            StartDialogueNode(firstNode);
        }

        public void InitializeDialogue(BaseSpeakingNPC npc)
        {
            _speaking = npc;
            _dialogueStrategy = new OneToOneDialogueStrategy(npc, _player ??= GameManager.Instance.Player);
            StartDialogueNode();
        }

        public void StartDialogueNode(string firstNode = "GuardianFirstMeeting")
        {
            var node = _dialogueStrategy?.GetNextDialogueNode(firstNode);

            if (node == null)
            {
                // log
                EndDialogue();
                return;
            }
            HandleDialogueNode(node);
        }

        private async void HandleDialogueNode(DialogueNode node)
        {
            _currentNode = node;
            foreach (var text in _currentNode?.Texts!)
            {
                UpdateUI(text.Text);
                await ToSignal(_dialogWindow!, "CanContinue");
            }
            UpdatePlayerDialoguesProgress(_currentNode);
            HandleQuests(_currentNode);
            HandleOptions(_currentNode);
            HandleReturnToPreviousNode(_currentNode);
        }

        private void HandleReturnToPreviousNode(DialogueNode node)
        {
            if (!node.ReturnToPrevious || _previousNode == null) return;
            ShowOptions(_previousNode.Options);
        }

        private void HandleOptions(DialogueNode node)
        {
            if (node.Options.Count == 0) return;
            ShowOptions(node.Options);
        }

        private void HandleQuests(DialogueNode node)
        {
            if (node.Quests.Count == 0) return;
            AcceptQuests(node.Quests);
        }

        private void UpdatePlayerDialoguesProgress(DialogueNode node)
        {
            if (!node.IsDialogMatterForQuest) return;
            _player?.Progress.OnDialogueCompleted(node.DialogueId);
        }

        private void AcceptQuests(Array<string> quests)
        {
            if (_questManager == null) return;
            foreach (var item in quests)
            {
                if (!QuestsTable.TryGetElement(item, out Quest? quest) || quest == null) continue;
                if (quest.QuestCanBeAccepted(_questManager) && !quest.ConfirmationRequired)
                {
                    quest.AcceptQuest();
                    _dialogWindow?.NewQuestAdded();
                }
            }
        }

        private void QuestsPressed()
        {
            var questMenu = NPCsQuests.Initialize().Instantiate<NPCsQuests>();
            if (_speaking == null || _questManager == null) return;
            _dialogWindow?.AddQuestOption(questMenu);


            // i need to show all quests that this npc have
            foreach (var item in _speaking.Quests)
            {
                if (!QuestsTable.TryGetElement(item, out Quest? quest) || quest == null) continue;
                if (!quest.QuestCanBeAccepted(_questManager)) continue;
                questMenu.AddQuests(quest);
            }
            _dialogWindow?.HideDialogueButtons();
        }

        private void OnOptionSelected(DialogueOption option)
        {
            // I need to save the last node with options so I can return to it.
            if (_currentNode?.Options.Count > 0) _previousNode = _currentNode;
            StartDialogueNode(option.TargetNode);
        }

        private void EndDialogue()
        {
            _dialogWindow?.Clear();
            _dialogWindow?.ShowDialogueButtons();
        }

        private void UpdateUI(string text)
        {
            _dialogWindow!.Clear();
            _dialogWindow.UpdateText(text);
        }

        private void ShowOptions(Array<DialogueOption> options)
        {
            foreach (var item in options)
            {
                if (!item.CheckConditions(_player ??= GameManager.Instance.Player)) continue;
                var option = DialogueUIOption.Initialize().Instantiate<DialogueUIOption>();
                option.Bind(item);
                option.Option += OnOptionSelected;
                _dialogWindow?.AddOption(option);
            }
        }
    }
}
