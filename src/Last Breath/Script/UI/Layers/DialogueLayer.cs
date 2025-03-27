namespace Playground.Script.UI.Layers
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Localization;
    using Playground.Resource;
    using Playground.Resource.Quests;
    using Playground.Script.NPC;
    using Playground.Script.QuestSystem;
    using Playground.Script.UI.View;

    public partial class DialogueLayer : CanvasLayer
    {
        private DialogueWindow? _dialogWindow;
        private DialogueNode? _currentNode, _previousNode;
        private QuestManager? _questManager;
        private Player? _player;
        private int _currentRelation;
        private BaseSpeakingNPC? _speaking;
        private IDialogueStrategy? _dialogueStrategy;

        [Signal]
        public delegate void CloseDialogueWindowEventHandler();

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
            _player = GameManager.Instance.Player;
            _questManager = QuestManager.Instance;
            // dialogue layer initialize one time for the entire life cycle
            _dialogWindow.QuitPressed += () => DialogueEnded?.Invoke();
            _dialogWindow.QuestsPressed += QuestsPressed;
            _dialogWindow.CloseDialogueWindow += ClosingDialogueWindow;
        }

        public void InitializeMonologue(string firstNode)
        {
            _dialogueStrategy = new MonologueStrategy(_player ??= GameManager.Instance.Player, _dialogWindow!);
            DisablePlayerMovement();
            StartDialogueNode("Conclusions");
        }

        public void InitializeDialogue(BaseSpeakingNPC npc)
        {
            _speaking = npc;
            _dialogueStrategy = new OneToOneDialogueStrategy(npc, _player ??= GameManager.Instance.Player, _dialogWindow!);
            DisablePlayerMovement();
            StartDialogueNode(_speaking.InitialDialogueId);
        }

        public void StartDialogueNode(string firstNode)
        {
            var node = _dialogueStrategy?.GetNextDialogueNode(firstNode);

            if (node == null)
            {
                // log
                _dialogueStrategy?.EndDialogue();
                return;
            }
            HandleDialogueNode(node);
        }

        private void ClosingDialogueWindow()
        {
            EnablePlayerMovement();
            EmitSignal(SignalName.CloseDialogueWindow);
        }

        private async void HandleDialogueNode(DialogueNode node)
        {
            _currentNode = node;
            foreach (var text in _currentNode?.Texts!)
            {
                UpdateUI(text.Text);
                // TODO: Rework this part, to bad
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
            _player?.OnDialogueCompleted(node.DialogueId);
        }

        private void AcceptQuests(Array<string> quests)
        {
            _questManager ??= QuestManager.Instance;
            foreach (var item in quests)
            {
                if (!QuestsTable.Instance.TryGetElement(item, out Quest? quest) || quest == null) continue;
                if (_questManager!.QuestCanBeAccepted(quest) && quest.IsTriggerQuest)
                {
                    _questManager.OnQuestAccepted(quest);
                    _dialogWindow?.NewQuestAdded();
                }
            }
        }

        private void QuestsPressed()
        {
            var questMenu = NPCsQuests.Initialize().Instantiate<NPCsQuests>();
            if (_speaking == null || _questManager == null) return;
            _dialogWindow?.AddQuestOption(questMenu);
            _questManager ??= QuestManager.Instance;
            // i need to show all quests that this npc have
            foreach (var item in _speaking.Quests)
            {
                if (!QuestsTable.Instance.TryGetElement(item, out Quest? quest) || quest == null) continue;
                if (!QuestManager.Instance!.QuestCanBeAccepted(quest)) continue;
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

        private void DisablePlayerMovement() => _player!.CanMove = false;

        private void EnablePlayerMovement() => _player!.CanMove = true;
    }
}
