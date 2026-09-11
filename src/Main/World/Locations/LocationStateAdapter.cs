namespace LastBreath.World.Locations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Items;
    using Core.Save;
    using Core.World.Locations;
    using Godot;
    using LootGeneration.Source;
    using Newtonsoft.Json.Linq;
    using Npc;

    internal sealed class LocationStateAdapter(IGameServiceProvider provider)
    {
        private InventoryItemSaveConverter Items => new(provider.GetService<EquipItemSaveConverter>(),
            provider.GetService<IItemDataProvider>(), provider.GetService<IAugmentItemMinter>());
        private LootOrchestrator Loot => (LootOrchestrator)provider.GetService<ILootOrchestrator>();

        public LocationSnapshot Capture(LocationRoot root, double now)
        {
            var nodes = root.Descendants().ToList();
            var records = nodes.OfType<BaseNpc>().Where(x => !x.IsSummon && !x.IsQueuedForDeletion())
                .Select(x => x.CaptureLocationNpc(root)).ToList();
            var points = nodes.OfType<IPersistentSpawnPoint>().ToList();
            foreach (var point in points)
            foreach (var record in records)
                if (point is NpcSpawnPoint normal && normal.OwnedIds.Contains(record.InstanceId)
                    || point is BossSpawnPoint boss && boss.OwnedId == record.InstanceId)
                    record.Owner = point.PointId;
            var converter = Items;
            var drops = Loot.ItemsOnGround.Where(x => Core.World.Spaces.SpatialAccess.SharesSpace(root, x) && x.Item != null)
                .Select(x => { var p = root.ToLocal(x.GlobalPosition); return new GroundItemSaveData { Item = converter.ToData(x.Item!, x.Quantity), X = p.X, Y = p.Y }; }).ToList();
            var objects = nodes.OfType<ILocationStateParticipant>().ToDictionary(x => x.ObjectId, x => x.CaptureLocationState());
            foreach (string id in root.AuthoredObjectIds)
                if (!objects.ContainsKey(id)) objects[id] = JValue.CreateNull();
            return new LocationSnapshot { LastSimulatedAt = now, State = new JObject
            {
                ["skirmishes"] = JToken.FromObject(((NpcSkirmishService)provider.GetService<Core.Ai.World.Skirmish.INpcSkirmishService>()).CaptureSpace(root)),
                ["npcs"] = JToken.FromObject(records),
                ["points"] = JToken.FromObject(points.Select(x => x.CaptureState()).ToList()),
                ["items"] = JToken.FromObject(drops),
                ["objects"] = JObject.FromObject(objects)
            }};
        }

        public void Restore(LocationRoot root, LocationSnapshot snapshot)
        {
            if (snapshot.Version != 1) throw new InvalidOperationException("Unsupported location snapshot.");
            var nodes = root.Descendants().ToList();
            foreach (var npc in nodes.OfType<BaseNpc>()) { npc.GetParent().RemoveChild(npc); npc.Free(); }
            var records = snapshot.State["npcs"]!.ToObject<List<LocationNpcState>>()!;
            var restored = records.ToDictionary(x => x.InstanceId, x => BaseNpc.RestoreLocationNpc(x, root, provider));
            foreach (var state in snapshot.State["points"]!.ToObject<List<SpawnPointSaveData>>()!)
            {
                var point = nodes.OfType<IPersistentSpawnPoint>().SingleOrDefault(x => x.PointId == state.Id);
                var residents = records.Where(x => x.Owner == state.Id).Select(x => restored[x.InstanceId]).ToList();
                if (point is NpcSpawnPoint normal) normal.RestoreOwnership(residents, state);
                else if (point is BossSpawnPoint boss) boss.RestoreOwnership(residents.FirstOrDefault());
            }
            foreach (var group in records.Where(x => x.GroupId != null).GroupBy(x => x.GroupId))
            {
                var squad = new Battle.Source.EntityGroup(group.Count());
                foreach (var record in group)
                {
                    var resident = restored[record.InstanceId];
                    resident.Group?.RemoveFromGroup(resident);
                    squad.TryAddToGroup(resident);
                }
            }
            ((NpcSkirmishService)provider.GetService<Core.Ai.World.Skirmish.INpcSkirmishService>()).RestoreSpace(
                snapshot.State["skirmishes"]?.ToObject<List<Core.Ai.World.Skirmish.NpcSkirmishState>>() ?? [], restored);
            ((Core.Ai.World.Raids.RaidService)provider.GetService<Core.Ai.World.Raids.IRaidService>()).ResumeResidents(restored.Values);
            var savedObjects = (JObject)snapshot.State["objects"]!;
            foreach (var obj in nodes.OfType<ILocationStateParticipant>())
            {
                if (!savedObjects.TryGetValue(obj.ObjectId, out var state)) continue;
                if (state.Type != JTokenType.Null) obj.RestoreLocationState(state);
                else if (obj is Node node) { node.GetParent().RemoveChild(node); node.Free(); }
            }
            var converter = Items;
            var drops = snapshot.State["items"]!.ToObject<List<GroundItemSaveData>>()!
                .Select(x => converter.FromData(x.Item) is { } item ? new GroundItemPlacement(item, x.Item.Amount, x.X, x.Y) : null)
                .OfType<GroundItemPlacement>().ToList();
            Loot.RestoreLocationItems(root, drops);
        }

        public void Reconcile(LocationRoot root, double minutes)
        {
            foreach (var obj in root.Descendants().OfType<ILocationStateParticipant>().ToList()) obj.ReconcileElapsed(minutes);
            var clock = provider.GetService<IWorldClock>();
            float seconds = (float)(minutes * clock.RealSecondsPerGameMinute);
            foreach (var npc in root.Descendants().OfType<BaseNpc>().ToList())
            {
                bool alive = npc.IsAlive;
                // Replacement deadlines start when the body actually rose, not when the player returned.
                if (npc.Lifecycle is { Stage: Core.Ai.World.NpcLifeStage.Defeated } lifecycle)
                    root.ReconciliationMinutes = clock.TotalMinutes - minutes
                        + Math.Max(0, lifecycle.ResurrectDelay - lifecycle.Elapsed) / clock.RealSecondsPerGameMinute;
                try { npc.Lifecycle?.Tick(seconds); }
                finally { root.ReconciliationMinutes = null; }
                if (alive) provider.GetService<Core.Ai.World.Recovery.IRestRecoveryService>().Reconcile(npc, npc.GlobalPosition, (float)minutes);
            }
            foreach (var point in root.Descendants().OfType<NpcSpawnPoint>().ToList()) point._Process(0);
        }

        public void Suspend(LocationRoot root)
        {
            ((NpcSkirmishService)provider.GetService<Core.Ai.World.Skirmish.INpcSkirmishService>()).SuspendSpace(root);
            ((Core.Ai.World.Raids.RaidService)provider.GetService<Core.Ai.World.Raids.IRaidService>()).SuspendSpace(root);
        }

        public void FillFresh(LocationRoot root)
        {
            foreach (var point in root.Descendants().OfType<IPersistentSpawnPoint>().ToList()) point.FillFresh();
        }

        public static void Validate(LocationSnapshot snapshot)
        {
            if (snapshot.Version != 1 || !double.IsFinite(snapshot.LastSimulatedAt) || snapshot.LastSimulatedAt < 0
                || snapshot.State["npcs"] is not JArray || snapshot.State["points"] is not JArray
                || snapshot.State["items"] is not JArray || snapshot.State["objects"] is not JObject)
                throw new InvalidOperationException("Invalid location snapshot.");
            var npcs = snapshot.State["npcs"]!.ToObject<List<LocationNpcState>>()!;
            if (npcs.Any(x => string.IsNullOrWhiteSpace(x.InstanceId) || string.IsNullOrWhiteSpace(x.NpcId)
                || !float.IsFinite(x.X) || !float.IsFinite(x.Y)) || npcs.Select(x => x.InstanceId).Distinct().Count() != npcs.Count)
                throw new InvalidOperationException("Invalid NPC location identity or placement.");
        }
    }
}
