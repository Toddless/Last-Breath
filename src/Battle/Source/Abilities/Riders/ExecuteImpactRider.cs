namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data;

    /// <summary>Impact rider: executes the impact's target when its health is below
    /// <c>threshold</c> (0.30 = 30%) of maximum.</summary>
    public class ExecuteImpactRider(string id, float threshold) : IImpactRider
    {
        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded || !impact.Target.IsAlive) return Task.CompletedTask;
            float healthLeft = impact.Target.CurrentHealth / impact.Target.Parameters.MaxHealth;
            if (healthLeft < threshold) impact.Target.Kill();
            return Task.CompletedTask;
        }
    }
}
