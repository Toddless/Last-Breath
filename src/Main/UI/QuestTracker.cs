namespace LastBreath.UI
{
    using System.Linq;
    using Core.Events;
    using Core.Localization;
    using Core.Narrative.Facts;
    using Core.Narrative.Quests;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The compact on-screen tracker: active quests with the current stage's visible objectives.
    /// A scene node the HUD hosts (instance QuestTracker.tscn into PlayerHUD) — it feeds itself
    /// from the services and lives on subscriptions like the world nodes do.
    /// </summary>
    [GlobalClass]
    public partial class QuestTracker : VBoxContainer
    {
        private const int MaxTrackedQuests = 3;

        private IQuestLogService? _questLog;
        private IQuestProvider? _quests;
        private IGameEventBus? _events;
        private IWorldFactsService? _facts;

        public override void _Ready()
        {
            _questLog = Services.GameServiceProvider.Instance.GetService<IQuestLogService>();
            _quests = Services.GameServiceProvider.Instance.GetService<IQuestProvider>();
            _facts = Services.GameServiceProvider.Instance.GetService<IWorldFactsService>();
            _events = Services.GameServiceProvider.Instance.GetService<IGameEventBus>();

            _events.Subscribe<QuestStatusChangedEvent>(OnQuestsChanged);
            _events.Subscribe<QuestStageAdvancedEvent>(OnStageAdvanced);
            _facts.FactChanged += OnFactChanged;
            Render();
        }

        public override void _ExitTree()
        {
            _events?.Unsubscribe<QuestStatusChangedEvent>(OnQuestsChanged);
            _events?.Unsubscribe<QuestStageAdvancedEvent>(OnStageAdvanced);
            if (_facts != null) _facts.FactChanged -= OnFactChanged;
        }

        private void OnQuestsChanged(QuestStatusChangedEvent evnt) => Render();

        private void OnStageAdvanced(QuestStageAdvancedEvent evnt) => Render();

        private void OnFactChanged(string key, int count) => Render();

        private void Render()
        {
            this.QueueFreeChildren();

            var tracked = _questLog?.States
                .Where(state => state.Status is QuestStatus.Active or QuestStatus.ReadyToTurnIn)
                .Take(MaxTrackedQuests) ?? [];

            foreach (var state in tracked)
                RenderQuest(state);
        }

        private void RenderQuest(QuestState state)
        {
            if (_quests?.Get(state.QuestId) is not { } quest) return;

            AddLine($"[b]{Localization.Localize(quest.Id)}[/b]");
            if (state.Status == QuestStatus.ReadyToTurnIn)
            {
                AddLine($"  [color=green]{Localization.Localize("UI_Quest_Status_ReadyToTurnIn")}[/color]");
                return;
            }

            if (state.StageIndex >= quest.Stages.Count) return;
            foreach (var objective in quest.Stages[state.StageIndex].Objectives.Where(entry => !entry.IsHidden && !entry.IsOptional))
            {
                (int current, int required) = _questLog!.GetObjectiveProgress(quest.Id, objective.Id);
                string mark = current >= required ? "[color=green]✔[/color]" : "•";
                string progress = required > 1 ? $" {current}/{required}" : string.Empty;
                AddLine($"  {mark} {Localization.Localize($"{quest.Id}_Obj_{objective.Id}")}{progress}");
            }
        }

        private void AddLine(string bbcode)
        {
            var label = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AppendText(bbcode);
            AddChild(label);
        }
    }
}
