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
    using Core.Localization;
    using Core.MessageBus;
    using Core.Save;
    using Core.Services;
    using Core.Views.UI;
    using Core.World.Containers;
    using Core.World.Interactions;
    using Core.World.Locations;
    using Crafting.Source.UIElements;
    using Godot;
    using Inventory;
    using Newtonsoft.Json.Linq;
    using UI;
    using World.Containers;
    using World.Interactions;
    using World.Interactions.UI;
    using World.Locations;

    public partial class InteractionTest : Node
    {
        private const string CellsPath = "Frame/Content/Rows/Cells";
        private const string TooltipTitlePath = "Panel/Body/Margin/Layout/Header/HeaderInfo/Title";
        // Longer than the hover tooltip's opening delay.
        private const double TooltipWaitSeconds = 0.6;
        // Player body center below the chest point, within reach; walls stand in the gap between the chest body and the player's capsule.
        private static readonly Vector2 s_playerBodyFromChest = new(0, 140);
        private static readonly Vector2 s_wallFromChest = new(0, 45);
        private static readonly Vector2 s_wallSize = new(200, 15);
        private int _checks;
        private void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
            _checks++;
        }
        private static void PlaceBody(Node2D player, Vector2 bodyCenter) =>
            player.GlobalPosition += bodyCenter - ((IInteractionActor)player).InteractionOrigin;
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

        /// <summary>Points the mouse at the control's center and waits past the hover tooltip's delay.</summary>
        private async Task Hover(Control target)
        {
            var point = target.GetGlobalRect().GetCenter();
            GetTree().Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            await ToSignal(GetTree().CreateTimer(TooltipWaitSeconds), SceneTreeTimer.SignalName.Timeout);
            await Ticks(1);
        }

        /// <summary>Presses and releases the key that pins and unpins a hover tooltip.</summary>
        private async Task PressPin()
        {
            GetTree().Root.PushInput(new InputEventKey { Keycode = Key.Alt, Pressed = true }, true);
            await Ticks(1);
            GetTree().Root.PushInput(new InputEventKey { Keycode = Key.Alt, Pressed = false }, true);
            await Ticks(1);
        }

        private async Task RightClick(Control target, bool ctrl = false)
        {
            var click = target.GetGlobalRect().GetCenter();
            GetTree().Root.PushInput(new InputEventMouseButton
            {
                Position = click, GlobalPosition = click, ButtonIndex = MouseButton.Right, Pressed = true, CtrlPressed = ctrl
            }, true);
            await Ticks();
            GetTree().Root.PushInput(new InputEventMouseButton
            {
                Position = click, GlobalPosition = click, ButtonIndex = MouseButton.Right, Pressed = false, CtrlPressed = ctrl
            }, true);
        }

        private static GridContainer CellsOf(Node main) => FindWindow<ChestContentsWindow>(main)!.GetNode<GridContainer>(CellsPath);

        /// <summary>Every slot has a cell, cell i shows slot i's item and amount while the slot holds one, and every other cell is empty.</summary>
        private static bool ShowsSlots(GridContainer cells, ChestComponent chest) =>
            cells.GetChildCount() >= chest.Contents.Slots.Count
            && cells.GetChildren().Cast<InventorySlot>().Select((cell, index) => (Cell: cell, Slot: chest.Contents.Slots.ElementAtOrDefault(index)))
                .All(x => x.Slot is { Amount: > 0, Item: { } item }
                    ? x.Cell.CurrentItem?.InstanceId == item.InstanceId && x.Cell.Quantity == x.Slot.Amount
                    : x.Cell.CurrentItem == null);

        /// <summary>The cell wears its item's look: the item's icon and the frame tinted with its rarity.</summary>
        private static bool WearsLook(InventorySlot cell, IItem item) =>
            cell.GetNode<TextureRect>("Icon").Texture == item.Icon
            && cell.GetNode<TextureRect>("Frame").Modulate == Color.FromHtml(TextPalette.RarityColor(item.Rarity));

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
                PlaceBody(player, chest.GlobalPosition + s_playerBodyFromChest);
                await Ticks();
                Check(interaction.Selected == chest.Target, "The chest is selected after entering its discovery area.");
                Check(controller.CandidateCount < 5, "Discovery retains a local candidate set.");
                int updates = controller.EvaluationCount;
                await Ticks(30);
                Check(controller.EvaluationCount - updates < 12, "Selection does not run on every physics tick.");
                Check(!chest.Contents.Initialized, "Hint polling does not generate chest contents.");

                var wall = new StaticBody2D { Position = chest.Position + s_wallFromChest };
                wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = s_wallSize } });
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
                var definition = provider.GetService<ChestCatalog>().Find(chest.DefinitionId)!;
                var cells = window!.GetNode<GridContainer>(CellsPath);
                Check(definition.Capacity == 20 && cells.GetChildCount() == definition.Capacity, "The starter chest window shows its twenty cells.");
                Check(chest.Contents.Slots.Select(x => x.Id).SequenceEqual(definition.Contents.Items.Select(x => x.SlotId)) && ShowsSlots(cells, chest),
                    "The authored positions fill the first cells in authored order and the other cells stay empty.");
                Check(chest.Contents.Slots.Select((slot, index) => WearsLook(cells.GetChild<InventorySlot>(index), slot.Item!)).All(x => x),
                    "A filled cell wears its item's icon and rarity frame.");

                await Hover(cells.GetChild<Control>(0));
                var tooltip = FindWindow<ItemTooltipPopup>(main);
                Check(tooltip != null && tooltip.GetNode<Label>(TooltipTitlePath).Text.StartsWith(chest.Contents.Slots[0].Item!.DisplayName),
                    "Hovering a filled cell shows the item tooltip of its item.");
                await PressPin();
                Check(tooltip!.IsPinned && !tooltip.GetChildren().OfType<InventorySlotTooltipButtons>().Any(),
                    "A pinned tooltip of a chest item offers no bag-only actions.");
                await PressPin();
                await Hover(cells.GetChild<Control>(definition.Capacity - 1));
                Check(FindWindow<ItemTooltipPopup>(main) == null, "Hovering an empty cell shows no tooltip.");

                var payload = new Godot.Collections.Dictionary
                {
                    [Core.Inventory.DragPayload.Item] = chest.Contents.Slots[0].Item!.Id,
                    [Core.Inventory.DragPayload.Instance] = chest.Contents.Slots[0].Item!.InstanceId,
                    [Core.Inventory.DragPayload.Quantity] = 1,
                    [Core.Inventory.DragPayload.MaxStackSize] = 1,
                    [Core.Inventory.DragPayload.Source] = cells.GetChild<Node>(0).GetPath()
                };
                var filled = cells.GetChild<InventorySlot>(0);
                Check(filled._GetDragData(Vector2.Zero).VariantType == Variant.Type.Nil && !filled._CanDropData(Vector2.Zero, payload)
                    && !cells.GetChild<InventorySlot>(definition.Capacity - 1)._CanDropData(Vector2.Zero, payload),
                    "A chest cell neither starts a drag nor takes a drop.");
                var bagView = new GridContainer();
                provider.GetService<ISlotLender>().AttachSlots(bagView);
                Check(bagView.GetChild<Slot>(0)._CanDropData(Vector2.Zero, payload), "A bag slot still takes a drop.");
                provider.GetService<ISlotLender>().DetachSlots();
                bagView.Free();

                var shownCells = cells.GetChildren().ToArray();
                var takenCell = cells.GetChild<InventorySlot>(2);
                bag.Clear();
                await RightClick(takenCell);
                Check(chest.Contents.Slots[2].Amount == 0 && bag.GetContents().Sum(x => x.Amount) == 1, "Right-click transfers the clicked item through root UI input.");
                Check(cells.GetChildren().SequenceEqual(shownCells) && takenCell.CurrentItem == null && ShowsSlots(cells, chest),
                    "The emptied slot leaves its own cell empty and every other cell as it was.");
                await RightClick(cells.GetChild<Control>(3), ctrl: true);
                Check(chest.Contents.Slots[3].Amount == 0 && bag.GetContents().Sum(x => x.Amount) == 2 && ShowsSlots(cells, chest),
                    "A right-click held with a modifier takes the cell's item too.");
                window.Refresh();
                Check(cells.GetChildren().SequenceEqual(shownCells) && ShowsSlots(cells, chest), "A repeated refresh reuses the cells.");
                // Control for the chest tooltip above: the same item, now held by the bag, grows the bag-only actions once pinned.
                var bagTooltip = (ItemTooltipPopup)ui.ShowPopup(typeof(ItemTooltipPopup));
                bagTooltip.ShowItem(chest.Contents.Slots[2].Item!);
                // The overlay layer adds a popup at the end of the frame; the pin key reaches it only once it is in the tree.
                await Ticks(1);
                await PressPin();
                Check(bagTooltip.IsPinned && bagTooltip.GetChildren().OfType<InventorySlotTooltipButtons>().Any(),
                    "The item taken into the bag grows the bag-only actions once pinned.");
                await PressPin();
                // Restore the original fixture for the independent partial-capacity scenario.
                bag.Clear();
                chest.RestoreLocationState(initial);
                interaction.CancelSession();
                await Ticks();
                await Press(Key.E);
                Check(JToken.DeepEquals(initial, chest.CaptureLocationState()), "Reopening preserves equipment rolls.");

                // A saved state may hold more slots than the capacity: every slot still gets its own cell.
                var resource = provider.GetService<IItemDataProvider>().GetAllResources().First(x => x.MaxStackSize > 10);
                var crowded = (JObject)initial.DeepClone();
                var crowdedSlots = (JArray)crowded["Slots"]!;
                while (crowdedSlots.Count < definition.Capacity) crowdedSlots.Add(JObject.FromObject(new { Id = $"empty{crowdedSlots.Count}", Amount = 0 }));
                crowdedSlots.Add(JObject.FromObject(new
                {
                    Id = "beyond", Amount = 2,
                    Item = new Core.Data.SaveData.InventoryItemSaveData { ResourceId = resource.Id, Amount = 2 }
                }));
                chest.RestoreLocationState(crowded);
                interaction.CancelSession();
                await Ticks();
                await Press(Key.E);
                cells = CellsOf(main);
                Check(cells.GetChildCount() == definition.Capacity + 1 && ShowsSlots(cells, chest),
                    "A chest holding more slots than its capacity shows every slot in its own cell.");
                chest.RestoreLocationState(initial);
                interaction.CancelSession();
                await Ticks();
                await Press(Key.E);

                // Fill all bag slots while leaving room for exactly three units in an existing stack.
                bag.Clear();
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
                Check(ShowsSlots(CellsOf(main), chest), "Take All redraws every slot in its own cell, the partial stack showing what is left.");
                Check(chest.Contents.RemoveAtMinutes == null, "A nonempty chest has no disappearance deadline.");
                var pending = bus.SendRequest<ContainerTransferRequest, InteractionResult>(new(chest.Target.Handle));
                interaction.CancelSession();
                Check(!(await pending).Success, "Closing a session cancels its queued transfer.");
                await Ticks();
                await Press(Key.E);
                var barrier = new StaticBody2D { Position = chest.Position + s_wallFromChest };
                barrier.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = s_wallSize } });
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
                PlaceBody(player, chest.GlobalPosition + s_playerBodyFromChest);
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
