namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;

    /// <summary>Impact rider: every SUCCESSFUL impact extends the poison stacks on its target.</summary>
    public class ExtendPoisonOnHitRider(int duration) : IImpactRider
    {
        public string Id => "Rider_Extend_Poison_On_Hit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return Task.CompletedTask;

            // Through the effect's own road: what a hit adds is charged to the poison's extension
            // budget, so a rider firing on every impact of a long series cannot make one stack eternal.
            foreach (IEffect effect in impact.Target.Effects.GetBy(x => x.Status == StatusEffects.Poison))
                effect.Extend(duration);
            return Task.CompletedTask;
        }
    }
}
