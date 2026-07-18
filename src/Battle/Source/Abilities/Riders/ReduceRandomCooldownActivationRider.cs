namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Godot;

    /// <summary>Cast rider: reduces the cooldown of one random cooling-down ability of the caster by
    /// <c>amount</c>. The ability being cast is excluded — it must not discount itself.</summary>
    public class ReduceRandomCooldownActivationRider(string id, int amount) : IActivationRider
    {
        private readonly RandomNumberGenerator _rnd = new();

        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(IAbilityActivationContext context)
        {
            var coolingDown = context.Caster.AbilityBook.AllAbilities
                .Where(ability => ability.CooldownLeft > 0 && ability.Id != context.Ability.Id)
                .ToList();
            if (coolingDown.Count == 0) return Task.CompletedTask;

            var lucky = coolingDown[_rnd.RandiRange(0, coolingDown.Count - 1)];
            lucky.CooldownLeft = Math.Max(0, lucky.CooldownLeft - amount);
            return Task.CompletedTask;
        }
    }
}
