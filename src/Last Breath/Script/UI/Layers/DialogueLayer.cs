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
        private Player? _player;
        private int _currentRelation;
        private bool _cutScene = false;
        private ISpeaking? _speaking;
        private readonly System.Collections.Generic.Dictionary<string, ISpeaking> _speakers = [];

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
            _player = GameManager.Instance.Player;
        }

        public void StartCutScene(string firstNode)
        {
            _cutScene = true;
            //HandleDialogueNode(firstNode);
        }

        public void SetSpeakers(List<BaseSpeakingNPC> npcs)
        {
            foreach (var npc in npcs)
            {
                _speakers.Add(npc.NpcId, npc);
            }
        }

        public void StartDialogueNode(string firstNode = "FirstMeeting")
        {
            DialogueNode? node = _speaking!.NpcTalking
                ? _speaking!.Dialogs.GetValueOrDefault(firstNode)
                : _player!.Dialogs.GetValueOrDefault(firstNode);
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
            if (_currentNode?.Quests.Count > 0)
                ShowQuests(_currentNode.Quests);
            if (_currentNode?.Options.Count > 0)
                ShowOptions();
            if (_currentNode!.ReturnToPrevious)
                ShowPreviousOptions();
        }

        private void ShowQuests(Array<Quest> quests)
        {
            var manager = DiContainer.GetService<QuestManager>();
            if (manager == null) return;
            foreach (var quest in quests)
            {
                if (!quest.ConfirmationRequired)
                {
                    if (quest.CanAcceptQuest(manager))
                    {
                        quest.AcceptQuest();
                        _dialogWindow?.NewQuestAdded();
                    }
                }
                else
                {
                    ShowQuestsAsOptions(quest);
                }
            }
        }

        private void ShowQuestsAsOptions(Quest quest)
        {
           var option = DialogueUIOption.Initialize().Instantiate<DialogueUIOption>();
            option.BindQuest(quest);
            option.Quest += OnQuestPressed;
            _dialogWindow?.AddOption(option);
        }

        private void OnQuestPressed(Quest quest)
        {

        }

        private void OnOptionSelected(DialogueOption option)
        {
            _currentRelation = Mathf.Clamp(_currentRelation + option.RelationEffect, -100, 100);
            // I need to save the last node with options so I can return to it.
            if (_currentNode?.Options != null)
            {
                _previousNode = _currentNode;
            }

            if (!string.IsNullOrWhiteSpace(option.TargetNode))
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
            else
            {
                EndDialogue();
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
            DialogueEnded?.Invoke();
            _dialogWindow?.Clear();
            _previousNode = null;
            _currentNode = null;
            _cutScene = false;
            _speaking = null;
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
