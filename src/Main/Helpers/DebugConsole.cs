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
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Narrative;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Save;
    using Core.Services;
    using Godot;

    /// <summary>
    /// Debug-build console (autoload, F12): drive quests, facts, influence, reputation, player
    /// vitals/stats, items, world time and NPC spawns without UI. The one place UI is built in
    /// code on purpose — it must not depend on anything it tests. Services resolve lazily per
    /// command so the autoload never races the provider bootstrap.
    /// </summary>
    public partial class DebugConsole : CanvasLayer
    {
        private const string ModifierSource = "debugConsole";

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
            _input = new LineEdit { PlaceholderText = "help" };
            _input.TextSubmitted += OnSubmitted;
            _input.GuiInput += OnInputKey;
            layout.AddChild(_output);
            layout.AddChild(_input);
            _root.AddChild(layout);
            AddChild(_root);
        }

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
                case "craft": ExecuteCraft(args); break;
                case "rep": ExecuteReputation(args); break;
                case "item": ExecuteItem(args); break;
                case "stats": ExecutePlayerStats(args); break;
                case "heal": ExecuteHeal(args); break;
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
                default:
                    Print("ability list | ability learn <abilityId>");
                    break;
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

        private void ExecuteMartial(string[] args)
        {
            var mastery = Service<Core.Battle.IMartialArtMastery>();
            if (args.Length > 2 && args[1].ToLowerInvariant() == "exp" && int.TryParse(args[2], out int exp))
                mastery.AddExperience(exp);
            Print($"Martial Art: level {mastery.CurrentLevel}/{mastery.MaximumLevel}, exp {mastery.CurrentExperience}, to next {mastery.ExpToNextLevelRemain()}");
        }

        private void ExecuteCraft(string[] args)
        {
            var mastery = Service<ICraftingMastery>();
            if (args.Length > 2 && args[1].ToLowerInvariant() == "exp" && int.TryParse(args[2], out int exp))
                mastery.AddExperience(exp);
            Print($"Crafting: level {mastery.CurrentLevel}/{mastery.MaximumLevel}, exp {mastery.CurrentExperience}, to next {mastery.ExpToNextLevelRemain()}");
        }

        // item add <id> [amount] [rarity]: with a rarity the equip goes through the loot pipeline
        // (rarity gives the affix slot split), without one it is a plain blueprint mint.
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

            var inventory = Service<Core.Inventory.IInventory>();
            var item = rarity is { } requested
                ? Service<IItemCreationService>().CreateItem(args[2], [], requested, equipEffectChance: 0, modifierMultiplier: 1)
                : Service<Core.Items.IItemMinter>().MintItem(args[2]);
            inventory.TryAddItem(item, amount);
            Print($"{args[2]}{(rarity != null ? $" ({rarity})" : string.Empty)}: now {inventory.GetTotalItemAmount(args[2])}");
        }

        private void ExecuteInventory(string[] args)
        {
            if (args.Length < 2 || args[1].ToLowerInvariant() != "clear")
            {
                Print("inv clear");
                return;
            }

            Service<Core.Inventory.IInventory>().Clear();
            Print("Inventory cleared");
        }

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
            Service<IPlayerAccessor>().Player?.Kill();
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
            Print("[b]Abilities:[/b] ability list | ability learn <abilityId>");
            Print("[b]Items:[/b] item add <itemId> [amount] [rarity] | inv clear");
            Print("[b]Masteries:[/b] influence [exp <n>] | martial [exp <n>] | craft [exp <n>]");
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
