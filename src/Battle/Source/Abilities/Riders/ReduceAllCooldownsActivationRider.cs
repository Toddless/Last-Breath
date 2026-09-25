namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;

    /// <summary>Cast rider: reduces the current cooldown of EVERY other ability of the caster by
    /// <c>amount</c>. Installed by <c>Augment_Reduce_All_Cooldowns</c>, which fits ANY ability rather
    /// than belonging to one.</summary>
    public class ReduceAllCooldownsActivationRider(string id, int amount) : IActivationRider
    {
        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(IAbilityActivationContext context)
        {
            foreach (var ability in context.Caster.AbilityBook.AllAbilities)
            {
                if (ability.Id == context.Ability.Id || ability.CooldownLeft == 0) continue;
                ability.CooldownLeft = Math.Max(0, ability.CooldownLeft - amount);
            }

            return Task.CompletedTask;
        }
    }
}
