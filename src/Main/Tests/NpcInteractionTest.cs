namespace LastBreath.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Battle.Source;
    using Core.Entity;
    using Core.Events;
    using Core.MessageBus;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Services;
    using Core.World.Interactions;
    using Core.World.Spaces;
    using Godot;
    using Npc;
    using World.Interactions;
    using World.Interactions.UI;
    using World.Locations;

    public partial class NpcInteractionTest : Node
    {
        // Player body center level with the NPC point on its left, within reach; the wall stands in the gap between the NPC body and the player's capsule.
        private static readonly Vector2 s_playerBodyFromNpc = new(-140, 92);
        private static readonly Vector2 s_wallFromNpc = new(-52, 92);
        private static readonly Vector2 s_wallSize = new(12, 200);
        private static readonly Vector2 s_playerBodyFromStaticTalker = new(0, 100);
        private int _checks;
        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }
        private static void PlaceBody(Node2D player, Vector2 bodyCenter) =>
            player.GlobalPosition += bodyCenter - ((IInteractionActor)player).InteractionOrigin;
        private async Task Ticks(int count = 14)
        {
            for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        private async Task Press(Key key)
        {
            GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = key, Pressed = true }, true);
            await Ticks();
            GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = key, Pressed = false }, true);
        }
        private static T? Find<T>(Node node) where T : Node
        {
            if (node is T result) return result;
            for (int i = 0; i < node.GetChildCount(); i++)
                if (Find<T>(node.GetChild(i)) is { } found) return found;
            return null;
        }

        public override async void _Ready()
        {
            Main? main = null;
            var provider = Services.GameServiceProvider.Instance;
            try
            {
                main = Main.Initialize().Instantiate<Main>();
                main.Set("_addTestItems", false);
                AddChild(main);
                await Ticks();
                var location = provider.GetService<LocationCoordinator>().Loaded("MainWorld")!;
                var player = (Node2D)provider.GetService<IPlayerAccessor>().Player!;
                var interaction = provider.GetService<InteractionService>();
                var messages = provider.GetService<IGameMessageBus>();
                var dialogue = provider.GetService<IDialogueService>();
                var facts = provider.GetService<IWorldFactsService>();
                var events = provider.GetService<IGameEventBus>();
                var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
                npc.Position = new Vector2(30000, 30000);
                npc.InjectServices(provider);
                location.AddChild(npc);
                var definition = provider.GetService<INpcProvider>().CreateDefinition("Npc_Human_Merchant");
                npc.ApplyDefinition(definition with { World = null }, provider);
                npc.CanMove = false;
                var target = npc.GetNode<NpcInteractionTarget>("InteractionTarget");
                PlaceBody(player, npc.GlobalPosition + s_playerBodyFromNpc);
                await Ticks();
                Check(interaction.Selected == target, "A nearby NPC is selected through the shared detector.");
                Check(target.Handle.ObjectId == "npc/" + npc.InstanceId, "The live instance owns target identity.");
                Check(target.ReadActions().Count(x => x.Enabled) == 2, "A neutral speaking NPC offers talk and attack.");
                Check(!facts.IsSet(FactKeys.NpcTalked(npc.Id)), "Polling never starts a conversation.");
                typeof(BaseNpc).GetMethod("OnBodyEnter", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(npc, [player]);
                Check(!npc.IsFighting, "Contact with a neutral NPC does not declare war.");
                await Press(Key.E);
                Check(interaction.SessionTarget == target && !dialogue.IsActive && !npc.IsFighting, "E opens the menu without starting talk or battle.");
                var menu = Find<InteractionMenuWindow>(main)!;
                Check(menu.GetNode<VBoxContainer>("Frame/Content/Rows").GetChildCount() == 2, "The actual menu renders both rows.");
                menu.GetNode<VBoxContainer>("Frame/Content/Rows").GetChild<Button>(0).EmitSignal(Button.SignalName.Pressed);
                await Ticks();
                Check(dialogue.IsActive && dialogue.Current?.NpcId == npc.Id, "The talk row starts the existing dialogue.");
                Check(interaction.SessionTarget == null && Find<UI.DialogueWindow>(main) != null, "Dialogue replaces the menu.");
                npc.GlobalPosition += new Vector2(1000, 0);
                await Ticks();
                Check(!dialogue.IsActive && Find<UI.DialogueWindow>(main) == null, "Walking away ends the target-bound conversation.");
                npc.GlobalPosition -= new Vector2(1000, 0);
                await Ticks();
                var click = target.GetGlobalTransformWithCanvas().Origin;
                GetTree().Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = click, GlobalPosition = click }, true);
                GetTree().Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = click, GlobalPosition = click }, true);
                await Ticks();
                Check(!dialogue.IsActive && !npc.IsFighting, "Mouse clicks no longer start NPC dialogue.");
                await Press(Key.E);
                var wall = new StaticBody2D { Position = npc.Position + s_wallFromNpc };
                wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = s_wallSize } });
                location.AddChild(wall);
                await Ticks();
                Check(interaction.Selected == null && interaction.SessionTarget == null, "An obstruction closes the NPC menu.");
                Check(!(await messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(target.Handle, InteractionActions.Attack))).Success,
                    "A direct attack request cannot cross a wall.");
                wall.QueueFree();
                await Ticks();
                await Press(Key.E);
                npc.IsFighting = true;
                await Ticks();
                Check(interaction.SessionTarget == null && interaction.Selected == null, "A new NPC battle invalidates the menu.");
                npc.IsFighting = false;
                typeof(BaseNpc).GetProperty(nameof(BaseNpc.CanTalk))!.SetValue(npc, false);
                npc.CanMove = false;
                await Ticks();
                Check(target.ReadActions().Count == 1 && target.ReadActions()[0].ExplicitChoice, "A non-speaking creature retains an explicit attack.");
                await Press(Key.E);
                Check(interaction.SessionTarget == target && !npc.IsFighting, "A lone attack still requires a menu selection.");
                interaction.CancelSession();
                await Ticks();
                typeof(BaseNpc).GetProperty(nameof(BaseNpc.CanTalk))!.SetValue(npc, true);
                npc.CanMove = false;
                var talkSource = target.GetNode<DialogueActor>("DialogueActor");
                talkSource.Set("_npcId", "Missing_Dialogue");
                await Ticks();
                Check(target.ReadActions().Any(x => x.Id == InteractionActions.Talk && !x.Enabled), "A missing dialogue disables only Talk.");
                await Press(Key.E);
                Check(interaction.SessionTarget == target && !npc.IsFighting, "Missing dialogue never turns E into an automatic attack.");
                talkSource.Set("_npcId", "");
                await Ticks();
                Check(target.ReadActions().Count(x => x.Enabled) == 2 && !npc.IsFighting, "The existing menu follows changed availability without executing it.");
                interaction.CancelSession();
                await Ticks();
                var originalHandle = target.Handle;
                var placement = new ParticipantPlacement(npc);
                var foreign = new SubViewport { World2D = new World2D() };
                AddChild(foreign);
                npc.GetParent().RemoveChild(npc);
                foreign.AddChild(npc);
                await Ticks();
                Check(!(await messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(originalHandle, InteractionActions.Talk))).Success,
                    "A detached NPC cannot receive its old command.");
                placement.Restore();
                foreign.QueueFree();
                await Ticks();
                Check(interaction.Selected == target && target.Handle != originalHandle, "Return to exploration renews registration.");
                Check(!(await messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(originalHandle, InteractionActions.Attack))).Success,
                    "A previous binding cannot attack the returned NPC.");

                var squad = new EntityGroup(4);
                squad.TryAddToGroup(npc);
                BaseNpc Squadmate(Node2D root, Vector2 position)
                {
                    var member = BaseNpc.Initialize().Instantiate<BaseNpc>();
                    member.Position = position;
                    member.InjectServices(provider);
                    root.AddChild(member);
                    member.ApplyDefinition(definition with { World = null }, provider);
                    member.CanMove = false;
                    squad.TryAddToGroup(member);
                    return member;
                }
                var teammate = Squadmate(location, npc.Position + new Vector2(800, 0));
                var busy = Squadmate(location, npc.Position + new Vector2(1600, 0));
                busy.IsFighting = true;
                var squadSpace = new SubViewport { World2D = new World2D() };
                AddChild(squadSpace);
                var squadRoot = new Node2D();
                squadSpace.AddChild(squadRoot);
                var remote = Squadmate(squadRoot, npc.Position);
                int starts = 0;
                BattleInitializedEvent? startedBattle = null;
                Vector2? noisePosition = null;
                ulong noiseSpace = 0;
                void OnStart(BattleInitializedEvent e) { starts++; startedBattle = e; }
                void OnNoise(WorldStimulusEvent e) { noisePosition = e.Stimulus.Position; noiseSpace = e.Stimulus.SpaceId; }
                events.Subscribe<BattleInitializedEvent>(OnStart);
                events.Subscribe<WorldStimulusEvent>(OnNoise);
                var battlePosition = npc.GlobalPosition;
                var battleSpace = NativeSpatialQuery.Instance.GetSpace(npc);
                await Press(Key.E);
                Check(!npc.IsFighting, "Opening the NPC menu still does not initiate combat.");
                var attack = await messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(target.Handle, InteractionActions.Attack));
                await Ticks();
                Check(attack.Success && starts == 1 && npc.IsFighting, "An explicit attack starts one real battle with a neutral NPC.");
                Check(startedBattle!.Entities.Count == 2 && startedBattle.Entities.Contains(teammate), "Explicit attack admits free local squadmates.");
                Check(!startedBattle.Entities.Contains(busy) && busy.IsFighting, "A squadmate in another battle remains there.");
                Check(!startedBattle.Entities.Contains(remote) && !remote.IsFighting, "Foreign squadmates are not frozen by the group attack.");
                Check(noiseSpace == battleSpace && noisePosition == battlePosition, "Initial battle noise retains the exploration origin.");
                Check(provider.GetService<BattleSiteRegistry>().Current?.SpaceId == battleSpace, "The existing battle marker remains in the origin.");
                Check(!SpatialAccess.SharesSpace(location, npc) && interaction.SessionTarget == null, "The arena owns fighters and clears exploration interaction.");
                var field = typeof(Main).GetField("_activeContext", BindingFlags.NonPublic | BindingFlags.Instance)!;
                ((BattleContext)field.GetValue(main)!).Abort();
                for (int i = 0; field.GetValue(main) != null && i < 180; i++) await Ticks(1);
                await Ticks();
                Check(!npc.IsFighting && SpatialAccess.SharesSpace(location, npc), "Battle teardown returns the NPC to exploration.");
                Check(interaction.Selected == target && target.ReadActions().Any(x => x.Id == InteractionActions.Talk), "The returned NPC can be selected and spoken to again.");
                Check(!teammate.IsFighting && SpatialAccess.SharesSpace(location, teammate), "Admitted squadmates return through normal battle teardown.");
                squadSpace.QueueFree();
                busy.IsFighting = false;
                events.Unsubscribe<BattleInitializedEvent>(OnStart);
                events.Unsubscribe<WorldStimulusEvent>(OnNoise);
                await Press(Key.E);
                npc.CurrentHealth = 0;
                await Ticks();
                Check(interaction.Selected == null && interaction.SessionTarget == null, "A dead NPC immediately loses interaction eligibility.");
                var staticOwner = new Node2D { Position = player.Position + new Vector2(600, 0) };
                var staticTarget = GD.Load<PackedScene>("res://World/Interactions/InteractionTarget.tscn").Instantiate<InteractionTarget>();
                staticTarget.ObjectId = "static_talker";
                var staticTalk = new DialogueActor();
                staticTalk.Set("_npcId", "Npc_Human_Merchant");
                staticTarget.AddChild(staticTalk);
                staticOwner.AddChild(staticTarget);
                location.AddChild(staticOwner);
                PlaceBody(player, staticOwner.GlobalPosition + s_playerBodyFromStaticTalker);
                await Ticks();
                Check(interaction.Selected == staticTarget && staticTarget.ReadActions().Count == 1, "An authored static talker needs no combat body.");
                await Press(Key.E);
                Check(dialogue.IsActive && dialogue.Current?.NpcId == "Npc_Human_Merchant", "A static talker uses the authored dialogue override.");
                staticOwner.QueueFree();
                await Ticks();
                Check(!dialogue.IsActive, "Removing a static talker ends its conversation.");
                GD.Print($"NPC_INTERACTION_TEST_PASS: {_checks} checks");
            }
            catch (Exception error)
            {
                GD.PushError($"NPC_INTERACTION_TEST_FAIL: {error}");
                GetTree().Quit(1);
                return;
            }
            finally
            {
                if (main != null && GodotObject.IsInstanceValid(main)) main.QueueFree();
            }
            await Ticks(2);
            var slots = new GridContainer();
            AddChild(slots);
            provider.GetService<Core.Inventory.ISlotLender>().AttachSlots(slots);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Ticks(2);
            GetTree().Quit();
        }
    }
}
