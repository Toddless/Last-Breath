namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>Impact rider: every SUCCESSFUL impact puts a poison stack on its target, scaled by the impact damage.</summary>
    public class PoisonOnHitRider(int duration) : IImpactRider
    {
        public string Id => "Rider_Poison_On_Hit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;

            var poison = new DamageOverTurnEffect(duration, StatusEffects.Poison);
            await poison.Apply(new EffectApplyingContext
            {
                Caster = impact.Caster,
                Target = impact.Target,
                Source = InstanceId,
                Damage = impact.Damage,
                IsCritical = impact.IsCritical
            });
        }
    }
}
