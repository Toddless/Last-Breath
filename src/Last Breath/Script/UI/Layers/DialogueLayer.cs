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
    using System.Collections.Generic;

    public partial class DialogueLayer : CanvasLayer
    {
        private DialogueWindow? _dialogWindow;
        private DialogueNode? _currentNode, _previousNode;
        private QuestManager? _questManager;
        private Player? _player;
        private int _currentRelation;
        private bool _cutScene = false;
        private BaseSpeakingNPC? _speaking;

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
            _player = GameManager.Instance.Player;
            _questManager = DiContainer.GetService<QuestManager>();
            _dialogWindow.QuitPressed += () => DialogueEnded?.Invoke();
            _dialogWindow.QuestsPressed += QuestsPressed;
        }

        public void StartCutScene(string firstNode)
        {
            _cutScene = true;
            StartDialogueNode("EvaluateSituation");
        }

        public void StartDialogue(BaseSpeakingNPC npcs)
        {
            _speaking = npcs;
            StartDialogueNode();
        }

        public void StartDialogueNode(string firstNode = "FirstMeeting")
        {
            DialogueNode? node = null;
            if (!_cutScene)
            {
                node = _speaking!.NpcTalking
                   ? _speaking!.Dialogs.GetValueOrDefault(firstNode)
                   : _player!.Dialogs.GetValueOrDefault(firstNode);
            }
            else
            {
                node = _player?.Dialogs.GetValueOrDefault(firstNode);
            }
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
            if (_currentNode.IsDialogMatterForQuest)
                _player?.Progress.OnDialogueCompleted(_currentNode.DialogueId);
            if (_currentNode.Quests.Count > 0)
                CheckAndAddQuests(_currentNode.Quests);
            if (_currentNode?.Options.Count > 0)
                ShowOptions();
            if (_currentNode!.ReturnToPrevious)
                ShowPreviousOptions();
        }

        private void CheckAndAddQuests(Array<Quest> quests)
        {
            if (_questManager == null) return;
            foreach (var quest in quests)
            {
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
            foreach (var quest in _speaking.Quests)
            {
                if (!quest.QuestCanBeAccepted(_questManager)) continue;
                questMenu.AddQuests(quest);
            }
            _dialogWindow?.HideDialogueButtons();
        }

        private void OnOptionSelected(DialogueOption option)
        {
            _currentRelation = Mathf.Clamp(_currentRelation + option.RelationEffect, -100, 100);
            // I need to save the last node with options so I can return to it.
            if (_currentNode?.Options.Count > 0)
            {
                _previousNode = _currentNode;
            }
            if (_cutScene)
            {
                StartDialogueNode(option.TargetNode);
            }
            else
            {
                if (option.UsePlayerSource)
                {
                    _speaking!.NpcTalking = false;
                    StartDialogueNode(option.TargetNode);
                }
                else
                {
                    _speaking!.NpcTalking = true;
                    StartDialogueNode(option.TargetNode);
                }
            }
        }

        private void ShowPreviousOptions()
        {
            foreach (var item in _previousNode?.Options!)
            {
                var option = DialogueUIOption.Initialize().Instantiate<DialogueUIOption>();
                option.Bind(item);
                option.Option += OnOptionSelected;
                _dialogWindow?.AddOption(option);
            }
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

        private void ShowOptions()
        {
            foreach (var item in _currentNode?.Options!)
            {
                var option = DialogueUIOption.Initialize().Instantiate<DialogueUIOption>();
                option.Bind(item);
                option.Option += OnOptionSelected;
                _dialogWindow?.AddOption(option);
            }
        }
    }
}
