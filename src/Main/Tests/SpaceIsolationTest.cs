namespace LastBreath.Tests
{
    using System;
    using System.Threading.Tasks;
    using Battle.Source;
    using Core.Entity;
    using Core.Services;
    using Core.World.Spaces;
    using Godot;
    using Npc;

    /// <summary>Run this scene in Godot; native physics and tree transfers cannot be tested in plain dotnet.</summary>
    public partial class SpaceIsolationTest : Node
    {
        private int _checks;

        public override async void _Ready()
        {
            try
            {
                await CheckPhysicalIsolation();
                await CheckBattleRoundTrip();
                GD.Print($"SPACE_TEST_PASS: {_checks} checks");
                GetTree().Quit();
            }
            catch (Exception exception)
            {
                GD.PushError($"SPACE_TEST_FAIL: {exception}");
                GetTree().Quit(1);
            }
        }

        private async Task Frames(int count)
        {
            for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        private void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            _checks++;
        }

        private void Capture(string name)
        {
            if (DisplayServer.GetName() == "headless") return;
            var image = GetTree().Root.GetTexture().GetImage();
            image.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lastbreath-space-{name}.png"));
        }

        private async Task CheckPhysicalIsolation()
        {
            var root = new Node2D();
            AddChild(root);
            var originView = new SubViewportContainer { Stretch = true, Size = new Vector2(640, 480) };
            var viewport = new SubViewport { World2D = new World2D(), Size = new Vector2I(640, 480), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            originView.AddChild(viewport);
            root.AddChild(originView);
            var origin = new Node2D();
            viewport.AddChild(origin);
            var detector = new Area2D { CollisionMask = 1 };
            detector.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 100 } });
            origin.AddChild(detector);
            var nested = new Node2D { Position = new Vector2(12, 23), Rotation = 0.2f, Scale = new Vector2(1.2f, 1.2f) };
            origin.AddChild(nested);
            var body = new SpacePhysicsProbe { Position = Vector2.One * 3 };
            body.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 5 } });
            nested.AddChild(body);
            await Frames(4);
            Check(detector.GetOverlappingBodies().Contains(body), "Control: same-space body must trigger the detector.");
            var transform = body.Transform;
            var placement = new ParticipantPlacement(body);
            using (var presentation = new BattlePresentation(origin, root))
            {
                await Frames(3);
                Check(!originView.Visible && viewport.GuiDisableInput, "Origin presentation and input must be gated.");
                var ticks = body.PhysicsTicks;
                var position = body.Position;
                await Frames(4);
                Check(body.PhysicsTicks > ticks && body.Position != position, "Hidden origin must continue processing and moving.");
                body.GetParent().RemoveChild(body);
                presentation.Viewport.AddChild(body);
                body.Position = Vector2.Zero;
                await Frames(4);
                Check(!SpatialAccess.SharesSpace(origin, body), "Battle must have independent physics.");
                Check(!detector.GetOverlappingBodies().Contains(body), "Equal coordinates must not trigger origin contacts.");
                placement.Restore();
                Check(body.GetParent() == nested && body.Transform.IsEqualApprox(transform), "Return must restore nested parent and transform.");
                placement.Restore();
                Check(body.Transform.IsEqualApprox(transform), "Restoration must be idempotent.");
                await Frames(4);
                Check(detector.GetOverlappingBodies().Contains(body), "Restored body must rejoin origin physics.");
            }
            await Frames(3);
            Check(originView.Visible && !viewport.GuiDisableInput, "Presentation and input must recover after teardown.");
            root.QueueFree();
            await Frames(3);
        }

        private void CheckWorldQueries(Node2D origin, BaseNpc npc, Core.Data.IGameServiceProvider provider)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var hostileField = typeof(BaseNpc).GetField("_hostileToPlayer", flags)!;
            var previousHostility = hostileField.GetValue(npc);
            hostileField.SetValue(npc, true);
            Check(npc.GetSighting(float.MaxValue) != null, "Control: world NPC must see a same-space hostile target.");
            var placement = new ParticipantPlacement(npc);
            var foreign = new SubViewport { World2D = new World2D() };
            AddChild(foreign);
            npc.GetParent().RemoveChild(npc);
            foreign.AddChild(npc);
            Check(npc.GetSighting(float.MaxValue) == null, "NPC must not see targets in another native world.");
            placement.Restore();
            foreign.QueueFree();

            var events = provider.GetService<Core.Events.IGameEventBus>();
            var brain = new Core.Ai.World.WorldBrain(npc, new Core.Ai.World.WorldBrainConfig(), new Core.Entity.Components.DefaultRandomNumberGenerator());
            var brainField = typeof(BaseNpc).GetField("_brain", flags)!;
            var oldBrain = brainField.GetValue(npc);
            brainField.SetValue(npc, brain);
            ulong space = NativeSpatialQuery.Instance.GetSpace(npc);
            events.Publish(new Core.Events.WorldStimulusEvent(new Core.Ai.World.Stimulus(Core.Ai.World.StimulusType.Noise, npc.GlobalPosition, space + 1)));
            Check(brain.State == Core.Ai.World.AlertnessState.Calm, "Foreign noise must not reach the world brain.");
            events.Publish(new Core.Events.WorldStimulusEvent(new Core.Ai.World.Stimulus(Core.Ai.World.StimulusType.Noise, npc.GlobalPosition)));
            Check(brain.State == Core.Ai.World.AlertnessState.Calm, "Unscoped noise must be rejected.");
            events.Publish(new Core.Events.WorldStimulusEvent(new Core.Ai.World.Stimulus(Core.Ai.World.StimulusType.Noise, npc.GlobalPosition, space)));
            Check(brain.State == Core.Ai.World.AlertnessState.Suspicious, "Local noise must reach the world brain.");
            brainField.SetValue(npc, oldBrain);
            npc.StopMoving();

            var sites = provider.GetService<BattleSiteRegistry>();
            var marker = new World.BattleSiteMarker { Position = new Vector2(100000, 100000) };
            origin.AddChild(marker);
            marker.Setup(events, sites);
            Check(sites.Current?.SpaceId == space && sites.Current.BattleId == marker.BattleId, "Marker must register its origin and battle identity.");
            Core.Events.WorldStimulusEvent? noise = null;
            Core.Events.BattleJoinRequestEvent? join = null;
            void OnNoise(Core.Events.WorldStimulusEvent e) => noise = e;
            void OnJoin(Core.Events.BattleJoinRequestEvent e) => join = e;
            events.Subscribe<Core.Events.WorldStimulusEvent>(OnNoise);
            events.Subscribe<Core.Events.BattleJoinRequestEvent>(OnJoin);
            marker._Process(5);
            Check(noise?.Stimulus.SpaceId == space && noise.Stimulus.Position == marker.GlobalPosition, "Marker noise must carry its own world position.");
            typeof(World.BattleSiteMarker).GetMethod("OnBodyEntered", flags)!.Invoke(marker, [npc]);
            Check(join?.BattleId == marker.BattleId && ReferenceEquals(join.Fighter, npc), "Reinforcement request must identify its battle.");
            marker.Close();
            Check(sites.Current == null, "Closing a marker must synchronously release the raid target.");
            events.Unsubscribe<Core.Events.WorldStimulusEvent>(OnNoise);
            events.Unsubscribe<Core.Events.BattleJoinRequestEvent>(OnJoin);
            hostileField.SetValue(npc, previousHostility);
            marker.QueueFree();
        }

        private async Task CheckBattleRoundTrip()
        {
            var main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
            main.Set("_addTestItems", false);
            AddChild(main);
            await Frames(5);
            var origin = main.GetNode<Node2D>("WorldView/WorldSpace/MainWorld");
            var provider = Services.GameServiceProvider.Instance;
            var player = (Node2D)provider.GetService<IPlayerAccessor>().Player!;
            var playerParent = player.GetParent();
            var playerTransform = player.Transform;
            Check(origin.GetViewport().GetVisibleRect().Size == GetTree().Root.GetVisibleRect().Size, "World viewport must fill the session view.");
            Capture("world");
            var nested = new Node2D { Position = new Vector2(60000, 40000), Rotation = 0.1f };
            origin.AddChild(nested);
            var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
            npc.Position = new Vector2(55, 30);
            nested.AddChild(npc);
            var npcTransform = npc.Transform;
            var late = BaseNpc.Initialize().Instantiate<BaseNpc>();
            late.Position = new Vector2(100, 100);
            nested.AddChild(late);
            var lateTransform = late.Transform;
            npc.InjectServices(provider);
            CheckWorldQueries(origin, npc, provider);
            using (var context = new BattleContext((IFightable)player, [npc], origin, provider, main))
            {
                await Frames(2);
                var task = context.RunBattleAsync();
                await Frames(3);
                Check(SpatialAccess.SharesSpace(player, npc), "Fighters must share the arena.");
                Check(!SpatialAccess.SharesSpace(origin, player), "Actual player must leave world physics.");
                Check(context.TryJoinBattle(late, false), "Origin reinforcement must be admitted.");
                await Frames(3);
                Check(SpatialAccess.SharesSpace(player, late), "Reinforcement must enter arena physics.");
                var spot = (EntitySpot)npc.GetParent();
                var bus = (Core.Events.IBattleEventBus)typeof(BattleContext).GetField("_localBus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(context)!;
                bool picked = false;
                bus.Subscribe<Core.Events.AttackTargetSelectedEvent>(_ => picked = true);
                spot.MarkSelectable("space-test", true);
                var clickPosition = spot.GetGlobalTransformWithCanvas().Origin;
                npc.GetViewport().NotifyMouseEntered();
                Capture("arena");
                npc.GetViewport().PushInput(new InputEventMouseMotion { Position = clickPosition, GlobalPosition = clickPosition }, true);
                npc.GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = clickPosition, GlobalPosition = clickPosition }, true);
                await Frames(3);
                Check(picked, "Arena picking must resolve a rendered target.");
                Capture("arena");
                context.Abort();
                for (int i = 0; !task.IsCompleted && i < 300; i++) await Frames(1);
                Check(task.IsCompleted, "Aborted battle must complete.");
                await task;
            }
            Check(player.GetParent() == playerParent && player.Transform.IsEqualApprox(playerTransform), "Actual player must return to its transform.");
            Check(npc.GetParent() == nested && npc.Transform.IsEqualApprox(npcTransform), "Actual NPC must return to nested parent.");
            Check(late.GetParent() == nested && late.Transform.IsEqualApprox(lateTransform), "Late participant must return to admission point.");
            Check(!((IFightable)player).IsFighting && !npc.IsFighting && !late.IsFighting, "Battle flags must clear.");
            await Frames(3);
            using (var cancelled = new BattleContext((IFightable)player, [npc], origin, provider, main))
            {
                await Frames(2);
                // No RunBattleAsync: disposing preparation must also release state and return addresses.
            }
            await Frames(3);
            Check(!((IFightable)player).IsFighting && player.GetParent() == playerParent, "Cancelled preparation must restore player state.");
            Check(player.GetViewport().GetCamera2D() != null, "World camera must become current after return.");
            provider.GetService<Core.Views.UI.IUiElementsManager>().ChangeHud(typeof(UI.PlayerHud));
            await Frames(4);
            Capture("returned");
            main.QueueFree();
            await Frames(5);
        }
    }
}
