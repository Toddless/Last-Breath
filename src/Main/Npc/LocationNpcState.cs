namespace LastBreath.Npc
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai.World;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Enums;
    using Core.Entity;
    using Godot;

    public sealed class LocationNpcState
    {
        public string InstanceId { get; init; } = "";
        public string NpcId { get; init; } = "";
        public int Level { get; init; }
        public Rarity Rarity { get; init; }
        public Stance Stance { get; init; }
        public Dictionary<EntityParameter, float> Parameters { get; init; } = [];
        public List<string> Abilities { get; init; } = [];
        public List<string> Modifiers { get; init; } = [];
        public float X { get; init; }
        public float Y { get; init; }
        public float Rotation { get; init; }
        public float HomeX { get; init; }
        public float HomeY { get; init; }
        public float Health { get; init; }
        public float Mana { get; init; }
        public float Barrier { get; init; }
        public bool Wild { get; init; }
        public bool Risen { get; init; }
        public float RisingBonus { get; init; }
        public NpcLifeStage Stage { get; init; }
        public float Delay { get; init; }
        public float Elapsed { get; init; }
        public string? Owner { get; set; }
        public string? GroupId { get; init; }
        public bool HasWorldBrain { get; init; }
        public int BossStageIndex { get; init; }
        public List<float[]> PatrolRoute { get; init; } = [];
    }

    public partial class BaseNpc
    {
        public LocationNpcState CaptureLocationNpc(Node2D root)
        {
            var p = root.ToLocal(GlobalPosition);
            var home = root.ToLocal(HomePosition);
            return new LocationNpcState
            {
                InstanceId = InstanceId, NpcId = Id, Level = Level, Rarity = Rarity,
                GroupId = Group?.GetEntitiesInGroup<Core.Entity.IFightable>().Select(x => x.InstanceId).Order().FirstOrDefault(),
                HasWorldBrain = _brain != null, BossStageIndex = CurrentStageIndex,
                PatrolRoute = _patrolRoute?.Select(x => new[] { x.X, x.Y }).ToList() ?? [],
                Stance = AbilityBook.CurrentStance,
                Parameters = _definitionParameters?.ToDictionary(x => x.Key, x => x.Value) ?? [],
                Abilities = AbilityBook.AllAbilities.Select(x => x.Id).ToList(),
                Modifiers = NpcModifiers.AllModifiers.Select(x => x.Id).ToList(),
                X = p.X, Y = p.Y, Rotation = GlobalRotation - root.GlobalRotation,
                HomeX = home.X, HomeY = home.Y, Health = CurrentHealth, Mana = CurrentMana, Barrier = CurrentBarrier,
                Wild = IsWild, Risen = IsRisen, RisingBonus = RisingBonus,
                Stage = Lifecycle?.Stage ?? NpcLifeStage.Alive, Delay = Lifecycle?.ResurrectDelay ?? 0, Elapsed = Lifecycle?.Elapsed ?? 0
            };
        }

        public static BaseNpc RestoreLocationNpc(LocationNpcState state, Node2D root, IGameServiceProvider provider)
        {
            var npc = Initialize().Instantiate<BaseNpc>();
            npc.InstanceId = state.InstanceId;
            npc.Position = new Vector2(state.X, state.Y);
            npc.Rotation = state.Rotation;
            npc.SetPatrolRoute(state.PatrolRoute.Select(x => new Vector2(x[0], x[1])).ToList());
            npc.InjectServices(provider);
            root.AddChild(npc);
            var definition = provider.GetService<INpcProvider>().CreateDefinition(state.NpcId,
                new NpcDefinitionOverrides { Level = state.Level, Rarity = state.Rarity, Stance = state.Stance });
            var abilities = provider.GetService<IAbilityProvider>();
            var modifiers = provider.GetService<INpcModifierProvider>();
            npc.ApplyDefinition(definition with
            {
                World = state.HasWorldBrain ? definition.World : null,
                Parameters = state.Parameters.Count > 0 ? state.Parameters : definition.Parameters,
                Abilities = state.Abilities.Select(x => abilities.CreateAbility(x)).ToList(),
                Modifiers = state.Modifiers.Select(modifiers.GetModifier).ToList()
            }, provider);
            if (state.BossStageIndex > 0) npc.ApplyStage(state.BossStageIndex);
            npc.HomePosition = root.ToGlobal(new Vector2(state.HomeX, state.HomeY));
            if (state.Wild) npc.MarkAsWild();
            if (state.Risen) npc.RestoreAsRisen(state.RisingBonus);
            if (state.Stage is NpcLifeStage.Defeated or NpcLifeStage.Dormant)
                npc.RestoreAsBody(state.Stage, state.Delay, state.Elapsed);
            else
            {
                npc.CurrentHealth = state.Health;
                npc.CurrentMana = state.Mana;
                npc.CurrentBarrier = state.Barrier;
            }
            return npc;
        }
    }
}
