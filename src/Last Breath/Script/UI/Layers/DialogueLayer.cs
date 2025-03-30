namespace Playground.Script.UI.Layers
{
    using System;
    using System.Linq;
    using Godot;
    using Godot.Collections;
    using Playground.Localization;
    using Playground.Resource;
    using Playground.Resource.Quests;
    using Playground.Script.NPC;
    using Playground.Script.QuestSystem;
    using Playground.Script.UI.View;
    using Stateless;

    public partial class DialogueLayer : CanvasLayer
    {
        private enum State { Dialogue, Main, Quests, Quest, CompletedQuests, CompletedQuest }
        private enum Trigger { Dialogue, Main, Quests, Quest, CompletedQuests, CompletedQuest, Back }

        private StateMachine<State, Trigger>? _machine;

        private DialogueWindow? _dialogWindow;
        private DialogueNode? _currentNode, _previousNode;
        private QuestManager? _questManager;
        private Player? _player;
        private int _currentRelation;
        private BaseSpeakingNPC? _speaking;
        private IDialogueStrategy? _dialogueStrategy;
        private Quest? _questToHandle;

        [Signal]
        public delegate void CloseDialogueWindowEventHandler();

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
            _machine = new(State.Dialogue);
            _player = GameManager.Instance.Player;
            _questManager = QuestManager.Instance;
            ConfigureStateMachine();
            SetEvents();
        }

        #region Dialogue
        public void InitializeMonologue(string firstNode)
        {
            _dialogueStrategy = new MonologueStrategy(_player ??= GameManager.Instance.Player, _dialogWindow!);
            DisablePlayerMovement();
            StartDialogueNode(/*firstNode*/ "Conclusion");
        }

        public void InitializeDialogue(BaseSpeakingNPC npc)
        {
            _speaking = npc;
            _dialogueStrategy = new OneToOneDialogueStrategy(npc, _player ??= GameManager.Instance.Player, _dialogWindow!);
            DisablePlayerMovement();
            StartDialogueNode(_speaking.InitialDialogueId);
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
            // TODO: Mark a dialogue as completed
        }

        private void StartDialogueNode(string firstNode)
        {
            var node = _dialogueStrategy?.GetNextDialogueNode(firstNode);

            if (node == null)
            {
                // log
                _dialogueStrategy?.EndDialogue();
                _machine?.Fire(Trigger.Main);
                return;
            }
            HandleDialogueNode(node);
        }

        private void ClosingDialogueWindow()
        {
            EnablePlayerMovement();
            EmitSignal(SignalName.CloseDialogueWindow);
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
        #endregion

        private void ConfigureStateMachine()
        {
            // TODO: Show "about your quest" only if player have at least one finished quest
            _machine?.Configure(State.Dialogue)
                .Permit(Trigger.Main, State.Main);

            _machine?.Configure(State.Main)
                .OnEntry(() =>
                {
                    _dialogWindow?.ShowMainButtons();
                    _dialogWindow?.HideBackButton();
                })
                .OnExit(() =>
                {
                    _dialogWindow?.HideMainButtons();
                })
                .Permit(Trigger.Quests, State.Quests)
                .Permit(Trigger.Dialogue, State.Dialogue)
                .Permit(Trigger.CompletedQuests, State.CompletedQuests);

            _machine?.Configure(State.Quests)
                .OnEntry(() =>
                {
                    AddQuests();
                    _dialogWindow?.ShowBackButton();
                })
                .OnExit(RemoveQuests)
                .Permit(Trigger.Quest, State.Quest)
                .Permit(Trigger.Back, State.Main);

            _machine?.Configure(State.Quest)
                .OnEntry(() =>
                {
                    _dialogWindow?.ShowAcceptButton();
                })
                .OnExit(() =>
                {
                    _dialogWindow?.HideAcceptButton();
                })
                .Permit(Trigger.Back, State.Quests);

            _machine?.Configure(State.CompletedQuests)
                .OnEntry(() =>
                {
                    AddCompletedQuests();
                    _dialogWindow?.ShowBackButton();
                })
                .OnExit(RemoveQuests)
                .Permit(Trigger.CompletedQuest, State.CompletedQuest)
                .Permit(Trigger.Back, State.Main);

            _machine?.Configure(State.CompletedQuest)
                .OnEntry(() =>
                {
                    _dialogWindow?.ShowCompleteQuestButton();
                })
                .OnExit(() =>
                {
                    _dialogWindow?.HideCompleteQuestButton();
                })
                .Permit(Trigger.Back, State.CompletedQuests);
        }

        private void SetEvents()
        {
            _dialogWindow!.QuitPressed += OnQuitPressed;
            _dialogWindow.QuestsPressed += QuestsPressed;
            _dialogWindow.CloseDialogueWindow += ClosingDialogueWindow;
            _dialogWindow.AcceptPressed += OnAcceptPressed;
            _dialogWindow.BackPressed += OnBackPressed;
            _dialogWindow.CompletedQuestsPressed += OnCompletedQuestsPressed;
            _dialogWindow.QuestCompleted += OnQuestCompleted;
        }

        private void OnQuestCompleted()
        {
            if (_questToHandle == null || _questManager == null) return;
            _questManager.OnQuestCompleted(_questToHandle);
            RemoveFromNpcList(_questToHandle);
            _machine?.Fire(Trigger.Back);
        }

        private void RemoveFromNpcList(Quest questToHandle)
        {
            if (_speaking == null) return;
            if (!_speaking.CompletedQuests.Remove(questToHandle.Id))
            {
                // TODO: Log
            }
            _questToHandle = null;
        }

        private void OnCompletedQuestsPressed() => _machine?.Fire(Trigger.CompletedQuests);

        private void AddCompletedQuests()
        {
            if (_speaking == null || _questManager == null) return;
            foreach (var item in _speaking.CompletedQuests)
            {
                if (!QuestsTable.Instance.TryGetElement(item, out Quest? quest) || quest == null) continue;
                var questOption = QuestOption.Initialize().Instantiate<QuestOption>();
                questOption.Bind(quest);
                questOption.QuestSelected += OnCompletedQuestSelected;
                questOption.QuestDetails += OnQuestDescription;
                _dialogWindow?.AddQuest(questOption);
            }
        }

        private void OnCompletedQuestSelected() => _machine?.Fire(Trigger.CompletedQuest);
        private void OnBackPressed() => _machine?.Fire(Trigger.Back);

        private void OnQuitPressed()
        {
            DialogueEnded?.Invoke();
            _player!.CanMove = true;
            _machine?.Fire(Trigger.Dialogue);
        }

        #region Quests handling
        private void OnAcceptPressed()
        {
            // TODO: Update UI after accepting a quest
            if (_questToHandle == null) return;
            QuestManager.Instance?.OnQuestAccepted(_questToHandle);
            _dialogWindow?.GetQuests().Cast<QuestOption>()?.FirstOrDefault(x => x.IsMatch(_questToHandle.Id) == true)?.QueueFree();
            // npc now know what quest a player is doing
            _speaking?.OnPlayerAcceptQuest(_questToHandle.Id);
            _dialogWindow?.ClearText();
            _questToHandle = null;
            _machine?.Fire(Trigger.Back);
        }

        private void QuestsPressed() => _machine?.Fire(Trigger.Quests);

        private void RemoveQuests()
        {
            foreach (var item in _dialogWindow!.GetQuests())
            {
                item.QueueFree();
            }
        }

        private void AddQuests()
        {
            if (_speaking == null || _questManager == null) return;

            foreach (var item in _speaking.Quests)
            {
                if (!QuestsTable.Instance.TryGetElement(item, out Quest? quest) || quest == null) continue;
                if (!QuestManager.Instance!.QuestCanBeAccepted(quest)) continue;
                var questOption = QuestOption.Initialize().Instantiate<QuestOption>();
                questOption.Bind(quest);
                questOption.QuestDetails += OnQuestDescription;
                questOption.QuestSelected += OnQuestSelected;
                _dialogWindow?.AddQuest(questOption);
            }
        }

        private void OnQuestSelected() => _machine?.Fire(Trigger.Quest);

        private void OnQuestDescription(string description, Quest? quest)
        {
            _questToHandle = quest;
            _dialogWindow?.UpdateText(description);
        }
        #endregion
    }
}
