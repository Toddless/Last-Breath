namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Ai;
    using Battle.Abilities;
    using Entity;
    using Enums;

    /// <summary>
    /// Which post-defeat cycle an NPC lives by. The choice travels as this enum and not as the
    /// authored string, so the wiring builds the cycle class by a switch and never by comparing text.
    /// <see cref="Undead"/> is the default member on purpose: a record that names no kind gets the
    /// only cycle that existed before the villager one.
    /// </summary>
    public enum NpcLifecycleKind : byte
    {
        /// <summary>Core.Ai.World.NpcLifecycle: the body rises AS UNDEAD, stronger the longer it lay.</summary>
        Undead,

        /// <summary>Core.Ai.World.VillagerLifecycle: the body gets up ALIVE, same faction, no strength gained.</summary>
        Villager
    }

    /// <summary>
    /// A fully rolled NPC ready to be applied to an entity: the provider resolved the stance,
    /// level, rarity, ability instances and behavior; parameters are already level-scaled.
    /// </summary>
    public record NpcDefinition
    {
        public required string NpcId { get; init; }
        public int Level { get; init; }
        public Rarity Rarity { get; init; }
        public EntityType EntityType { get; init; }
        public Fractions Fraction { get; init; }
        public Stance Stance { get; init; }
        public required IReadOnlyDictionary<EntityParameter, float> Parameters { get; init; }

        /// <summary>The very factor <see cref="Parameters"/> were scaled by — 1 + (level − 1) × levelScaling
        /// of the record. Carried so the bearer side can scale its FLAT modifier buffs by the same growth
        /// (NpcBuffBinder); 1 for a level-1 spawn and for anything built outside the provider.</summary>
        public float LevelFactor { get; init; } = 1f;
        public required IReadOnlyList<IAbility> Abilities { get; init; }

        /// <summary>Rolled NPC modifiers (count = type × rarity); copies, ready to attach.</summary>
        public IReadOnlyList<INpcModifier> Modifiers { get; init; } = [];

        /// <summary>Combat reactions (hidden triggered casts, e.g. the twin's assist). Empty for most NPCs.</summary>
        public IReadOnlyList<NpcReactionConfig> Reactions { get; init; } = [];

        /// <summary>Authored passive skills (boss kits, e.g. Deep Wounds): raw id + properties;
        /// the entity resolves them through the skill registry at apply time.</summary>
        public IReadOnlyList<NpcPassiveData> Passives { get; init; } = [];

        /// <summary>Boss stages (weakened opening act → transformation). Empty for most NPCs;
        /// when present, <see cref="Abilities"/> is empty — each stage owns its ability set.</summary>
        public IReadOnlyList<NpcStageConfig> Stages { get; init; } = [];
        public BehaviorProfile? Behavior { get; init; }

        /// <summary>World-mode brain tuning; null = the NPC stands still like before.</summary>
        public Ai.World.WorldBrainConfig? World { get; init; }

        /// <summary>Which post-defeat cycle to build; the config of the other kind is inert, never null,
        /// so the wiring switches on this and hands over a config without a null check of its own.</summary>
        public NpcLifecycleKind LifecycleKind { get; init; }

        /// <summary>Numbers of the UNDEAD cycle (resurrection/burning). Always present — defaults if not authored.</summary>
        public required Ai.World.NpcLifecycleConfig Lifecycle { get; init; }

        /// <summary>Numbers of the VILLAGER cycle (recovery timer). Always present — defaults if not
        /// authored; read only when <see cref="LifecycleKind"/> is <see cref="NpcLifecycleKind.Villager"/>.</summary>
        public Ai.World.VillagerLifecycleConfig VillagerLifecycle { get; init; } = new();

        /// <summary>Species capability, not state: whether this kind of NPC converses at all.</summary>
        public bool CanTalk { get; init; }
    }
}
