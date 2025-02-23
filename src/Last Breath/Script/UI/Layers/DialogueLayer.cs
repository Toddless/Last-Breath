namespace Playground.Script.UI.Layers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Localization;
    using Playground.Script.NPC;
    using Playground.Script.UI.View;

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

        private void SetCurrentNode(string startNode)
        {
            if (_currentNpc!.Dialogs.TryGetValue(startNode, out var node))
            {
                _currentNode = node;
                UpdateUI();
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

        private void UpdateUI()
        {
            _dialogWindow?.Clear();
            _dialogWindow?.UpdateText(GetText(_currentNode?.Texts));
            if (_currentNode?.Options != null)
            {
                foreach (var item in _currentNode.Options)
                {
                    var option = DialogueUIOption.Initialize().Instantiate<DialogueUIOption>();
                    option.Bind(item);
                    option.Option += OnOptionSelected;
                    _dialogWindow?.AddOption(option);
                }
            }
        }

        private string GetText(List<DialogueText>? texts)
        {
            if (texts == null || texts.Count == 0) return "No text";

            var eligibleText = texts.Where(x => x.MinRelation <= _currentRelation)
                .OrderByDescending(x => x.MinRelation)
                .ToList();

            return eligibleText.FirstOrDefault()?.NpcText ?? "No text";
        }

        private async void OnOptionSelected(DialogueOption option)
        {
            _currentRelation = Mathf.Clamp(_currentRelation + option.RelationEffect, -100, 100);

            if (!string.IsNullOrEmpty(option.TargetNode))
            {
                _previousNode = _currentNode;
                SetCurrentNode(option.TargetNode);
            }
            else
            {
                if (!string.IsNullOrEmpty(option.Text))
                {
                    _dialogWindow?.UpdateText(option.Text);
                    await ToSignal(_dialogWindow, "TextEnd");
                }
                EndDialogue();
            }
        }
    }
}
