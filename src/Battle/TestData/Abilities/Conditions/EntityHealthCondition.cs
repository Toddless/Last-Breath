namespace Battle.TestData.Abilities.Conditions
{
    using Core.Interfaces.Abilities;

    public class EntityHealthCondition(string id, float threshold, bool checkCaster = false) : ICondition
    {
        public string Id { get; } = id;

        public bool IsMet(EffectApplyingContext context)
        {
            var entity = checkCaster ? context.Caster : context.Target;
            float maxHealth = entity.Parameters.MaxHealth;
            float currentHealth = entity.CurrentHealth;

            float percent = currentHealth > 0 ? currentHealth / maxHealth : 0;
            return percent >= threshold;
        }
    }
}
