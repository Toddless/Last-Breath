namespace Playground.Script.UI.View
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Resource.Quests;
    using System.Linq;
    using Playground.Script.QuestSystem;

    public partial class NPCsQuests : HBoxContainer
    {
        private VBoxContainer? _questsList;
        private Button? _close, _accept;
        private RichTextLabel? _description;
        private Quest? _questToAccept;

        [Signal]
        public delegate void ClosePressedEventHandler();

        public override void _Ready()
        {
            _questsList = (VBoxContainer?)NodeFinder.FindBFSCached(this, "QuestList");
            _close = (Button?)NodeFinder.FindBFSCached(this, "Close");
            _accept = (Button?)NodeFinder.FindBFSCached(this, "Accept");
            _description = (RichTextLabel?)NodeFinder.FindBFSCached(this, "QuestDescription");
            NodeFinder.ClearCache();
            SetEvents();
        }
        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath.NPCsQuests);

        public void AddQuests(Quest quest)
        {
            var questOption = QuestOption.Initialize().Instantiate<QuestOption>();
            questOption.Bind(quest);
            questOption.QuestDetails += OnDescription;
            _questsList?.AddChild(questOption);
        }

        public override void _ExitTree()
        {
            Unbind();
            base._ExitTree();
        }

        private void SetEvents()
        {
            _close!.Pressed += OnClosePressed;
            _accept!.Pressed += OnAcceptPressed;
        }

        private void OnAcceptPressed()
        {
            if (_questToAccept == null) return;
            QuestManager.Instance.OnQuestAccepted(_questToAccept);
            _questsList?.GetChildren().Cast<QuestOption>()?.FirstOrDefault(x => x.IsMatch(_questToAccept.Id) == true)?.QueueFree();
            _description!.Text = string.Empty;
            _questToAccept = null;
        }

        private void OnClosePressed()
        {
            EmitSignal(SignalName.ClosePressed);
            foreach (var item in _questsList!.GetChildren())
            {
                item.QueueFree();
            }
            QueueFree();
        }

        private void OnDescription(string obj, Quest? quest)
        {
            _description!.Text = obj;
            _questToAccept = quest;
        }

        private void Unbind()
        {
            _close!.Pressed -= OnClosePressed;
            _accept!.Pressed -= OnAcceptPressed;
            if (_questToAccept != null) _questToAccept = null;
            if (_description?.Text != null) _description.Text = null;
        }
    }
}
