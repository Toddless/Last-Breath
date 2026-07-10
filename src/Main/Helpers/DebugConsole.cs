namespace LastBreath.Helpers
{
    using System;
    using System.Linq;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Narrative;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Godot;

    /// <summary>
    /// Debug-build console (autoload, F12): drive quests, facts, influence and reputation without
    /// UI. The one place UI is built in code on purpose — it must not depend on anything it tests.
    /// Services resolve lazily per command so the autoload never races the provider bootstrap.
    /// </summary>
    public partial class DebugConsole : CanvasLayer
    {
        private RichTextLabel? _output;
        private LineEdit? _input;
        private Control? _root;

        public override void _Ready()
        {
            if (!OS.IsDebugBuild())
            {
                QueueFree();
                return;
            }

            Layer = 100;
            BuildUi();
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true, Keycode: Key.F12 }) return;
            _root!.Visible = !_root.Visible;
            if (_root.Visible) _input!.GrabFocus();
        }

        private void BuildUi()
        {
            _root = new PanelContainer
            {
                Visible = false,
                AnchorRight = 1,
                AnchorBottom = 0.45f,
            };
            var layout = new VBoxContainer();
            _output = new RichTextLabel { ScrollFollowing = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None };
            _input = new LineEdit { PlaceholderText = "help" };
            _input.TextSubmitted += OnSubmitted;
            layout.AddChild(_output);
            layout.AddChild(_input);
            _root.AddChild(layout);
            AddChild(_root);
        }

        private void OnSubmitted(string text)
        {
            _input!.Clear();
            if (text.Trim().Length == 0) return;
            Print($"> {text}");
            try
            {
                Execute(text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception exception)
            {
                Print($"[color=red]{exception.Message}[/color]");
            }
        }

        private void Execute(string[] args)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "help": PrintHelp(); break;
                case "quest": ExecuteQuest(args); break;
                case "fact": ExecuteFact(args); break;
                case "influence": ExecuteInfluence(args); break;
                case "rep": ExecuteReputation(args); break;
                default: Print("Unknown command, try: help"); break;
            }
        }

        private void ExecuteQuest(string[] args)
        {
            var log = Service<IQuestLogService>();
            var quests = Service<IQuestProvider>();
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "list";

            if (sub == "list")
            {
                foreach (var quest in quests.All)
                {
                    var state = log.GetState(quest.Id);
                    string progress = state == null ? "-" : ProgressOf(log, quest, state);
                    Print($"{quest.Id}: {state?.Status.ToString() ?? "NotTaken"} {progress}");
                }
                return;
            }

            if (args.Length < 3)
            {
                Print("quest <accept|decline|abandon|fail|turnin> <questId>");
                return;
            }

            string id = args[2];
            bool done = sub switch
            {
                "accept" => log.Accept(id, NarrativeContext.Empty),
                "turnin" => log.TurnIn(id, NarrativeContext.Empty),
                "decline" => Run(() => log.Decline(id, NarrativeContext.Empty)),
                "abandon" => Run(() => log.Abandon(id)),
                "fail" => Run(() => log.Fail(id, "DebugConsole")),
                _ => false,
            };
            Print(done ? $"{sub} {id}: ok → {log.GetStatus(id)?.ToString() ?? "NotTaken"}" : $"{sub} {id}: rejected");
        }

        private string ProgressOf(IQuestLogService log, QuestDefinition quest, QuestState state)
        {
            if (state.Status != QuestStatus.Active) return string.Empty;
            var stage = quest.Stages[state.StageIndex];
            var parts = stage.Objectives.Select(objective =>
            {
                (int current, int required) = log.GetObjectiveProgress(quest.Id, objective.Id);
                return $"{objective.Id} {current}/{required}";
            });
            return $"[{stage.Id}: {string.Join(", ", parts)}]";
        }

        private void ExecuteFact(string[] args)
        {
            var facts = Service<IWorldFactsService>();
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "dump")
            {
                case "set" when args.Length > 2:
                    if (args.Length > 3 && int.TryParse(args[3], out int amount)) facts.Add(args[2], amount);
                    else facts.SetFact(args[2]);
                    Print($"{args[2]} = {facts.GetCount(args[2])}");
                    break;
                case "dump":
                    string prefix = args.Length > 2 ? args[2] : string.Empty;
                    foreach ((string key, int count) in facts.Snapshot.Where(fact => fact.Key.StartsWith(prefix)).OrderBy(fact => fact.Key))
                        Print($"{key} = {count}");
                    Print($"({facts.Snapshot.Count} total)");
                    break;
                default:
                    Print("fact set <key> [amount] | fact dump [prefix]");
                    break;
            }
        }

        private void ExecuteInfluence(string[] args)
        {
            var mastery = Service<IInfluenceMastery>();
            if (args.Length > 2 && args[1].ToLowerInvariant() == "exp" && int.TryParse(args[2], out int exp))
                mastery.AddExperience(exp);
            Print($"Influence: level {mastery.CurrentLevel}/{mastery.MaximumLevel}, exp {mastery.CurrentExperience}, to next {mastery.ExpToNextLevelRemain()}");
        }

        private void ExecuteReputation(string[] args)
        {
            var relations = Service<IFactionRelationService>();
            if (args.Length > 3 && args[1].ToLowerInvariant() == "add"
                && Enum.TryParse(args[2], true, out Fractions faction) && int.TryParse(args[3], out int delta))
            {
                relations.AddReputation(faction, delta, "DebugConsole");
                Print($"{faction}: {relations.GetReputation(faction)} ({relations.GetPlayerRelation(faction)})");
                return;
            }
            Print("rep add <faction> <delta>");
        }

        private void PrintHelp()
        {
            Print("quest list | quest <accept|decline|abandon|fail|turnin> <questId>");
            Print("fact set <key> [amount] | fact dump [prefix]");
            Print("influence [exp <n>]");
            Print("rep add <faction> <delta>");
        }

        private static bool Run(Action action)
        {
            action();
            return true;
        }

        private static T Service<T>() => Services.GameServiceProvider.Instance.GetService<T>();

        private void Print(string line) => _output!.AppendText(line + "\n");
    }
}
