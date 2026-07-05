namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>Impact rider: every SUCCESSFUL impact extends the poison stacks on its target.</summary>
    public class ExtendPoisonOnHitRider(int duration) : IImpactRider
    {
        public string Id => "Rider_Extend_Poison_On_Hit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return Task.CompletedTask;

            foreach (IEffect effect in impact.Target.Effects.GetBy(x => x.Status == StatusEffects.Poison))
                effect.Duration += duration;
            return Task.CompletedTask;
        }
    }
}
