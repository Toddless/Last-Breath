namespace Playground.Script.UI.Layers
{
    using System.Collections.Generic;
    using Playground.Script.UI.View;
    using Playground.Localization;
    using Playground.Script.NPC;
    using System.Linq;
    using System;
    using Godot;

    public partial class DialogueLayer : CanvasLayer
    {
        private DialogueWindow? _dialogWindow;
        private BaseNPC? _currentNpc;
        private DialogueNode? _currentNode;
        private DialogueNode? _previousNode;
        private int _currentRelation;

        public Action? DialogueEnded;
        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogueWindow>(nameof(DialogueWindow));
        }

        public void StartDialogue(BaseNPC npc, string startNode = "Greeting")
        {
            _currentNpc = npc;
            SetCurrentNode(startNode);
        }

        private async void SetCurrentNode(string startNode)
        {
            if (_currentNpc!.Dialogs.TryGetValue(startNode, out DialogueNode? node))
            {
                _currentNode = node;
                UpdateUI(GetText(_currentNode?.Texts));
                await ToSignal(_dialogWindow, "CanContinue");
                if (_currentNode?.Options != null)
                    ShowOptions();
            }
            else
            {
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            DialogueEnded?.Invoke();
            _dialogWindow?.Clear();
            _currentNpc = null;
            _currentNode = null;
            _previousNode = null;
        }

        private void UpdateUI(string text)
        {
            _dialogWindow!.Clear();
            _dialogWindow.UpdateText(text);
        }

        private async void OnOptionSelected(DialogueOption option)
        {
            _currentRelation = Mathf.Clamp(_currentRelation + option.RelationEffect, -100, 100);

            if (!string.IsNullOrWhiteSpace(option.Text))
            {
                UpdateUI(option.Text);
                await ToSignal(_dialogWindow, "CanContinue");
            }

            if (!string.IsNullOrWhiteSpace(option.TargetNode))
            {
                _previousNode = _currentNode;
                SetCurrentNode(option.TargetNode);
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

        private string GetText(List<DialogueText>? texts)
        {
            if (texts == null || texts.Count == 0) return "Text not found";

            var eligibleText = texts.Where(x => x.MinRelation <= _currentRelation)
                .OrderByDescending(x => x.MinRelation)
                .ToList();

            return eligibleText.FirstOrDefault()?.NpcText ?? "Text not found";
        }
    }
}
