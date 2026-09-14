namespace LastBreath.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Save;
    using Core.Services;
    using Core.Views.UI;
    using Core.World.Containers;
    using Core.World.Interactions;
    using Core.World.Locations;
    using Godot;
    using Newtonsoft.Json.Linq;
    using World.Containers;
    using World.Interactions;
    using World.Interactions.UI;
    using World.Locations;

    public partial class InteractionTest : Node
    {
        private int _checks;
        private void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
            _checks++;
        }
        private async Task Ticks(int count = 12)
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

        private static T? FindWindow<T>(Node root) where T : Node
        {
            if (root is T match) return match;
            for (int i = 0; i < root.GetChildCount(); i++)
                if (FindWindow<T>(root.GetChild(i)) is { } found) return found;
            return null;
        }

        public override async void _Ready()
        {
            Main? main = null;
            var provider = Services.GameServiceProvider.Instance;
            try
            {
                main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
                main.Set("_addTestItems", false);
                AddChild(main);
                await Ticks();
                var travel = provider.GetService<LocationCoordinator>();
                var world = travel.Loaded("MainWorld")!;
                var player = (Node2D)provider.GetService<IPlayerAccessor>().Player!;
                var interaction = provider.GetService<InteractionService>();
                var bus = provider.GetService<IGameMessageBus>();
                var bag = provider.GetService<IInventory>();
                var ui = provider.GetService<IUiElementsManager>();
                var controller = player.GetNode<PlayerInteractionController>("PlayerInteractionController");

                player.GlobalPosition = world.Endpoint("VillageSource").GlobalPosition;
                await Ticks();
                Check(interaction.Selected?.Handle.ObjectId == "endpoint/VillageSource", "Area overlap selects the exit.");
                await Press(Key.E);
                Check(travel.ActiveLocationId == "SourceOfPowerNearVillage", "Shared E travels from the root window.");
                var side = travel.Loaded("SourceOfPowerNearVillage")!;
                var chest = side.GetNode<ChestComponent>("StarterChest");
                player.GlobalPosition = chest.GlobalPosition + new Vector2(0, 120);
                await Ticks();
                Check(interaction.Selected == chest.Target, "The chest is selected after entering its discovery area.");
                Check(controller.CandidateCount < 5, "Discovery retains a local candidate set.");
                int updates = controller.EvaluationCount;
                await Ticks(30);
                Check(controller.EvaluationCount - updates < 12, "Selection does not run on every physics tick.");
                Check(!chest.Contents.Initialized, "Hint polling does not generate chest contents.");

                var wall = new StaticBody2D { Position = chest.Position + new Vector2(0, 60) };
                wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(200, 15) } });
                side.AddChild(wall);
                await Ticks();
                Check(interaction.Selected != chest.Target, "A wall blocks the chest hint.");
                Check(!(await bus.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(chest.Target.Handle, InteractionActions.Open))).Success,
                    "Direct commands cannot bypass the wall.");
                wall.QueueFree();
                await Ticks();
                var textField = new LineEdit();
                AddChild(textField);
                textField.GrabFocus();
                await Ticks();
                Check(interaction.Selected == null, "Text focus suppresses world interactions.");
                textField.QueueFree();
                await Ticks();

                var probe = new InteractionProbe { Position = player.Position + new Vector2(0, 10), Available = false };
                var probeTarget = GD.Load<PackedScene>("res://World/Interactions/InteractionTarget.tscn").Instantiate<InteractionTarget>();
                probeTarget.ObjectId = "test/probe";
                probe.AddChild(probeTarget);
                side.AddChild(probe);
                await Ticks();
                Check(interaction.Selected == chest.Target, "A closer disabled target does not steal a usable action.");
                probe.Available = true;
                await Ticks();
                Check(interaction.Selected == probeTarget, "A closer enabled target wins.");
                await Press(Key.E);
                Check(probe.Executed == 0 && interaction.SessionTarget == probeTarget, "E opens multiple actions without executing one.");
                probe.HasChoice = false;
                await Ticks();
                Check(probe.Executed == 0, "Availability changes do not auto-execute a remaining dangerous action.");
                Check((await bus.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(probeTarget.Handle, "danger"))).Success
                    && probe.Executed == 1, "Explicit selection executes the intended action once.");
                await Ticks();
                await Press(Key.E);
                Check(probe.Executed == 1 && interaction.SessionTarget == probeTarget, "A lone dangerous action still requires selection.");
                interaction.CancelSession();
                probe.QueueFree();
                await Ticks();
                await Press(Key.E);
                Check(interaction.SessionTarget == chest.Target && chest.Contents.Initialized, "Opening uses the same target and starts a session.");
                Check(ui.HasMovementBlockingWindow, "The container window blocks player walking.");
                Check(chest.Contents.Slots.Count == 5 && chest.Contents.Slots.All(x => x.Amount == 1 && x.Item!.Rarity == Rarity.Uncommon),
                    "Starter contents match the authored uncommon equipment.");
                Check(chest.Contents.Slots.Any(x => x.Item!.Id == "Boots_Stone_Tread"), "Starter boots resolve to the existing item.");
                var initial = chest.CaptureLocationState();
                var window = FindWindow<ChestContentsWindow>(main)!;
                Check(window != null, "Container UI is instantiated from its scene.");
                var row = window!.GetNode<Control>("Frame/Content/Rows").GetChild<Control>(0);
                var click = row.GetGlobalRect().GetCenter();
                bag.Clear();
                GetTree().Root.PushInput(new InputEventMouseButton { Position = click, GlobalPosition = click, ButtonIndex = MouseButton.Right, Pressed = true }, true);
                await Ticks();
                GetTree().Root.PushInput(new InputEventMouseButton { Position = click, GlobalPosition = click, ButtonIndex = MouseButton.Right, Pressed = false }, true);
                Check(chest.Contents.Slots[0].Amount == 0 && bag.GetContents().Sum(x => x.Amount) == 1, "Right-click transfers the clicked item through root UI input.");
                // Restore the original fixture for the independent partial-capacity scenario.
                bag.Clear();
                chest.RestoreLocationState(initial);
                interaction.CancelSession();
                await Ticks();
                await Press(Key.E);
                Check(JToken.DeepEquals(initial, chest.CaptureLocationState()), "Reopening preserves equipment rolls.");

                // Fill all bag slots while leaving room for exactly three units in an existing stack.
                bag.Clear();
                var resource = provider.GetService<IItemDataProvider>().GetAllResources().First(x => x.MaxStackSize > 10);
                Check(bag.TryAddItem(resource, bag.InventoryCapacity * resource.MaxStackSize), "Fill the real bag.");
                bag.RemoveItemById(resource.Id, 3);
                var saved = (JObject)chest.CaptureLocationState();
                ((JArray)saved["Slots"]!).Add(JObject.FromObject(new
                {
                    Id = "resource", Amount = 10,
                    Item = new Core.Data.SaveData.InventoryItemSaveData { ResourceId = resource.Id, Amount = 10 }
                }));
                chest.RestoreLocationState(saved);
                int observedRemaining = -1;
                void OnAmount(string id, int amount)
                {
                    if (id == resource.Id) observedRemaining = chest.Contents.Slots.Last().Amount;
                }
                bag.ItemAmountChanges += OnAmount;
                await Press(Key.R);
                bag.ItemAmountChanges -= OnAmount;
                Check(observedRemaining == 7, "Inventory callbacks observe the committed chest remainder.");
                Check(chest.Contents.Slots.Take(5).All(x => x.Amount == 1) && chest.Contents.Slots.Last().Amount == 7,
                    "Take All continues after refused equipment and partially transfers the later stack.");
                Check(chest.Contents.RemoveAtMinutes == null, "A nonempty chest has no disappearance deadline.");
                var pending = bus.SendRequest<ContainerTransferRequest, InteractionResult>(new(chest.Target.Handle));
                interaction.CancelSession();
                Check(!(await pending).Success, "Closing a session cancels its queued transfer.");
                await Ticks();
                await Press(Key.E);
                var barrier = new StaticBody2D { Position = chest.Position + new Vector2(0, 60) };
                barrier.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(200, 15) } });
                side.AddChild(barrier);
                await Ticks();
                Check(interaction.SessionTarget == null, "A new obstruction closes an existing container session.");
                barrier.QueueFree();
                await Ticks();

                var remaining = chest.CaptureLocationState();
                var captured = provider.GetService<ISaveManager>().Capture(new SaveMetadata());
                var file = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveFile>(Newtonsoft.Json.JsonConvert.SerializeObject(captured))!;
                interaction.CancelSession();
                main.QueueFree();
                await Ticks(2);
                provider.GetService<Core.Services.INpcPopulationService>().Reset();
                var saves = provider.GetService<ISaveGameService>();
                typeof(SaveGameService).GetField("_pendingLoad", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(saves, file);
                main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
                main.Set("_addTestItems", false);
                AddChild(main);
                await Ticks();
                world = travel.Loaded("MainWorld")!;
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                player = (Node2D)provider.GetService<IPlayerAccessor>().Player!;
                chest = side.GetNode<ChestComponent>("StarterChest");
                Check(JToken.DeepEquals(remaining, chest.CaptureLocationState()), "SaveDirector restores exact chest contents after a full Main reload.");
                Check(!saves.HasPendingLoad && interaction.SessionTarget == null, "Loading consumes the file without restoring a stale UI session.");
                var oldHandle = chest.Target.Handle;
                interaction.CancelSession();
                await Ticks();
                player.GlobalPosition = side.Endpoint("Entrance").GlobalPosition;
                Check(await travel.TravelAsync(new("SourceOfPowerNearVillage", "Entrance")) == TravelResult.Completed, "Leave the side location.");
                player.GlobalPosition = world.Endpoint("VillageSource").GlobalPosition;
                Check(await travel.TravelAsync(new("MainWorld", "VillageSource")) == TravelResult.Completed, "Revisit the chest.");
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                chest = side.GetNode<ChestComponent>("StarterChest");
                Check(JToken.DeepEquals(remaining, chest.CaptureLocationState()), "Unloading preserves exact slot contents.");
                Check(!(await bus.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(oldHandle, InteractionActions.Open))).Success,
                    "A command for an unloaded binding cannot target its replacement.");
                player.GlobalPosition = chest.GlobalPosition + new Vector2(0, 120);
                await Ticks();
                await Press(Key.E);
                bag.Clear();
                await Press(Key.R);
                Check(chest.Contents.Empty && chest.Contents.RemoveAtMinutes != null, "Emptying starts the deadline.");
                Check(bag.GetContents().Sum(x => x.Amount) == 12, "Remaining five equipment pieces and seven resources transfer exactly once.");
                interaction.CancelSession();
                player.GlobalPosition = side.Endpoint("Entrance").GlobalPosition;
                await travel.TravelAsync(new("SourceOfPowerNearVillage", "Entrance"));
                provider.GetService<IWorldClock>().RestoreTime(provider.GetService<IWorldClock>().TotalMinutes + 6);
                player.GlobalPosition = world.Endpoint("VillageSource").GlobalPosition;
                await travel.TravelAsync(new("MainWorld", "VillageSource"));
                await Ticks();
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                Check(side.GetNodeOrNull("StarterChest") == null, "An expired empty chest remains permanently removed.");
                var snapshot = new LocationStateAdapter(provider).Capture(side, provider.GetService<IWorldClock>().TotalMinutes);
                Check(snapshot.State["objects"]!["starter_chest"]!.Type == JTokenType.Null, "Removal is stored as a location tombstone.");

                GD.Print($"INTERACTION_TEST_PASS: {_checks} checks");
            }
            catch (Exception error)
            {
                GD.PushError($"INTERACTION_TEST_FAIL: {error}");
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
            provider.GetService<ISlotLender>().AttachSlots(slots);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Ticks(2);
            GetTree().Quit();
        }
    }
}
