namespace LastBreath.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Battle.Source.Effects;
    using Core.Ai.World.Raids;
    using Core.Ai.World.Time;
    using Core.Battle.Abilities;
    using Core.Crafting;
    using Core.Data.AbilityData;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Narrative;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Context;
    using Core.PassiveTree.Rules;
    using Core.Save;
    using Core.Services;
    using Godot;

    /// <summary>
    /// Debug-build console (autoload, F12): drive and inspect quests, facts, influence, reputation,
    /// the passive tree, player vitals/stats, items, world time and NPC spawns without UI. Systems
    /// that have no game interface yet are accepted by hand through here. The one place UI is
    /// built in code on purpose — it must not depend on anything it tests. Services resolve lazily
    /// per command so the autoload never races the provider bootstrap.
    /// </summary>
    public partial class DebugConsole : CanvasLayer
    {
        private const string ModifierSource = "debugConsole";

        private const string PassiveTreeUsage =
            "passive tree | passive frontier | passive node <nodeId> | passive take <nodeId> | passive refund <nodeId> | passive dump | passive respec";

        private const string AugmentUsage =
            "aug catalog [filter] | aug bag | aug sockets | aug install <socket> <copy> | aug extract <socket>";

        private const string OrnamentUsage =
            "orn list | orn attach <ornamentId> <abilityId> | orn detach <ornamentId>";

        /// <summary>How many frontier rows one answer prints: the whole frontier of a wide allocation
        /// would scroll the useful lines out of the console.</summary>
        private const int FrontierLimit = 25;

        /// <summary>How many catalog rows one answer prints. The augment section is over a hundred
        /// records long; a filter is how the rest of it is reached.</summary>
        private const int CatalogLimit = 25;

        /// <summary>How much of a copy id the listings print. A copy id is a guid, and the head of one
        /// is what a player can retype — the install command takes any head naming exactly one copy.</summary>
        private const int CopyIdHeadLength = 8;

        private readonly List<string> _history = [];
        private int _historyIndex;
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
            _root = new PanelContainer { Visible = false, AnchorRight = 1, AnchorBottom = 0.45f, };
            var layout = new VBoxContainer();
            _output = new RichTextLabel { ScrollFollowing = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None };
            UseFixedWidthFont(_output);
            _input = new LineEdit { PlaceholderText = "help" };
            _input.TextSubmitted += OnSubmitted;
            _input.GuiInput += OnInputKey;
            layout.AddChild(_output);
            layout.AddChild(_input);
            _root.AddChild(layout);
            AddChild(_root);
        }

        /// <summary>Puts the transcript in a fixed-width face. The listings here are columns padded with
        /// spaces, and columns only line up while every character is the same width. The face is asked
        /// for by name, so a machine without it falls back to the theme's own rather than failing; the
        /// bold face is left alone, which keeps the help headings apart from the lines under them.</summary>
        private static void UseFixedWidthFont(RichTextLabel output) =>
            output.AddThemeFontOverride("normal_font", new SystemFont { FontNames = ["Consolas", "Courier New", "monospace"] });

        // ↑/↓ recall previous commands; one step past the newest entry restores an empty line.
        private void OnInputKey(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true, Keycode: Key.Up or Key.Down } key) return;
            NavigateHistory(key.Keycode == Key.Up ? -1 : 1);
            _input!.AcceptEvent();
        }

        private void NavigateHistory(int direction)
        {
            if (_history.Count == 0) return;
            _historyIndex = Mathf.Clamp(_historyIndex + direction, 0, _history.Count);
            _input!.Text = _historyIndex == _history.Count ? string.Empty : _history[_historyIndex];
            _input.CaretColumn = _input.Text.Length;
        }

        private void OnSubmitted(string text)
        {
            _input!.Clear();
            if (text.Trim().Length == 0) return;
            _history.Add(text.Trim());
            _historyIndex = _history.Count;
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
                case "martial": ExecuteMartial(args); break;
                case "passive": ExecutePassiveTree(args); break;
                case "craft": ExecuteCraft(args); break;
                case "rep": ExecuteReputation(args); break;
                case "item": ExecuteItem(args); break;
                case "aug": ExecuteAugment(args); break;
                case "orn": ExecuteOrnament(args); break;
                case "stats": ExecutePlayerStats(args); break;
                case "heal": ExecuteHeal(args); break;
                case "trade": ExecuteTrade(args); break;
                case "restore": ExecuteRestore(args); break;
                case "cd": ExecuteCooldowns(args); break;
                case "effect": ExecuteEffect(args); break;
                case "ability": ExecuteAbility(args); break;
                case "time": ExecuteTime(args); break;
                case "spawn": ExecuteSpawn(args); break;
                case "tp": ExecuteTeleport(args); break;
                case "inv": ExecuteInventory(args); break;
                case "raid": ExecuteRaid(); break;
                case "kill": ExecuteKill(); break;
                case "revive": ExecuteRevive(); break;
                case "save": ExecuteSave(args); break;
                case "load": ExecuteLoad(args); break;
                default: Print("Unknown command, try: help"); break;
            }
        }

        /// <summary>trade [traderId] — opens the trade window (test path until an NPC carries it).</summary>
        private void ExecuteTrade(string[] args)
        {
            string traderId = args.Length > 1 ? args[1] : "Trader_Human_Merchant";
            Service<IGameMessageBus>().PublishMessageAsync(new Core.MessageBus.Messages.OpenTradeWindowMessage(traderId));
            Print($"trade window requested: {traderId}");
        }

        private void ExecuteHeal(string[] args)
        {
            var player = Service<IPlayerAccessor>().Player;
            if (player == null || args.Length < 2 || !TryParseFloat(args[1], out float amount))
            {
                Print("heal <amount>");
                return;
            }

            player.CurrentHealth += amount;
            Print($"Health: {player.CurrentHealth}/{player.Parameters.MaxHealth}");
        }

        private void ExecuteRestore(string[] args)
        {
            var player = Service<IPlayerAccessor>().Player;
            if (player == null) return;
            string target = args.Length > 1 ? args[1].ToLowerInvariant() : "all";
            if (target is not ("hp" or "mana" or "barrier" or "all"))
            {
                Print("restore [hp|mana|barrier|all]");
                return;
            }

            if (target is "hp" or "all") player.CurrentHealth = player.Parameters.MaxHealth;
            if (target is "mana" or "all") player.CurrentMana = player.Parameters.MaxMana;
            if (target is "barrier" or "all") player.CurrentBarrier = player.Parameters.MaxBarrier;
            Print($"HP {player.CurrentHealth}/{player.Parameters.MaxHealth}, mana {player.CurrentMana}/{player.Parameters.MaxMana}, barrier {player.CurrentBarrier}/{player.Parameters.MaxBarrier}");
        }

        private void ExecuteCooldowns(string[] args)
        {
            var player = Service<IPlayerAccessor>().Player;
            if (player == null) return;
            if (args.Length > 1 && args[1].ToLowerInvariant() == "reset")
            {
                int count = 0;
                foreach (var ability in player.AbilityBook.AllAbilities.Where(ability => ability.CooldownLeft > 0))
                {
                    ability.CooldownLeft = 0;
                    count++;
                }

                Print($"Cooldowns reset: {count}");
                return;
            }

            var cooling = player.AbilityBook.AllAbilities.Where(ability => ability.CooldownLeft > 0).ToList();
            if (cooling.Count == 0)
            {
                Print("All abilities ready | cd reset");
                return;
            }

            foreach (var ability in cooling)
                Print($"{ability.Id}: {ability.CooldownLeft}/{ability.Cooldown}");
        }

        private void ExecutePlayerStats(string[] args)
        {
            var player = Service<IPlayerAccessor>().Player;
            if (player == null) return;
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "list")
            {
                case "list":
                    foreach (var parameter in Enum.GetValues<EntityParameter>())
                        Print($"{parameter}: {player.Parameters.GetValueForParameter(parameter)}");
                    break;
                case "add" when args.Length > 4 && Enum.TryParse(args[2], true, out EntityParameter stat)
                                                && TryParseFloat(args[3], out float amount)
                                                && Enum.TryParse(args[4], true, out ModifierValueType type):
                    player.ParameterModifiers.AddModifier(ModifiersCreator.CreateModifierInstance(stat, type, amount, ModifierSource));
                    Print($"{stat}: {player.Parameters.GetValueForParameter(stat)}");
                    break;
                case "remove" when args.Length > 2 && Enum.TryParse(args[2], true, out EntityParameter stat):
                    foreach (var modifier in player.ParameterModifiers.GetModifiers(stat).Where(modifier => modifier.Source == ModifierSource).ToList())
                        player.ParameterModifiers.RemoveModifier(modifier);
                    Print($"{stat}: {player.Parameters.GetValueForParameter(stat)}");
                    break;
                case "clear":
                    player.ParameterModifiers.RemoveModifierBySource(ModifierSource);
                    Print("Console stat modifiers cleared");
                    break;
                default:
                    Print("stats list | stats add <param> <amount> <flat|increase|multiplicative> | stats remove <param> | stats clear");
                    break;
            }
        }

        private void ExecuteEffect(string[] args)
        {
            var player = Service<IPlayerAccessor>().Player;
            if (player == null) return;
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "list")
            {
                case "list":
                    var views = player.Effects.GetEffectViews();
                    if (views.Count == 0) Print("No active effects");
                    foreach (var view in views)
                        Print($"{view.Id} x{view.Stacks}, {view.Duration} turns");
                    break;
                case "stun":
                    player.Effects.AddEffect(new StunEffect(DurationOf(args)));
                    Print("Stun applied");
                    break;
                case "freeze":
                    player.Effects.AddEffect(new FreezeEffect(DurationOf(args)));
                    Print("Freeze applied");
                    break;
                case "clear":
                    player.Effects.RemoveAllEffects();
                    Print("Effects cleared");
                    break;
                default:
                    Print("effect list | effect <stun|freeze> [duration] | effect clear");
                    break;
            }
        }

        private static int DurationOf(string[] args) => args.Length > 2 && int.TryParse(args[2], out int duration) ? duration : 1;

        private void ExecuteAbility(string[] args)
        {
            var player = Service<IPlayerAccessor>().Player;
            var provider = Service<IAbilityProvider>();
            if (player == null) return;
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "list")
            {
                case "list":
                    var known = player.AbilityBook.AllAbilities.Select(ability => ability.Id).ToHashSet();
                    foreach (string id in provider.KnownAbilityIds.OrderBy(id => id))
                        Print($"{id} ({provider.GetAbilityStance(id)}){(known.Contains(id) ? " [learned]" : string.Empty)}");
                    break;
                case "learn" when args.Length > 2:
                    string abilityId = args[2];
                    if (!provider.KnownAbilityIds.Contains(abilityId))
                    {
                        Print($"Unknown ability: {abilityId} | ability list");
                        return;
                    }

                    player.AbilityBook.Learn(provider.GetAbilityStance(abilityId), provider.CreateAbility(abilityId));
                    Print($"Learned {abilityId} ({provider.GetAbilityStance(abilityId)})");
                    break;
                case "show" when args.Length > 2:
                    PrintLearnedAbility(player, args[2]);
                    break;
                default:
                    Print("ability list | ability learn <abilityId> | ability show <abilityId>");
                    break;
            }
        }

        /// <summary>
        /// One learned ability as it actually casts, and what it is wearing. The numbers on the first
        /// line are the LIVE ones — everything in the ability's sockets is already counted in them — so
        /// seating an augment and printing this again is where an install stops being an arrangement
        /// and becomes a build. Each row beneath names the slot, what stands in it and the numbers that
        /// copy is working at, which are the copy's own and not the record's averages.
        /// </summary>
        private void PrintLearnedAbility(IPlayer player, string abilityId)
        {
            IAbility? ability = player.AbilityBook.AllAbilities.FirstOrDefault(learned => learned.Id == abilityId);
            if (ability == null)
            {
                Print($"Not learned: {abilityId} | ability list");
                return;
            }

            Print($"{ability.Id}: costs {ability.CostValue} {ability.CostType}, waits {Rounded(ability.Cooldown)} turn(s)");
            if (ability.InstalledUpgrades.Count == 0)
            {
                Print("Wears nothing: no augment sits in its sockets | aug sockets");
                return;
            }

            PrintTable(
                ["socket (address)", "augment", "tier", "working at"],
                [
                    .. ability.InstalledUpgrades
                        .OrderBy(worn => worn.Value.Tier)
                        .ThenBy(worn => worn.Key, StringComparer.Ordinal)
                        .Select(worn => new[] { worn.Key, worn.Value.Id, Text(worn.Value.Tier), AppliedNumbers(worn.Value) })
                ]);
        }

        /// <summary>The numbers the upgrade was built with — the seated copy's own, since the copy's
        /// dictionary is what both the behaviour and the description are made from.</summary>
        private static string AppliedNumbers(IAugment augment) =>
            augment.DescriptionValues.Count == 0
                ? "no numbers"
                : string.Join(", ", augment.DescriptionValues
                    .OrderBy(applied => applied.Key, StringComparer.Ordinal)
                    .Select(applied => $"{applied.Key} {applied.Value}"));

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
                "fail" => log.Fail(id, "DebugConsole"),
                _ => false,
            };
            Print(done ? $"{sub} {id}: ok → {log.GetStatus(id)?.ToString() ?? "NotTaken"}" : $"{sub} {id}: rejected");
        }

        private string ProgressOf(IQuestLogService log, QuestDefinition quest, QuestState state)
        {
            if (state.Status != QuestStatus.Active || quest.Stage(state.StageId) is not { } stage) return string.Empty;
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

        private void ExecuteMartial(string[] args)
        {
            var mastery = Service<Core.Battle.IMartialArtMastery>();
            if (args.Length > 2 && args[1].ToLowerInvariant() == "exp" && int.TryParse(args[2], out int exp))
                mastery.AddExperience(exp);
            Print($"Martial Art: level {mastery.CurrentLevel}/{mastery.MaximumLevel}, exp {mastery.CurrentExperience}, to next {mastery.ExpToNextLevelRemain()}");
        }

        // The tree has no game UI yet, so this is the only way to drive it by hand. Every branch is a
        // call into IPassiveTreeService: the console shows its answer, it never decides anything.
        private void ExecutePassiveTree(string[] args)
        {
            var tree = Service<IPassiveTreeService>();
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "tree")
            {
                case "tree": PrintTreeState(tree); break;
                case "frontier": PrintFrontier(tree); break;
                case "node" when args.Length > 2: PrintNode(tree, args[2]); break;
                case "take" when args.Length > 2: PrintAllocation(tree, "take", args[2], tree.Take(args[2])); break;
                case "refund" when args.Length > 2: PrintAllocation(tree, "refund", args[2], tree.Refund(args[2])); break;
                case "dump": PrintTreeModifiers(tree); break;
                case "respec": Respec(tree); break;
                default:
                    Print(PassiveTreeUsage);
                    break;
            }
        }

        /// <summary>
        /// The console's respec is the free, systemic one — the same road a new game and a session reset
        /// take — because a debug command that could not undo an allocation without gold would be a
        /// debug command nobody can use.
        /// <para>It prints what the player would have paid all the same: a free command that says nothing
        /// about the price is a quiet way around it, and the number stops being looked at.</para>
        /// </summary>
        private void Respec(IPassiveTreeService tree)
        {
            int paidFor = tree.SpentPoints;
            int price = Service<IPassiveRespecPricing>().PriceOf(paidFor, Service<Core.Battle.IMartialArtMastery>().EarnedLevel);

            tree.Respec();
            Print($"Respec done, free. {paidFor} node(s) would have cost the player {price} gold. {PointsOf(tree)}");
        }

        private void PrintTreeState(IPassiveTreeService tree)
        {
            Print($"Passive tree: {tree.Tree.Nodes.Count} node(s), design budget {tree.Tree.Budget}");
            Print(PointsOf(tree));
            Print(tree.TakenNodes.Count == 0
                ? "Taken: none"
                : $"Taken: {string.Join(", ", tree.TakenNodes.OrderBy(id => id, StringComparer.Ordinal))}");
        }

        /// <summary>
        /// What can be bought right now, so a node can be picked without reading the tree file — whose
        /// node count and ids move under an authoring pass, which is exactly why the pick is made here.
        /// Adjacency is not recomputed: a node is listed when <see cref="IPassiveTreeService.CheckTake"/>
        /// says it would succeed, so the list can never disagree with what <c>passive take</c> does.
        /// </summary>
        private void PrintFrontier(IPassiveTreeService tree)
        {
            List<PassiveNode> available = tree.Tree.Nodes
                .Where(node => tree.CheckTake(node.Id) == AllocationResult.Success)
                .OrderBy(node => node.Id, StringComparer.Ordinal)
                .ToList();

            Print($"Frontier: {available.Count} node(s) available, {Math.Min(available.Count, FrontierLimit)} shown. {PointsOf(tree)}");
            foreach (PassiveNode node in available.Take(FrontierLimit)) Print($"  {HeadlineOf(node)}");
        }

        /// <summary>
        /// One node in full: what it is, whether the character holds it, what it would cost, what it
        /// touches and what it grants. The availability verdict is the service's own answer, printed
        /// through the same wording a refused purchase uses.
        /// </summary>
        private void PrintNode(IPassiveTreeService tree, string nodeId)
        {
            PassiveNode? node = tree.Tree.Find(nodeId);
            if (node == null)
            {
                Print($"[color=red]node {nodeId}: {ReasonOf(tree.Tree, nodeId, AllocationResult.UnknownNode)}[/color]");
                return;
            }

            var formatter = Service<ModifierFormatter>();
            Print(HeadlineOf(node));
            Print($"  taken: {tree.IsTaken(node.Id)}, costs a point: {NodeKindRules.CostsPoint(node.Kind)}, take now: {ReasonOf(tree.Tree, node.Id, tree.CheckTake(node.Id))}");
            if (node.AbilityId.Length > 0) Print($"  ability: {node.AbilityId}");
            Print($"  neighbours: {Listed(tree.Tree.Neighbours(node.Id).OrderBy(id => id, StringComparer.Ordinal))}");
            Print($"  lines: {Listed(node.Modifiers.Select(line => FormatTreeLine(formatter, line)))}");
        }

        private void PrintAllocation(IPassiveTreeService tree, string action, string nodeId, AllocationResult result)
        {
            if (result != AllocationResult.Success)
            {
                Print($"[color=red]{action} {nodeId}: refused — {ReasonOf(tree.Tree, nodeId, result)}[/color]");
                return;
            }

            Print($"{action} {NameOf(tree.Tree, nodeId)}: ok. {PointsOf(tree)}");
        }

        /// <summary>Everything the taken nodes contribute. The tree feeds two channels that never meet —
        /// parameters and battle pipelines — and a dump that showed one of them would call an allocation
        /// empty while half of it is on the character.</summary>
        private void PrintTreeModifiers(IPassiveTreeService tree)
        {
            PrintTreeParameters(tree);
            PrintTreeKnobs(tree);
        }

        /// <summary>
        /// The parametric channel, read straight off the modifier source the tree publishes and
        /// grouped by parameter. The resolved player value is shown only when the character actually
        /// carries that source — otherwise the tree holds these lines and the character does not.
        /// </summary>
        private void PrintTreeParameters(IPassiveTreeService tree)
        {
            var parameters = tree.ParameterSource.AffectedParameters.OrderBy(parameter => parameter.ToString(), StringComparer.Ordinal).ToList();
            if (parameters.Count == 0)
            {
                Print($"No parametric lines from {tree.TakenNodes.Count} taken node(s)");
                return;
            }

            var formatter = Service<ModifierFormatter>();
            var formats = Service<IParameterFormatProvider>();
            var player = Service<IPlayerAccessor>().Player;
            bool carried = player != null && parameters.Any(parameter => player.ParameterModifiers.GetModifiers(parameter)
                .Any(modifier => modifier.Source == PassiveTreeDocument.ModifierSource));

            foreach (var parameter in parameters)
            {
                string lines = string.Join(", ", tree.ParameterSource.GetModifiers(parameter).Select(modifier => FormatTreeModifier(formatter, modifier)));
                // The raw total on purpose: this line answers what the tree resolved into the parameter,
                // not what a resistance mitigates with after its maximum.
                string resolved = carried
                    ? $" → player {ParameterValueText.Format(formats, parameter, player!.Parameters.GetValueForParameter(parameter))}"
                    : string.Empty;
                Print($"{parameter}: {lines}{resolved}");
            }

            Print(carried
                ? $"Source \"{PassiveTreeDocument.ModifierSource}\" is registered on the player"
                : $"Source \"{PassiveTreeDocument.ModifierSource}\" is NOT registered on the player — the character carries none of this yet");
        }

        /// <summary>
        /// The context channel: one row per pipeline knob holding everything taken for it — the tree folds
        /// every line feeding a knob into a single modifier, so a per-line dump would print numbers no
        /// pipeline ever reads. The total is the allocation's, read in the unit the knob is counted in, so
        /// a knob taken in whole turns is never credited with a fraction that dies at the binding. It is
        /// bare of units otherwise: knobs have no entry in the parameter format catalog, and the bucket a
        /// line is written in decides nothing beyond the wording of its own sentence.
        /// <para>A knob with conditional lines is printed twice over: the total counting all of them, which
        /// is what the allocation holds, and beside it what the modifier standing behind the knob reads at
        /// this moment. The two part where a condition is off — the same divergence the parametric rows
        /// show per line as [on]/[off], which a folded knob has no room for. A line naming an id the
        /// catalog does not hold parts them as well, and only here: the total counts it, the knob's
        /// modifier does not, and the parametric channel drops such a line before it can be printed at
        /// all.</para>
        /// </summary>
        private void PrintTreeKnobs(IPassiveTreeService tree)
        {
            Dictionary<ContextParameter, List<ContextModifierLine>> knobs = ContextKnobTotals.Gather(tree.Tree, tree.TakenNodes);
            if (knobs.Count == 0)
            {
                Print($"No context knobs from {tree.TakenNodes.Count} taken node(s)");
                return;
            }

            foreach (KeyValuePair<ContextParameter, List<ContextModifierLine>> knob in knobs.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
            {
                string total = Rounded(ContextKnobTotals.AsRead(knob.Key, knob.Value));
                int conditional = knob.Value.Count(line => line.IsConditional);
                string gated = conditional > 0
                    ? $", {conditional} conditional — {Rounded(tree.ContextSource.ValueOf(knob.Key))} in force now"
                    : string.Empty;
                Print($"{knob.Key}: {total} from {knob.Value.Count} line(s){gated}");
            }
        }

        private static string Rounded(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        // Units come from ParameterFormats.json through the game's own formatter, so a parameter that
        // stores a fraction reads as a percent here exactly as it does on the character sheet. A line held
        // up by a condition is named as one: it is in the source and out of the character's value.
        private static string FormatTreeModifier(ModifierFormatter formatter, IModifierInstance modifier) =>
            $"{Signed(formatter, modifier.ModifierValueType, modifier.EntityParameter, modifier.Value)} {modifier.ModifierValueType} ({modifier.Source})"
            + (modifier is IConditionalModifier conditional ? conditional.IsActive ? " [on]" : " [off]" : string.Empty);

        // The dump names the carrier of a per-unit line too: without it the authored number reads as an
        // outright bonus and the console disagrees with the wheel about the same line.
        private static string FormatTreeLine(ModifierFormatter formatter, ModifierLine line) =>
            $"{Signed(formatter, line.ValueType, line.Parameter, line.Value)} {line.Parameter} {line.ValueType}"
            + (line.IsScaled ? $" per {line.PerParameter}" : string.Empty)
            + (line.IsConditional ? $" if \"{line.Condition}\"" : string.Empty);

        /// <summary>The formatter renders the magnitude with its unit and carries the minus sign; the
        /// plus is a console convention and the only thing added on top of it.</summary>
        private static string Signed(ModifierFormatter formatter, ModifierValueType valueType, EntityParameter parameter, float value) =>
            (value < 0 ? string.Empty : "+") + formatter.FormatValue(valueType, parameter, value);

        private static string PointsOf(IPassiveTreeService tree) =>
            $"Points: {tree.TotalPoints} granted, {tree.SpentPoints} spent, {tree.AvailablePoints} left, {tree.TakenNodes.Count} node(s) taken";

        private static string Listed(IEnumerable<string> parts)
        {
            string joined = string.Join(", ", parts);
            return joined.Length == 0 ? "none" : joined;
        }

        private static string HeadlineOf(PassiveNode node) => $"{TitledOf(node)} [{node.Kind}]";

        private static string TitledOf(PassiveNode node) =>
            string.IsNullOrWhiteSpace(node.Title) ? node.Id : $"{node.Id} \"{node.Title}\"";

        private static string NameOf(PassiveTreeDocument document, string nodeId)
        {
            var node = document.Find(nodeId);
            return node == null ? nodeId : TitledOf(node);
        }

        // The refusal spelled out. NotConnected covers two different dead ends — a node the allocation
        // has not reached yet, and a node no allocation can ever reach — so the wording splits them.
        private static string ReasonOf(PassiveTreeDocument document, string nodeId, AllocationResult result) => result switch
        {
            AllocationResult.UnknownNode => "no node with that id in the tree",
            AllocationResult.AlreadyTaken => "already taken",
            AllocationResult.NotTaken => "not taken",
            AllocationResult.Granted => "granted with the character: seeds are never bought and never given back",
            AllocationResult.NotEnoughPoints => "not enough points (martial art levels grant them: martial exp <n>)",
            AllocationResult.NotConnected when document.Neighbours(nodeId).Count == 0 => "the node has no links at all: nothing can ever reach it",
            AllocationResult.NotConnected => "not adjacent to anything already taken",
            AllocationResult.WouldOrphan => "not the end of a branch: giving it back would leave what is behind it hanging",
            _ => result.ToString()
        };

        private void ExecuteCraft(string[] args)
        {
            var mastery = Service<ICraftingMastery>();
            if (args.Length > 2 && args[1].ToLowerInvariant() == "exp" && int.TryParse(args[2], out int exp))
                mastery.AddExperience(exp);
            Print($"Crafting: level {mastery.CurrentLevel}/{mastery.MaximumLevel}, exp {mastery.CurrentExperience}, to next {mastery.ExpToNextLevelRemain()}");
        }

        // item add <id> [amount] [rarity]: with a rarity the equip goes through the loot pipeline
        // (rarity gives the affix slot split), without one it is a plain mint — which is also the door
        // an augment id comes through, the minter offering every id to every kind it knows.
        private void ExecuteItem(string[] args)
        {
            if (args.Length < 3 || args[1].ToLowerInvariant() != "add")
            {
                Print("item add <itemId> [amount] [rarity]");
                return;
            }

            int rarityIndex = 3;
            int amount = 1;
            if (args.Length > 3 && int.TryParse(args[3], out int parsed))
            {
                amount = parsed;
                rarityIndex = 4;
            }

            Rarity? rarity = null;
            if (args.Length > rarityIndex)
            {
                if (!Enum.TryParse(args[rarityIndex], true, out Rarity parsedRarity))
                {
                    Print($"Unknown rarity: {args[rarityIndex]} ({string.Join(", ", Enum.GetNames<Rarity>())})");
                    return;
                }

                if (parsedRarity is Rarity.Mythic or Rarity.Unique)
                {
                    Print("Mythic/Unique are authored templates — mint them by their item id without a rarity");
                    return;
                }

                rarity = parsedRarity;
            }

            var inventory = Service<IInventory>();
            AddItems(inventory, args[2], amount, rarity);
            Print($"{args[2]}{(rarity != null ? $" ({rarity})" : string.Empty)}: now {inventory.GetTotalItemAmount(args[2])}");
        }

        /// <summary>
        /// Puts the amount into the bag. A stackable id is minted once and laid out as a stack; one that
        /// rolls — an equip, an augment — is minted once PER COPY, because a single instance laid into
        /// several slots is one draw shown many times over: every slot shows the same numbers, and
        /// whatever takes the instance out of the bag empties all of them at once.
        /// </summary>
        private void AddItems(IInventory inventory, string itemId, int amount, Rarity? rarity)
        {
            IItem first = Minted(itemId, rarity);
            if (first.MaxStackSize > 1)
            {
                Placed(inventory, first, amount);
                return;
            }

            for (int copy = 0; copy < amount; copy++)
                if (!Placed(inventory, copy == 0 ? first : Minted(itemId, rarity), 1))
                    return;
        }

        private IItem Minted(string itemId, Rarity? rarity) =>
            rarity is { } requested
                ? Service<IItemCreationService>().CreateItem(itemId, [], requested, equipEffectChance: 0, modifierMultiplier: 1)
                : Service<IItemMinter>().MintItem(itemId);

        private bool Placed(IInventory inventory, IItem item, int amount)
        {
            if (inventory.TryAddItem(item, amount)) return true;

            Print($"[color=red]The bag would not take {amount} x {item.Id}[/color]");
            return false;
        }

        private void ExecuteInventory(string[] args)
        {
            if (args.Length < 2 || args[1].ToLowerInvariant() != "clear")
            {
                Print("inv clear");
                return;
            }

            Service<IInventory>().Clear();
            Print("Inventory cleared");
        }

        /// <summary>
        /// Drives one augment from the bag into a slot and back out. There is no socket window yet, so
        /// this is the only hand on that road; the two operations travel the bus gates the window will
        /// use, because a console reaching for the board itself would answer for a road the game does
        /// not take. Everything else here reads: the records, the copies carried, the slots open.
        /// </summary>
        private void ExecuteAugment(string[] args)
        {
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "sockets")
            {
                case "catalog": PrintAugmentCatalog(args.Length > 2 ? args[2] : string.Empty); break;
                case "bag": PrintCarriedAugments(); break;
                case "sockets": PrintSockets(); break;
                case "install" when args.Length > 3: InstallAugment(args[2], args[3]); break;
                case "extract" when args.Length > 2: ExtractAugment(args[2]); break;
                default: Print(AugmentUsage); break;
            }
        }

        /// <summary>
        /// Drives one ornament between the bag and an ability. The only hand on that road until a window
        /// grows one, and it travels the same bus gates the window will use. An ornament is named by its
        /// own id rather than by a copy id: there is one of each in the game, so the id names the thing.
        /// </summary>
        private void ExecuteOrnament(string[] args)
        {
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "list")
            {
                case "list": PrintOrnaments(); break;
                case "attach" when args.Length > 3: AttachOrnament(args[2], args[3]); break;
                case "detach" when args.Length > 2: DetachOrnament(args[2]); break;
                default: Print(OrnamentUsage); break;
            }
        }

        /// <summary>Every ornament the data declares and where it currently is. "Where" is the whole
        /// question about an ornament: it grants a socket to whatever ability wears it, and there is one
        /// of each in the game.</summary>
        private void PrintOrnaments()
        {
            var catalog = Service<IOrnamentCatalog>();
            var board = Service<IAbilitySocketBoard>();

            Print($"Ornaments: {catalog.All.Count} declared, {board.Ornaments.Count} worn");
            if (catalog.All.Count == 0)
            {
                Print("None declared: the Ornaments catalog is empty or failed to load");
                return;
            }

            PrintTable(
                ["id", "grants tier", "where", "socket holds"],
                [
                    .. catalog.All
                        .OrderBy(ornament => ornament.Tier)
                        .Select(ornament => new[]
                        {
                            ornament.Id,
                            Text(ornament.Tier),
                            WhereaboutsOf(board, ornament.Id),
                            board.Attachment(ornament.Id) is { } worn
                            && board.Find(worn.Slot.Address) is { } granted
                                ? HoldingOf(granted)
                                : "-",
                        })
                ]);
        }

        /// <summary>Which ability wears it, or which bag it waits in. An ornament in neither has not been
        /// handed out yet — the trials line is the only road that hands one over.</summary>
        private static string WhereaboutsOf(IAbilitySocketBoard board, string ornamentId)
        {
            if (board.Attachment(ornamentId) is { } worn) return $"on {worn.AbilityId}";

            return Service<IInventory>().GetTotalItemAmount(ornamentId) > 0 ? "in bag" : "not owned";
        }

        /// <summary>Puts a carried ornament onto an ability through the gate a window will use. The
        /// ornament is found in the bag by its own id, because that is what the player types.</summary>
        private void AttachOrnament(string ornamentId, string abilityId)
        {
            if (CarriedOrnament(ornamentId) is not { } item)
            {
                Print($"[color=red]attach {ornamentId}: not in the bag — item add {ornamentId} mints one (orn list)[/color]");
                return;
            }

            SendAndReport<AttachOrnamentRequest, OrnamentAttachResult>(
                new AttachOrnamentRequest(item.InstanceId, abilityId),
                result => Print(result == OrnamentAttachResult.Attached
                    ? $"attach {ornamentId} -> {abilityId}: ok"
                    : $"[color=red]attach {ornamentId} -> {abilityId}: refused — {result}[/color]"));
        }

        /// <summary>Takes an ornament off whatever wears it, through the mirror gate.</summary>
        private void DetachOrnament(string ornamentId)
        {
            SendAndReport<DetachOrnamentRequest, OrnamentDetachResult>(
                new DetachOrnamentRequest(ornamentId),
                result => Print(result == OrnamentDetachResult.Detached
                    ? $"detach {ornamentId}: ok"
                    : $"[color=red]detach {ornamentId}: refused — {result}[/color]"));
        }

        /// <summary>The bag's copy of one ornament, or nothing while it holds none.</summary>
        private static IOrnamentItem? CarriedOrnament(string ornamentId) =>
            Service<IInventory>().GetContents()
                .Select(entry => entry.Item)
                .OfType<IOrnamentItem>()
                .FirstOrDefault(item => string.Equals(item.Id, ornamentId, StringComparison.Ordinal));

        /// <summary>
        /// Every augment the data declares, so a copy can be asked for by id without opening the ability
        /// file. What is shown is the RECORD — the tier, where it may go, the figures its numbers are
        /// drawn around — which is what a slot measures. What a copy came out at is the copy's own and
        /// is shown beside the copy.
        /// </summary>
        private void PrintAugmentCatalog(string filter)
        {
            var catalog = Service<IAbilityAugmentCatalog>();
            List<AbilityAugmentData> matching =
            [
                .. catalog.All
                    .Where(augment => augment.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(augment => augment.Id, StringComparer.Ordinal)
            ];

            Print(filter.Length == 0
                ? $"Augments: {matching.Count} declared, {Math.Min(matching.Count, CatalogLimit)} shown"
                : $"Augments: {matching.Count} of {catalog.All.Count} match \"{filter}\", {Math.Min(matching.Count, CatalogLimit)} shown");

            PrintTable(
                ["id", "tier", "rarity", "goes on", "group", "declared"],
                [
                    .. matching.Take(CatalogLimit).Select(augment => new[]
                    {
                        augment.Id,
                        Text(augment.Tier),
                        augment.Rarity.ToString(),
                        BindingOf(augment),
                        augment.ExclusionGroup.Length == 0 ? "none" : augment.ExclusionGroup,
                        Numbers(augment.UpgradeProperties),
                    })
                ]);
        }

        /// <summary>
        /// The augments in the bag as copies rather than as names: two copies of one record differ in
        /// nothing but what they rolled, so an id alone cannot say which one is about to be seated. The
        /// head of the copy id printed here is what the install command takes.
        /// </summary>
        private void PrintCarriedAugments()
        {
            var inventory = Service<IInventory>();
            var catalog = Service<IAbilityAugmentCatalog>();
            IReadOnlyList<IAugmentItem> carried = CarriedAugments(inventory);

            Print($"Bag: {carried.Count} augment(s) carried, {inventory.GetAvailableCapacity()} of {inventory.InventoryCapacity} slot(s) free");
            if (carried.Count == 0)
            {
                Print("Nothing to seat: item add <augmentId> mints one (aug catalog lists the ids)");
                return;
            }

            PrintTable(
                ["copy", "id", "tier", "rarity", "rolled"],
                [
                    .. carried.Select(item => new[]
                    {
                        Head(item.InstanceId),
                        item.Id,
                        catalog.Find(item.Id) is { } record ? Text(record.Tier) : "?",
                        item.Rarity.ToString(),
                        RolledNumbers(item.Augment),
                    })
                ]);
        }

        /// <summary>The augments the bag actually holds, read off its slots — the same list a save
        /// writes down. Ordered by record so two copies of one augment stand next to each other, which
        /// is where two rolls of it are compared.</summary>
        private static IReadOnlyList<IAugmentItem> CarriedAugments(IInventory inventory) =>
        [
            .. inventory.GetContents()
                .Select(entry => entry.Item)
                .OfType<IAugmentItem>()
                .OrderBy(item => item.Id, StringComparer.Ordinal)
        ];

        /// <summary>Every slot the character has and what stands in it. The first column is the ADDRESS
        /// the commands take — a node id is not enough on its own, because a node repointed between
        /// builds can leave the slot it used to open holding an augment beside the one it opens now.
        /// A slot no node backs any more is marked: it is remove-only, and what is in it is still the
        /// player's.</summary>
        private void PrintSockets()
        {
            var board = Service<IAbilitySocketBoard>();
            IReadOnlyList<AbilitySocket> sockets =
            [
                .. board.Sockets
                    .OrderBy(socket => socket.AbilityId, StringComparer.Ordinal)
                    .ThenBy(socket => socket.Tier)
                    .ThenBy(socket => socket.Address, StringComparer.Ordinal)
            ];

            Print($"Sockets: {sockets.Count(socket => socket.IsOpen)} open, " +
                  $"{sockets.Count(socket => !socket.IsOpen)} remove-only, {sockets.Count(socket => !socket.IsEmpty)} filled");
            if (sockets.Count == 0)
            {
                Print("No node opens one yet: an ability node opens the tier-1 slot of its ability, a socket node the higher ones (passive frontier)");
                return;
            }

            PrintTable(
                ["socket (address)", "ability", "tier", "state", "holds"],
                [.. sockets.Select(socket => new[]
                {
                    socket.Address, socket.AbilityId, Text(socket.Tier), StateOf(socket), HoldingOf(socket)
                })]);
        }

        /// <summary>Whether the allocation still backs the slot, in a word.</summary>
        private static string StateOf(AbilitySocket socket) => socket.IsOpen ? "open" : "remove-only";

        /// <summary>Hands one carried copy to a slot through the gate the socket window uses. A refusal
        /// is named in words: the gate says which of them answered and carries the fitting rule's own
        /// verdict with it, so nothing here works the rule out a second time.</summary>
        private void InstallAugment(string socket, string copy)
        {
            if (!TryResolveCopy(copy, out string instanceId) || !TryResolveSocket(socket, out string address)) return;

            SendAndReport<InstallAugmentRequest, AugmentInstallResult>(
                new InstallAugmentRequest(address, instanceId),
                result => ReportInstall(address, result));
        }

        /// <summary>Takes the augment out of a slot and back into the bag through the mirror gate.</summary>
        private void ExtractAugment(string socket)
        {
            if (!TryResolveSocket(socket, out string address)) return;

            SendAndReport<ExtractAugmentRequest, AugmentExtractResult>(
                new ExtractAugmentRequest(address),
                result => ReportExtract(address, result));
        }

        /// <summary>
        /// Turns what was typed into a slot address. A full address is passed through; a node id
        /// naming exactly one slot resolves to it, which is what makes the short form usable while a
        /// node opens one slot. A node naming several is the one refusal made here — the console has no
        /// ground to choose between an augment the player is wearing and one he is only keeping.
        /// Anything else goes through untouched: an address no slot has is the gate's answer to give.
        /// </summary>
        private bool TryResolveSocket(string typed, out string address)
        {
            address = typed;
            var board = Service<IAbilitySocketBoard>();
            if (board.Find(typed) != null) return true;

            List<AbilitySocket> named = [.. board.Sockets.Where(socket => socket.SocketId == typed)];
            if (named.Count > 1)
            {
                Print($"[color=red]node \"{typed}\" carries {named.Count} slots — name the address (aug sockets)[/color]");
                return false;
            }

            if (named.Count == 1) address = named[0].Address;
            return true;
        }

        private void ReportInstall(string address, AugmentInstallResult result)
        {
            if (!result.Installed)
            {
                Print($"[color=red]install {address}: refused — {RefusalOf(result)}[/color]");
                return;
            }

            Print($"install {address}: ok");
            PrintSocket(address);
        }

        private void ReportExtract(string address, AugmentExtractResult result)
        {
            if (result != AugmentExtractResult.Extracted)
            {
                Print($"[color=red]extract {address}: refused — {ExtractRefusalOf(result)}[/color]");
                return;
            }

            Print($"extract {address}: ok");
            PrintSocket(address);
            PrintCarriedAugments();
        }

        /// <summary>The one slot an operation just touched, so its new state is on screen without
        /// listing the whole board again. An extraction that emptied a remove-only slot takes the slot
        /// with it, and there is then nothing left to print.</summary>
        private void PrintSocket(string address)
        {
            if (Service<IAbilitySocketBoard>().Find(address) is not { } socket) return;

            Print($"  {socket.Address} ({socket.AbilityId}, tier {socket.Tier}, {StateOf(socket)}): {HoldingOf(socket)}");
        }

        /// <summary>Sends one socket command and prints the answer when it comes. Void-returning because
        /// the console has nothing to hand a task to and the printed line IS the answer; the wait is
        /// kept rather than skipped, so a gate that stops answering at once still reports.</summary>
        private async void SendAndReport<TRequest, TResponse>(TRequest request, Action<TResponse> report)
            where TRequest : IRequest<TResponse>
        {
            try
            {
                report(await Service<IGameMessageBus>().SendRequest<TRequest, TResponse>(request));
            }
            catch (Exception exception)
            {
                Print($"[color=red]{exception.Message}[/color]");
            }
        }

        /// <summary>
        /// Turns what was typed into the instance id of a carried copy. A copy id is a guid, so the
        /// listings print its head and this accepts one: a head naming exactly one carried augment
        /// resolves to it. Anything else is passed on untouched — an id the bag does not know is the
        /// gate's answer to give and not the console's — and the single refusal made here is a head
        /// naming several copies, which the console has no ground to choose between.
        /// </summary>
        private bool TryResolveCopy(string typed, out string instanceId)
        {
            instanceId = typed;
            List<IAugmentItem> matching =
            [
                .. CarriedAugments(Service<IInventory>())
                    .Where(item => item.InstanceId.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
            ];

            if (matching.Count > 1)
            {
                Print($"[color=red]{matching.Count} carried copies start with \"{typed}\" — type more of the id (aug bag)[/color]");
                return false;
            }

            if (matching.Count == 1) instanceId = matching[0].InstanceId;
            return true;
        }

        /// <summary>Which gate turned the augment back, in words. The fitting rule's own reasons are
        /// not repeated in this list — they travel beside the outcome and are spelled out where they
        /// are read.</summary>
        private static string RefusalOf(AugmentInstallResult result) => result.Outcome switch
        {
            AugmentInstallOutcome.AugmentNotHeld => "the bag holds no augment under that copy id (aug bag)",
            AugmentInstallOutcome.NoSuchSocket => "no socket of that address exists (aug sockets)",
            AugmentInstallOutcome.SocketClosed => "no node backs that socket any more — it can only be emptied (aug extract <socket>)",
            AugmentInstallOutcome.SocketOccupied => "the socket already holds an augment — take it out first (aug extract <socket>)",
            AugmentInstallOutcome.DoesNotFit => FitRefusalOf(result.Fit),
            _ => result.Outcome.ToString(),
        };

        /// <summary>Why the slot does not take that augment, in the fitting rule's own terms. A verdict
        /// that never came is a record the catalog no longer declares: there was nothing to measure.</summary>
        private static string FitRefusalOf(AugmentFitResult? fit) => fit switch
        {
            AugmentFitResult.TierAboveSocket => "the augment stands above the tier the socket takes",
            AugmentFitResult.BoundToAnotherAbility => "the augment is written for another ability (aug catalog names it)",
            AugmentFitResult.NoSharedTag => "the augment shares no tag with the socket's ability",
            AugmentFitResult.ExclusionGroupTaken => "the ability already wears an augment of the same exclusion group",
            AugmentFitResult.ContradictoryDeclaration => "the record answers twice and differently: it claims every ability and names one",
            null => "no record declares that augment any more, so nothing can judge it",
            _ => fit.ToString()!,
        };

        private static string ExtractRefusalOf(AugmentExtractResult result) => result switch
        {
            AugmentExtractResult.NothingToExtract => "no socket of that address exists, or it stands empty (aug sockets)",
            AugmentExtractResult.NoBagRoom => "the bag has no room — the augment stays in the socket rather than being lost on the way",
            AugmentExtractResult.CannotBeHeld => "no record declares that augment any more — it stays in the socket",
            _ => result.ToString(),
        };

        /// <summary>Where the record says the augment may go, in the words the fitting rule reads it by.
        /// A record answering the binding question twice is shown as it stands: every slot refuses it,
        /// and the author sees why here instead of at the first install.</summary>
        private static string BindingOf(AbilityAugmentData augment)
        {
            if (augment.FitsAnyAbility)
                return augment.AbilityId.Length == 0 ? "any ability" : $"any ability AND {augment.AbilityId} — contradiction";

            if (augment.AbilityId.Length > 0) return augment.AbilityId;

            return augment.Tags.Length == 0 ? "nothing: no tag and no ability" : $"tag {string.Join("/", augment.Tags)}";
        }

        private string HoldingOf(AbilitySocket socket) =>
            socket.Augment is { } augment ? $"{augment.AugmentId}: {RolledNumbers(augment)}" : "empty";

        /// <summary>What one copy came out at, every number beside the figure it was drawn around — a
        /// roll cannot be read against nothing. A property the record has dropped since the copy was
        /// minted is shown alone: the copy keeps what it rolled either way.</summary>
        private string RolledNumbers(AugmentInstance augment)
        {
            if (augment.Values.Count == 0) return "no numbers";

            AbilityAugmentData? record = Service<IAbilityAugmentCatalog>().Find(augment.AugmentId);
            return string.Join(", ", augment.Values
                .OrderBy(rolled => rolled.Key, StringComparer.Ordinal)
                .Select(rolled => record != null && record.UpgradeProperties.TryGetValue(rolled.Key, out float declared)
                    ? $"{rolled.Key} {Rounded(rolled.Value)} (base {Rounded(declared)})"
                    : $"{rolled.Key} {Rounded(rolled.Value)}"));
        }

        /// <summary>The figures a record declares, in a fixed order so two records read side by side.</summary>
        private static string Numbers(IReadOnlyDictionary<string, float> values) =>
            values.Count == 0
                ? "none"
                : string.Join(", ", values
                    .OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(value => $"{value.Key} {Rounded(value.Value)}"));

        /// <summary>Prints the rows under their header with every column as wide as its widest cell. The
        /// console is read by eye and held against what was expected, so a listing whose columns do not
        /// line up is one that has to be read twice. Nothing at all is printed for an empty listing —
        /// the count line above it has already said so, and a header over no rows only reads as one.</summary>
        private void PrintTable(string[] header, IReadOnlyList<string[]> rows)
        {
            if (rows.Count == 0) return;

            int[] widths = new int[header.Length];
            foreach (string[] row in rows.Append(header))
                for (int column = 0; column < header.Length; column++)
                    widths[column] = Math.Max(widths[column], row[column].Length);

            Print(Padded(header, widths));
            foreach (string[] row in rows) Print(Padded(row, widths));
        }

        /// <summary>One line of a table. The last cell is left as it stands — padding a tail of numbers
        /// only drags trailing spaces across the console.</summary>
        private static string Padded(string[] cells, int[] widths) =>
            string.Join("  ", cells.Select((cell, column) => column == cells.Length - 1 ? cell : cell.PadRight(widths[column])));

        private static string Head(string instanceId) =>
            instanceId.Length <= CopyIdHeadLength ? instanceId : instanceId[..CopyIdHeadLength];

        private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

        private void ExecuteReputation(string[] args)
        {
            var relations = Service<IFactionRelationService>();
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty)
            {
                case "add" when args.Length > 3 && Enum.TryParse(args[2], true, out Fractions faction) && int.TryParse(args[3], out int delta):
                    relations.AddReputation(faction, delta, "DebugConsole");
                    Print($"{faction}: {relations.GetReputation(faction)} ({relations.GetPlayerRelation(faction)})");
                    break;
                case "set" when args.Length > 3 && Enum.TryParse(args[2], true, out Fractions faction) && Enum.TryParse(args[3], true, out RelationLevel level):
                    relations.SetPlayerRelation(faction, level);
                    Print($"{faction}: {relations.GetReputation(faction)} ({relations.GetPlayerRelation(faction)})");
                    break;
                default:
                    Print($"rep add <faction> <delta> | rep set <faction> <{string.Join("|", Enum.GetNames<RelationLevel>())}>");
                    break;
            }
        }

        private void ExecuteRaid()
        {
            bool started = Service<IRaidService>().ForceRaid();
            Print(started
                ? "Raid started"
                : "No raid: needs an alive non-fighting player and a Hatred faction with a raid-capable spawn site (rep set <faction> hatred)");
        }

        private void ExecuteKill()
        {
            Service<IPlayerAccessor>().Player?.Kill(isDebug: true); // console death frames nobody
            Print("Player killed");
        }

        private void ExecuteRevive()
        {
            if (Service<IPlayerAccessor>().Player is not Player.Player { Lifecycle: { } lifecycle })
            {
                Print("Player is not lying dead");
                return;
            }

            lifecycle.ForceRevive();
            Print("Player revived");
        }

        private void ExecuteTime(string[] args)
        {
            var clock = Service<IWorldClock>();
            switch (args.Length > 1 ? args[1].ToLowerInvariant() : "show")
            {
                case "set" when args.Length > 2 && int.TryParse(args[2], out int hour):
                    int minute = args.Length > 3 && int.TryParse(args[3], out int parsedMinute) ? parsedMinute : 0;
                    clock.RestoreState(clock.Day, (Mathf.Clamp(hour, 0, 23) * 60) + Mathf.Clamp(minute, 0, 59));
                    PrintClock(clock);
                    break;
                case "scale" when args.Length > 2 && TryParseFloat(args[2], out float scale) && scale > 0:
                    Engine.TimeScale = scale;
                    PrintClock(clock);
                    break;
                case "show":
                    PrintClock(clock);
                    break;
                default:
                    Print("time | time set <hour> [minute] | time scale <x>");
                    break;
            }
        }

        private void PrintClock(IWorldClock clock) =>
            Print($"Day {clock.Day}, {clock.Hour:00}:{clock.Minute:00} ({clock.Phase}), time scale {Engine.TimeScale}");

        private void ExecuteSpawn(string[] args)
        {
            var provider = Service<INpcProvider>();
            if (args.Length < 2 || args[1].ToLowerInvariant() == "list")
            {
                Print(string.Join(", ", provider.KnownNpcIds.OrderBy(id => id)));
                return;
            }

            string npcId = args[1];
            if (!provider.KnownNpcIds.Contains(npcId))
            {
                Print($"Unknown npc: {npcId} | spawn list");
                return;
            }

            if (Service<IPlayerAccessor>().Player is not Node2D playerNode)
            {
                Print("No player in the world");
                return;
            }

            var overrides = new NpcDefinitionOverrides
            {
                Level = args.Length > 2 && int.TryParse(args[2], out int level) ? level : null,
                Rarity = args.Length > 3 && Enum.TryParse(args[3], true, out Rarity rarity) ? rarity : null,
            };
            var definition = provider.CreateDefinition(npcId, overrides);
            var npc = Service<INpcWorldSpawner>().Spawn(definition, playerNode.GlobalPosition + new Vector2(250, 0));
            if (npc == null)
            {
                Print("Spawn failed: no world available");
                return;
            }

            // Same bookkeeping as raids: outside the population cap, released by final death.
            Service<INpcPopulationService>().ReserveOutsideLimit();
            Print($"Spawned {npcId} (lvl {definition.Level}, {definition.Rarity})");
        }

        private void ExecuteTeleport(string[] args)
        {
            if (Service<IPlayerAccessor>().Player is not Node2D playerNode)
            {
                Print("No player in the world");
                return;
            }

            if (args.Length > 2 && TryParseFloat(args[1], out float x) && TryParseFloat(args[2], out float y))
            {
                // In battle the player's position IS the arena spot — moving it would corrupt the fight.
                if (Service<IPlayerAccessor>().Player!.IsFighting)
                {
                    Print("Not while fighting");
                    return;
                }

                playerNode.GlobalPosition = new Vector2(x, y);
            }

            Print($"Position: {playerNode.GlobalPosition}");
        }

        private void ExecuteSave(string[] args)
        {
            var saves = Service<ISaveGameService>();
            int slot = SlotOf(args);
            if (!saves.CanSave)
            {
                Print("Cannot save now (battle, raid or dead)");
                return;
            }

            saves.SaveToSlot(slot);
            Print($"Saved to slot {slot}");
        }

        private void ExecuteLoad(string[] args)
        {
            int slot = SlotOf(args);
            Print(Service<ISaveGameService>().RequestLoad(slot) ? $"Loading slot {slot}…" : $"Load rejected: slot {slot} empty or unavailable");
        }

        private static int SlotOf(string[] args) => args.Length > 1 && int.TryParse(args[1], out int slot) ? slot : 0;

        private void PrintHelp()
        {
            Print("[b]Player:[/b] restore [hp|mana|barrier|all] | heal <amount> | cd [reset] | kill | revive | tp [x y]");
            Print("[b]Stats:[/b] stats list | stats add <param> <amount> <flat|increase|multiplicative> | stats remove <param> | stats clear");
            Print("[b]Effects:[/b] effect list | effect <stun|freeze> [duration] | effect clear");
            Print("[b]Abilities:[/b] ability list | ability learn <abilityId> | ability show <abilityId>");
            Print("[b]Items:[/b] item add <itemId> [amount] [rarity] | inv clear");
            Print($"[b]Augments:[/b] {AugmentUsage}");
            Print($"[b]Ornaments:[/b] {OrnamentUsage}");
            Print("[b]Masteries:[/b] influence [exp <n>] | martial [exp <n>] | craft [exp <n>]");
            Print($"[b]Passive tree:[/b] {PassiveTreeUsage}");
            Print("[b]Reputation:[/b] rep add <faction> <delta> | rep set <faction> <level> | raid");
            Print("[b]World:[/b] time [set <hour> [minute]|scale <x>] | spawn [list|<npcId> [level] [rarity]]");
            Print("[b]Narrative:[/b] quest list | quest <accept|decline|abandon|fail|turnin> <questId> | fact set <key> [amount] | fact dump [prefix]");
            Print("[b]Saves:[/b] save [slot] | load [slot]");
        }

        private static bool TryParseFloat(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool Run(Action action)
        {
            action();
            return true;
        }

        private static T Service<T>() => Services.GameServiceProvider.Instance.GetService<T>();

        private void Print(string line) => _output!.AppendText(line + "\n");
    }
}
