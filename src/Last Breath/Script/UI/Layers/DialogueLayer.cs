namespace Playground.Script.UI.Layers
{
    using Godot;
    using System;
    using Playground.Script.NPC;
    using Playground.Localization;
    using Playground.Script.UI.View;
    using System.Collections.Generic;
    using Playground.Script.QuestSystem;

    public partial class DialogueLayer : CanvasLayer
    {
        private DialogueWindow? _dialogWindow;
        private DialogueNode? _currentNode;
        private DialogueNode? _previousNode;
        private int _currentRelation;
        private bool _cutScene = false;
        private ISpeaking? _speaking;

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
        }

        public void StartCutScene(string firstNode)
        {
            _speaking = GameManager.Instance.Player;
            _cutScene = true;
            HandleDialogueNode(firstNode);
        }

        public void StartDialogue(ISpeaking npc, string node = "Greeting")
        {
            _speaking = npc;
            HandleDialogueNode(node);
        }

        private async void HandleDialogueNode(string nodeName)
        {
            if (_speaking!.Dialogs.TryGetValue(nodeName, out var node))
            {
                _currentNode = node;
                foreach (var text in _currentNode?.Texts!)
                {
                    UpdateUI(text.Text);
                    await ToSignal(_dialogWindow!, "CanContinue");
                }

                if (_currentNode?.Options != null)
                    ShowOptions();
                if (_currentNode!.ReturnToPrevious)
                    ShowPreviousOptions();
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

        private void OnOptionSelected(DialogueOption option)
        {
            _currentRelation = Mathf.Clamp(_currentRelation + option.RelationEffect, -100, 100);
            // I need to save the last node with options so I can return to it later.
            if (_currentNode?.Options != null)
            {
                // i need to save all previous node so i can then decide what text i should show
                _previousNode = _currentNode;
            }

            if (!string.IsNullOrWhiteSpace(option.TargetNode))
            {
                HandleDialogueNode(option.TargetNode);
            }
            else
            {
                EndDialogue();
            }
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

        private void HandleQuests(List<string> quests)
        {
            var questManager = DiContainer.GetService<QuestManager>();
            if (questManager == null) return;
            foreach (var quest in quests)
            {
                if (questManager.AddNewQuest(quest))
                    _dialogWindow?.NewQuestAdded();
            }
        }
    }
}
