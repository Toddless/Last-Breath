namespace Battle.Source.Abilities.Summon
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Events;

    /// <summary>
    /// The Bone Pack Leader's call: raises bone wolves at the summoner's side, each inheriting a
    /// share of the summoner's CURRENT stats at cast time. The ability only publishes the request —
    /// the arena's SummonService owns the spawn, the dynamic slot and the per-summoner cap
    /// (a recast refills the pack up to WolvesPerCast, never stacks past it).
    /// </summary>
    public class SummonBoneWolves(AbilityBaseData data) : Ability(data)
    {
        // Content-level constant: abilityProperties is a float dictionary, an npc id cannot travel in it.
        private const string SummonedNpcId = "Npc_Bone_Wolf";

        public int WolvesPerCast => (int)this[Parameters.WolvesPerCast];
        public float StatShare => this[Parameters.StatShare];

        public static class Parameters
        {
            public const string WolvesPerCast = nameof(WolvesPerCast);
            public const string StatShare = nameof(StatShare);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.WolvesPerCast, 3f);
            parameters.RegisterDefault(Parameters.StatShare, 0.25f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new SummonBoneWolves(Data));

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            owner.CombatEvents.Publish(new SummonRequestedEvent(owner, SummonedNpcId, WolvesPerCast, WolvesPerCast, StatShare));
            return Task.CompletedTask;
        }
    }
}
