namespace LastBreath.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Events;
    using Core.Localization;
    using Core.Narrative.Facts;
    using Core.Narrative.Quests;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The quest journal: taken quests on the left, the selected one's story on the right.
    /// Old school — objectives and progress, no map markers. Hidden objectives stay hidden,
    /// optional ones are labeled. Progress re-renders live while the window is open.
    /// </summary>
    public partial class QuestJournalWindow : Control, IWindow
    {
        private const string UID = "uid://bqstjrn1wnd4a";

        private static readonly QuestStatus[] s_listOrder =
        [
            QuestStatus.ReadyToTurnIn, QuestStatus.Active, QuestStatus.Completed, QuestStatus.Failed, QuestStatus.Declined,
        ];

        [Export] private ItemList? _questList;
        [Export] private Label? _title;
        [Export] private RichTextLabel? _details;
        [Export] private Button? _abandonButton;

        private readonly List<string> _listedQuestIds = [];
        private IQuestLogService? _questLog;
        private IQuestProvider? _quests;
        private IGameEventBus? _events;
        private IWorldFactsService? _facts;
        private string? _selectedQuestId;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void InjectServices(IGameServiceProvider provider)
        {
            _questLog = provider.GetService<IQuestLogService>();
            _quests = provider.GetService<IQuestProvider>();
            _facts = provider.GetService<IWorldFactsService>();
            _events = provider.GetService<IGameEventBus>();

            _events.Subscribe<QuestStatusChangedEvent>(OnQuestsChanged);
            _events.Subscribe<QuestStageAdvancedEvent>(OnStageAdvanced);
            _facts.FactChanged += OnFactChanged;

            if (_questList != null) _questList.ItemSelected += OnQuestSelected;
            if (_abandonButton != null)
            {
                _abandonButton.Text = Localization.Localize("UI_Journal_Abandon");
                _abandonButton.Pressed += OnAbandonPressed;
            }

            Rebuild();
        }

        public void Close() => QueueFree();

        public override void _ExitTree()
        {
            _events?.Unsubscribe<QuestStatusChangedEvent>(OnQuestsChanged);
            _events?.Unsubscribe<QuestStageAdvancedEvent>(OnStageAdvanced);
            if (_facts != null) _facts.FactChanged -= OnFactChanged;
        }

        private void OnQuestsChanged(QuestStatusChangedEvent evnt) => Rebuild();

        private void OnStageAdvanced(QuestStageAdvancedEvent evnt) => RenderDetails();

        private void OnFactChanged(string key, int count) => RenderDetails();

        private void OnQuestSelected(long index)
        {
            _selectedQuestId = index >= 0 && index < _listedQuestIds.Count ? _listedQuestIds[(int)index] : null;
            RenderDetails();
        }

        private void OnAbandonPressed()
        {
            if (_selectedQuestId != null) _questLog?.Abandon(_selectedQuestId);
        }

        private void Rebuild()
        {
            if (_questList == null || _questLog == null) return;

            _questList.Clear();
            _listedQuestIds.Clear();

            var states = _questLog.States
                .OrderBy(state => System.Array.IndexOf(s_listOrder, state.Status))
                .ToList();

            foreach (var state in states)
            {
                _listedQuestIds.Add(state.QuestId);
                _questList.AddItem($"{Localization.Localize(state.QuestId)} — {StatusText(state.Status)}");
            }

            if (_selectedQuestId == null || !_listedQuestIds.Contains(_selectedQuestId))
                _selectedQuestId = _listedQuestIds.FirstOrDefault();
            if (_selectedQuestId != null)
                _questList.Select(_listedQuestIds.IndexOf(_selectedQuestId));

            RenderDetails();
        }

        private void RenderDetails()
        {
            if (_title == null || _details == null) return;

            if (_selectedQuestId == null || _questLog?.GetState(_selectedQuestId) is not { } state
                || _quests?.Get(_selectedQuestId) is not { } quest)
            {
                _title.Text = string.Empty;
                _details.Text = Localization.Localize("UI_Journal_Empty");
                _abandonButton?.Hide();
                return;
            }

            _title.Text = Localization.Localize(quest.Id);
            _details.Clear();
            _details.AppendText($"[i]{StatusText(state.Status)}[/i]\n\n");
            _details.AppendText($"{Localization.LocalizeDescription(quest.Id)}\n");

            if (state.Status is QuestStatus.Active or QuestStatus.ReadyToTurnIn)
                RenderObjectives(quest, state);

            bool abandonable = state.Status is QuestStatus.Active or QuestStatus.ReadyToTurnIn;
            _abandonButton?.SetVisible(abandonable);
        }

        private void RenderObjectives(QuestDefinition quest, QuestState state)
        {
            if (_details == null || state.StageIndex >= quest.Stages.Count) return;
            var stage = quest.Stages[state.StageIndex];

            _details.AppendText($"\n[b]{Localization.Localize($"{quest.Id}_Stage_{stage.Id}")}[/b]\n");
            foreach (var objective in stage.Objectives.Where(entry => !entry.IsHidden))
            {
                (int current, int required) = _questLog!.GetObjectiveProgress(quest.Id, objective.Id);
                string mark = current >= required ? "[color=green]✔[/color]" : "•";
                string optional = objective.IsOptional ? $" [i]({Localization.Localize("UI_Journal_Optional")})[/i]" : string.Empty;
                string progress = required > 1 ? $" {current}/{required}" : string.Empty;
                _details.AppendText($"  {mark} {Localization.Localize($"{quest.Id}_Obj_{objective.Id}")}{progress}{optional}\n");
            }
        }

        private static string StatusText(QuestStatus status) => Localization.Localize($"UI_Quest_Status_{status}");
    }
}
