namespace LastBreath.World.Locations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Items;
    using Core.Events;
    using Core.MessageBus;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Views.UI;
    using Core.World.Locations;
    using Godot;
    using LootGeneration.Source;
    using Newtonsoft.Json.Linq;

    public sealed class LocationCoordinator(LocationCatalog catalog) : ILocationTravelService, ILocationSaveCoordinator, ISaveParticipant
    {
        private readonly Dictionary<string, LocationView> _loaded = [];
        private readonly Dictionary<string, LocationSnapshot> _snapshots = [];
        private Main? _host;
        private IGameServiceProvider _provider = null!;
        private LocationStateAdapter _state = null!;
        private SaveFile? _loadingFile;
        public string ActiveLocationId { get; private set; } = LocationCatalog.MainWorldId;
        public bool IsTransitioning { get; private set; }
        public string SectionId => "locations";
        public bool RequiredForLoad => true;
        public int Version => 1;
        public int RestoreOrder => Core.Save.RestoreOrder.SpawnPoints + 1;
        private IWorldClock Clock => _provider.GetService<IWorldClock>();
        private CharacterBody2D Player => (CharacterBody2D)_provider.GetService<IPlayerAccessor>().Player!;
        public LocationRoot? Loaded(string id) => _loaded.GetValueOrDefault(id)?.Root;

        public void Attach(Main host, MainWorld world, IGameServiceProvider provider)
        {
            _host = host;
            _provider = provider;
            _state = new LocationStateAdapter(provider);
            _loaded.Clear();
            _snapshots.Clear();
            ActiveLocationId = world.LocationId;
            var viewport = (SubViewport)world.GetViewport();
            _loaded.Add(world.LocationId, new LocationView(world, (SubViewportContainer)viewport.GetParent()));
            ValidateCatalogScenes();
            host.GetTree().Root.SizeChanged += Resize;
            provider.GetService<IGameEventBus>().Subscribe<NpcFinalDeathEvent>(OnDormantNpcRemoved);
            if (provider.GetService<ISaveGameService>().HasPendingLoad) return;
            ActivateFresh(world);
            if (catalog.Data.Start.LocationId == world.LocationId) PlaceAt(world.Endpoint(catalog.Data.Start.EndpointId));
            if (catalog.Data.Start.LocationId != world.LocationId)
            {
                IsTransitioning = true;
                viewport.GuiDisableInput = true;
                Callable.From(StartInConfiguredLocation).CallDeferred();
            }
        }

        private void StartInConfiguredLocation()
        {
            try
            {
                var view = Prepare(catalog.Data.Start.LocationId);
                ActivateFresh(view.Root);
                PlaceAt(view.Root.Endpoint(catalog.Data.Start.EndpointId));
                Present(view.Root.LocationId);
            }
            finally { IsTransitioning = false; Present(ActiveLocationId); }
        }

        public void Detach()
        {
            if (_host != null && GodotObject.IsInstanceValid(_host)) _host.GetTree().Root.SizeChanged -= Resize;
            _provider.GetService<IGameEventBus>().Unsubscribe<NpcFinalDeathEvent>(OnDormantNpcRemoved);
            _host = null;
            _loaded.Clear();
            _snapshots.Clear();
            _loadingFile = null;
            IsTransitioning = false;
        }

        private void ValidateCatalogScenes()
        {
            foreach (var definition in catalog.Data.Locations)
            {
                LocationRoot? temporary = null;
                var root = Loaded(definition.Id);
                try
                {
                    root ??= temporary = LoadRoot(definition.Id);
                    var endpoints = root.Descendants().OfType<LocationEndpoint>().ToList();
                    if (endpoints.Any(x => string.IsNullOrWhiteSpace(x.EndpointId))
                        || endpoints.Select(x => x.EndpointId).Distinct().Count() != endpoints.Count)
                        throw new InvalidOperationException($"Duplicate or empty endpoint in {definition.Id}.");
                    var points = root.Descendants().OfType<IPersistentSpawnPoint>().ToList();
                    if (points.Any(x => string.IsNullOrWhiteSpace(x.PointId)) || points.Select(x => x.PointId).Distinct().Count() != points.Count)
                        throw new InvalidOperationException($"Duplicate or empty spawn point ID in {definition.Id}.");
                    var objects = root.Descendants().OfType<ILocationStateParticipant>().ToList();
                    if (objects.Any(x => string.IsNullOrWhiteSpace(x.ObjectId)) || objects.Select(x => x.ObjectId).Distinct().Count() != objects.Count)
                        throw new InvalidOperationException($"Duplicate or empty object ID in {definition.Id}.");
                    var addresses = catalog.Data.Connections.SelectMany(x => new[] { x.From, x.To }).Append(catalog.Data.Start);
                    foreach (var address in addresses.Where(x => x.LocationId == definition.Id))
                        _ = root.Endpoint(address.EndpointId).Arrival;
                }
                finally { temporary?.Free(); }
            }
        }

        private LocationRoot LoadRoot(string id)
        {
            var scene = GD.Load<PackedScene>(catalog.Get(id).Scene) ?? throw new InvalidOperationException($"Missing scene: {id}.");
            var node = scene.Instantiate();
            if (node is LocationRoot root && root.LocationId == id)
            {
                // Scene-local players support F6 previews; a managed session transfers its existing player.
                foreach (var preview in root.Descendants().OfType<Core.Entity.IPlayer>().OfType<Node>().ToList())
                {
                    preview.GetParent().RemoveChild(preview);
                    preview.Free();
                }
                return root;
            }
            node.Free();
            throw new InvalidOperationException($"Scene must have a LocationRoot with ID {id}.");
        }

        private LocationView Prepare(string id)
        {
            if (_loaded.TryGetValue(id, out var existing)) return existing;
            var root = LoadRoot(id);
            try
            {
                var view = LocationView.Create(root, _host!, _host!.GetTree().Root.GetVisibleRect().Size, _loaded[LocationCatalog.MainWorldId].Viewport);
                _loaded.Add(id, view);
                return view;
            }
            catch { root.Free(); throw; }
        }

        private Task SafeBoundary()
        {
            var completion = new TaskCompletionSource();
            Callable.From(() => completion.SetResult()).CallDeferred();
            return completion.Task;
        }

        public async Task<TravelResult> TravelAsync(TravelRequest request)
        {
            if (_host == null || IsTransitioning) return TravelResult.Busy;
            if (_provider.GetService<IPlayerAccessor>().Player is not { IsAlive: true, IsFighting: false }
                || _provider.GetService<IUiElementsManager>().HasMovementBlockingWindow) return TravelResult.Unavailable;
            if (request.SourceLocationId != ActiveLocationId) return TravelResult.InvalidConnection;
            var source = _loaded[ActiveLocationId];
            var endpoint = source.Root.Descendants().OfType<LocationEndpoint>().SingleOrDefault(x => x.EndpointId == request.EndpointId);
            if (endpoint == null || catalog.Destination(new(request.SourceLocationId, request.EndpointId)) is not { } address)
                return TravelResult.InvalidConnection;
            if (!endpoint.InReach(Player)) return TravelResult.OutOfReach;
            IsTransitioning = true;
            source.Viewport.GuiDisableInput = true;
            Player.Velocity = Vector2.Zero;
            LocationView? destination = null;
            bool created = !_loaded.ContainsKey(address.LocationId);
            try
            {
                await SafeBoundary();
                if (_host == null || !GodotObject.IsInstanceValid(Player)
                    || _provider.GetService<IPlayerAccessor>().Player is not { IsAlive: true, IsFighting: false }) return TravelResult.Unavailable;
                destination = Prepare(address.LocationId);
                if (created && _snapshots.TryGetValue(address.LocationId, out var saved))
                {
                    using (_provider.GetService<LoadScope>().Begin()) _state.Restore(destination.Root, saved);
                }
                // Capture at the same boundary that stops the abandoned scene.
                LocationSnapshot? leaving = source.Root.LocationId == LocationCatalog.MainWorldId || source == destination
                    ? null : _state.Capture(source.Root, Clock.TotalMinutes);
                if (created)
                {
                    destination.Root.IsPreparing = false;
                    if (_snapshots.TryGetValue(address.LocationId, out var snapshot))
                        _state.Reconcile(destination.Root, snapshot.ElapsedMinutes(Clock.TotalMinutes));
                    else _state.FillFresh(destination.Root);
                    destination.Root.ProcessMode = Node.ProcessModeEnum.Inherit;
                }
                PlaceAt(destination.Root.Endpoint(address.EndpointId));
                Present(address.LocationId);
                if (created) _snapshots.Remove(address.LocationId);
                if (leaving != null)
                {
                    _snapshots[source.Root.LocationId] = leaving;
                    Unload(source.Root.LocationId);
                }
                try { await _provider.GetService<IGameMessageBus>().PublishMessageAsync(new LocationChangedMessage(ActiveLocationId)); }
                catch (Exception e) { Tracker.TrackException("Location changed notification failed after commit", e); }
                return TravelResult.Completed;
            }
            catch (Exception e)
            {
                Tracker.TrackException("Location transition failed", e);
                if (created && destination != null && ActiveLocationId != destination.Root.LocationId) Unload(destination.Root.LocationId);
                return TravelResult.Failed;
            }
            finally
            {
                IsTransitioning = false;
                if (_loaded.TryGetValue(ActiveLocationId, out var active)) active.Viewport.GuiDisableInput = false;
            }
        }

        private void PlaceAt(LocationEndpoint endpoint)
        {
            var root = LocationRoot.Find(endpoint)!;
            var player = Player;
            player.Reparent(root, false);
            player.Position = root.ToLocal(endpoint.Arrival.GlobalPosition);
            player.Rotation = endpoint.Arrival.GlobalRotation - root.GlobalRotation;
            player.Velocity = Vector2.Zero;
            var cameras = player.FindChildren("*", "Camera2D", true, false);
            foreach (var camera in cameras.OfType<Camera2D>()) camera.MakeCurrent();
        }

        private void Present(string id)
        {
            ActiveLocationId = id;
            foreach (var pair in _loaded) pair.Value.Present(pair.Key == id);
            _provider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(_loaded[id].Root);
        }

        private void ActivateFresh(LocationRoot root)
        {
            root.IsPreparing = false;
            _state.FillFresh(root);
            root.ProcessMode = Node.ProcessModeEnum.Inherit;
            Present(root.LocationId);
        }

        private void Unload(string id)
        {
            if (id == LocationCatalog.MainWorldId) throw new InvalidOperationException("MainWorld cannot be unloaded.");
            if (!_loaded.Remove(id, out var view)) return;
            _state.Suspend(view.Root);
            view.Root.IsPreparing = true;
            view.Container.GetParent().RemoveChild(view.Container);
            view.Container.Free();
        }

        private void OnDormantNpcRemoved(NpcFinalDeathEvent e)
        {
            foreach (var snapshot in _snapshots.Values)
                foreach (var npc in snapshot.State["npcs"]!.Children().Where(x => (string?)x["InstanceId"] == e.InstanceId).ToList())
                    npc.Remove();
        }

        private void Resize()
        {
            if (_host == null) return;
            foreach (var view in _loaded.Values) view.Container.Size = _host.GetTree().Root.GetVisibleRect().Size;
        }

        public JToken Capture()
        {
            var states = new Dictionary<string, LocationSnapshot>(_snapshots);
            foreach (var pair in _loaded) states[pair.Key] = _state.Capture(pair.Value.Root, Clock.TotalMinutes);
            return JToken.FromObject(new LocationSaveData { Locations = states });
        }

        public async Task PrepareLoadAsync(SaveFile file)
        {
            IsTransitioning = true;
            _loadingFile = file;
            foreach (var view in _loaded.Values) view.Viewport.GuiDisableInput = true;
            var placement = file.Sections.TryGetValue("playerPlacement", out var p)
                ? p.Data.ToObject<PlayerPlacementSaveData>() ?? new() : new PlayerPlacementSaveData();
            if (file.Sections.TryGetValue(SectionId, out var section))
            {
                if (section.Version != Version) throw new InvalidOperationException("Unsupported locations section.");
                var saved = section.Data.ToObject<LocationSaveData>() ?? throw new InvalidOperationException("Missing location data.");
                if (!saved.Locations.ContainsKey(LocationCatalog.MainWorldId) || !saved.Locations.ContainsKey(placement.LocationId))
                    throw new InvalidOperationException("Saved placement has no location snapshot.");
                foreach (var pair in saved.Locations) { catalog.Get(pair.Key); LocationStateAdapter.Validate(pair.Value); }
            }
            else if (placement.LocationId != LocationCatalog.MainWorldId)
                throw new InvalidOperationException("Legacy saves must place the player in MainWorld.");
            await SafeBoundary();
            Prepare(placement.LocationId);
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<LocationSaveData>()!;
            _snapshots.Clear();
            foreach (var pair in saved.Locations)
            {
                _snapshots[pair.Key] = pair.Value;
                int count = ((JArray)pair.Value.State["npcs"]!).Count;
                for (int i = 0; i < count; i++) _provider.GetService<INpcPopulationService>().ReserveOutsideLimit();
            }
            foreach (var pair in _loaded)
            {
                var snapshot = _snapshots[pair.Key];
                _state.Restore(pair.Value.Root, snapshot);
                pair.Value.Root.IsPreparing = false;
                _state.Reconcile(pair.Value.Root, snapshot.ElapsedMinutes(Clock.TotalMinutes));
                _snapshots.Remove(pair.Key);
            }
        }

        public void RestoreWithoutSection()
        {
            // Legacy sections have MainWorld coordinates and no side-location ownership.
            var world = _loaded[LocationCatalog.MainWorldId].Root;
            var npc = new NpcWorldSaveParticipant(_provider.GetService<Core.Ai.World.Skirmish.INpcWorldRegistry>(),
                _provider.GetService<INpcProvider>(), _provider.GetService<INpcModifierProvider>(),
                _provider.GetService<INpcPopulationService>(), _provider.GetService<INpcWorldSpawner>());
            var points = new SpawnPointsSaveParticipant(_provider.GetService<ISpawnPointRegistry>());
            if (_loadingFile?.Sections.TryGetValue("spawnPoints", out var legacyPoints) == true)
            {
                foreach (var point in _provider.GetService<ISpawnPointRegistry>().All)
                foreach (var record in (legacyPoints.Data["Points"] ?? legacyPoints.Data["points"])?.Children() ?? [])
                    if (((string?)(record["Id"] ?? record["id"]))?.EndsWith("/" + point.PointId, StringComparison.Ordinal) == true)
                        record["Id"] = point.PointId;
            }
            var ground = new GroundItemsSaveParticipant(_provider.GetService<IGroundItemStore>(), _provider.GetService<IItemDataProvider>(),
                _provider.GetService<EquipItemSaveConverter>(), _provider.GetService<IAugmentItemMinter>());
            foreach (ISaveParticipant participant in new ISaveParticipant[] { npc, points, ground })
                if (_loadingFile?.Sections.TryGetValue(participant.SectionId, out var section) == true) participant.Restore(section.Data, section.Version);
                else participant.RestoreWithoutSection();
            world.IsPreparing = false;
        }

        public PlayerPlacementSaveData CapturePlacement()
        {
            var root = _loaded[ActiveLocationId].Root;
            var position = root.ToLocal(Player.GlobalPosition);
            return new() { LocationId = ActiveLocationId, X = position.X, Y = position.Y, Rotation = Player.GlobalRotation - root.GlobalRotation };
        }

        public void RestorePlacement(PlayerPlacementSaveData data)
        {
            if (!float.IsFinite(data.X) || !float.IsFinite(data.Y) || !float.IsFinite(data.Rotation))
                throw new InvalidOperationException("Invalid saved player placement.");
            var root = _loaded[data.LocationId].Root;
            Player.Reparent(root, false);
            Player.Position = new Vector2(data.X, data.Y);
            Player.Rotation = data.Rotation;
            Player.Velocity = Vector2.Zero;
            Present(data.LocationId);
        }

        public void CompleteLoad(bool success)
        {
            _loadingFile = null;
            if (!success) return;
            IsTransitioning = false;
            foreach (var view in _loaded.Values) { view.Root.IsPreparing = false; view.Root.ProcessMode = Node.ProcessModeEnum.Inherit; }
            Present(ActiveLocationId);
        }
    }
}
