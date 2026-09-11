namespace LastBreath.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Entity;
    using Core.Items;
    using Core.Save;
    using Core.Services;
    using Core.World.Locations;
    using Godot;
    using LootGeneration.Source;
    using Npc;
    using World.Locations;

    public partial class LocationTravelTest : Node
    {
        private int _checks;
        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }
        private async Task Frames(int count = 3)
        {
            for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        private void CheckObjectSnapshots(IGameServiceProvider provider)
        {
            var viewport = new SubViewport { World2D = new World2D() };
            AddChild(viewport);
            var scene = GD.Load<PackedScene>("res://Tests/LocationStateFixture.tscn");
            var root = scene.Instantiate<LocationRoot>();
            viewport.AddChild(root);
            root.GetNode<LocationStateProbe>("Container").Contents = 0;
            root.GetNode("Removed").Free();
            var adapter = new LocationStateAdapter(provider);
            var snapshot = adapter.Capture(root, 100);
            root.Free();
            root = scene.Instantiate<LocationRoot>();
            viewport.AddChild(root);
            adapter.Restore(root, snapshot);
            adapter.Reconcile(root, snapshot.ElapsedMinutes(100.25));
            Check(root.GetNode<LocationStateProbe>("Container").Contents == 0, "Empty authored containers must not refill on activation.");
            Check(root.GetNodeOrNull("Removed") == null, "Permanent removal must survive scene re-instantiation.");
            var updated = adapter.Capture(root, 100.25);
            adapter.Restore(root, updated);
            adapter.Reconcile(root, updated.ElapsedMinutes(100.25));
            Check(root.GetNode<LocationStateProbe>("Container").ElapsedMinutes == 0.25, "The same elapsed interval cannot be reconciled twice.");
            viewport.QueueFree();
        }

        public override async void _Ready()
        {
            try
            {
                var main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
                main.Set("_addTestItems", false);
                AddChild(main);
                await Frames();
                var provider = Services.GameServiceProvider.Instance;
                var travel = provider.GetService<LocationCoordinator>();
                var world = travel.Loaded(LocationCatalog.MainWorldId)!;
                var player = (Node2D)provider.GetService<IPlayerAccessor>().Player!;
                var population = provider.GetService<INpcPopulationService>();
                var clock = provider.GetService<IWorldClock>();
                var request = new TravelRequest("MainWorld", "VillageSource");
                Check(await travel.TravelAsync(request) == TravelResult.OutOfReach, "A remote endpoint must refuse travel.");
                player.GlobalPosition = world.Endpoint("VillageSource").GlobalPosition;
                var first = travel.TravelAsync(request);
                Check(await travel.TravelAsync(request) == TravelResult.Busy, "Concurrent travel must be rejected.");
                Check(await first == TravelResult.Completed, "MainWorld to side location.");
                var side = travel.Loaded("SourceOfPowerNearVillage")!;
                Check(player.GetParent() == side && world.IsInsideTree(), "Player transfers while MainWorld stays resident.");
                Check(!Core.World.Spaces.SpatialAccess.SharesSpace(world, player), "Exploration locations must have independent worlds.");
                double before = clock.TotalMinutes;
                await Frames(5);
                Check(clock.TotalMinutes > before, "MainWorld clock continues while its view is hidden.");
                var definition = provider.GetService<INpcProvider>().CreateDefinition("Npc_Bandit_Veteran");
                var npc = (BaseNpc)provider.GetService<INpcWorldSpawner>().SpawnAt(definition, new Vector2(5000, 5000), side)!;
                npc.MarkAsWild();
                npc.CurrentHealth *= 0.5f;
                string npcId = npc.InstanceId;
                float health = npc.CurrentHealth;
                population.ReserveOutsideLimit();
                var corpse = (BaseNpc)provider.GetService<INpcWorldSpawner>().SpawnAt(definition, new Vector2(6000, 5000), side)!;
                corpse.MarkAsWild();
                corpse.RestoreAsBody(Core.Ai.World.NpcLifeStage.Defeated, 1, 0);
                string corpseId = corpse.InstanceId;
                population.ReserveOutsideLimit();
                int count = population.CurrentCount;
                var loot = (LootOrchestrator)provider.GetService<ILootOrchestrator>();
                var item = provider.GetService<IItemDataProvider>().GetAllResources().First();
                loot.RestoreLocationItems(side, [new GroundItemPlacement(item, 3, 3000, 3000)]);
                player.GlobalPosition = side.Endpoint("Entrance").GlobalPosition;
                Check(await travel.TravelAsync(new("SourceOfPowerNearVillage", "Entrance")) == TravelResult.Completed, "Side location to MainWorld.");
                Check(travel.Loaded("SourceOfPowerNearVillage") == null, "Abandoned side scene must unload.");
                Check(population.CurrentCount == count, "Dormant residents retain population reservations.");
                clock.Tick(2);
                player.GlobalPosition = world.Endpoint("VillageSource").GlobalPosition;
                Check(await travel.TravelAsync(request) == TravelResult.Completed, "Revisit side location.");
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                var restored = side.Descendants().OfType<BaseNpc>().Single(x => x.InstanceId == npcId);
                Check(Mathf.IsEqualApprox(restored.CurrentHealth, health), "Wounds away from a rest zone survive unload.");
                Check(population.CurrentCount == count, "Revisit must not reserve the same NPC twice.");
                var risen = side.Descendants().OfType<BaseNpc>().Single(x => x.InstanceId == corpseId);
                Check(risen.IsAlive && risen.IsRisen, "An unloaded corpse reconciles its resurrection deadline.");
                var returnEndpoint = world.Endpoint("VillageSource");
                world.RemoveChild(returnEndpoint);
                player.GlobalPosition = side.Endpoint("Entrance").GlobalPosition;
                Check(await travel.TravelAsync(new("SourceOfPowerNearVillage", "Entrance")) == TravelResult.Failed,
                    "A missing destination endpoint refuses the transition.");
                Check(player.GetParent() == side && !side.GetViewport().GuiDisableInput, "Failed travel preserves source placement and input.");
                world.AddChild(returnEndpoint);
                Check(loot.ItemsOnGround.Single(x => Core.World.Spaces.SpatialAccess.SharesSpace(side, x)).Quantity == 3, "Ground contents survive unload.");
                var manager = provider.GetService<ISaveManager>();
                var file = manager.Capture(new SaveMetadata());
                Check(file.Sections.ContainsKey("locations") && !file.Sections.ContainsKey("npcWorld"), "Locations own world state without duplicate legacy sections.");
                var placement = travel.CapturePlacement();
                player.GlobalPosition = side.Endpoint("Entrance").GlobalPosition;
                Check(await travel.TravelAsync(new("SourceOfPowerNearVillage", "Entrance")) == TravelResult.Completed, "Leave before loading a saved side location.");
                await travel.PrepareLoadAsync(file);
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                manager.Restore(file);
                travel.CompleteLoad(true);
                Check(travel.ActiveLocationId == placement.LocationId && player.GetParent() == side, "Saved side-location placement restores.");
                Check(population.CurrentCount == count, "Loading restores one reservation per resident.");
                Check(side.Descendants().OfType<BaseNpc>().Count(x => x.InstanceId == npcId) == 1, "Loading cannot duplicate residents.");
                Check(loot.ItemsOnGround.Count(x => Core.World.Spaces.SpatialAccess.SharesSpace(side, x)) == 1, "Loading cannot duplicate drops.");
                player.GlobalPosition = side.Endpoint("Entrance").GlobalPosition;
                GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = Key.E, Pressed = true }, true);
                await Frames(5);
                GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = Key.E, Pressed = false }, true);
                Check(travel.ActiveLocationId == "MainWorld", "The authored interaction action reaches the endpoint through the viewport.");
                player.GlobalPosition = world.Endpoint("VillageSource").GlobalPosition;
                Check(await travel.TravelAsync(request) == TravelResult.Completed, "Return for battle-origin validation.");
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                restored = side.Descendants().OfType<BaseNpc>().Single(x => x.InstanceId == npcId);
                var battlePosition = player.Position;
                using (var battle = new Battle.Source.BattleContext((IFightable)player, [restored], side, provider, main))
                {
                    await Frames(3);
                    var running = battle.RunBattleAsync();
                    await Frames(5);
                    Check(side.IsInsideTree() && world.IsInsideTree(), "Both exploration locations remain resident during a side-location battle.");
                    Check(!Core.World.Spaces.SpatialAccess.SharesSpace(side, player), "Battle has its own space even inside a side location.");
                    battle.Abort();
                    await running;
                }
                await Frames(3);
                Check(player.GetParent() == side && player.Position.IsEqualApprox(battlePosition), "Battle returns the player to the side location.");

                CheckObjectSnapshots(provider);
                // Stage in memory to exercise the real SaveDirector path without touching user save slots.
                var saveGame = provider.GetService<ISaveGameService>();
                typeof(SaveGameService).GetField("_pendingLoad",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(saveGame, file);
                population.Reset();
                main.QueueFree();
                await Frames(5);
                main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
                main.Set("_addTestItems", false);
                AddChild(main);
                await Frames(12);
                player = (Node2D)provider.GetService<IPlayerAccessor>().Player!;
                side = travel.Loaded("SourceOfPowerNearVillage")!;
                Check(!saveGame.HasPendingLoad && !travel.IsTransitioning, "SaveDirector completes the staged load after scene recreation.");
                Check(player.GetParent() == side && travel.ActiveLocationId == "SourceOfPowerNearVillage", "Scene reload restores side-location ownership.");
                Check(side.Descendants().OfType<BaseNpc>().Count(x => x.InstanceId == npcId) == 1, "Fresh scene restoration creates each saved resident once.");
                Check(population.CurrentCount == count, "Fresh scene restoration reserves saved residents once.");
                var inventoryHost = new GridContainer();
                AddChild(inventoryHost);
                provider.GetService<Core.Inventory.ISlotLender>().AttachSlots(inventoryHost);
                main.QueueFree();
                await Frames(6);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Frames(3);
                GD.Print($"LOCATION_TEST_PASS: {_checks} checks");
                GetTree().Quit();
            }
            catch (Exception e)
            {
                GD.PushError($"LOCATION_TEST_FAIL: {e}");
                GetTree().Quit(1);
            }
        }
    }
}
