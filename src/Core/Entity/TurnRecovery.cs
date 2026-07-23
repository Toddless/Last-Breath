namespace Core.Entity
{
    using Context;
    using Enums;

    /// <summary>Consumes the flat HealthRecovery/ManaRecovery parameters at the end of the owner's turn.
    /// Percent-based recovery ("5% of max/current per turn") is granted by passive skills instead.</summary>
    public static class TurnRecovery
    {
        public static void Apply(IFightable entity)
        {
            float health = entity.Parameters.HealthRecovery;
            if (health > 0) entity.Heal(new HealContext(entity, entity) { Amount = health, Cause = RecoveryCause.Regen });

            float mana = entity.Parameters.ManaRecovery;
            if (mana > 0) entity.RestoreMana(new ManaRecoveryContext(entity, entity) { Amount = mana, Cause = RecoveryCause.Regen });
        }
    }
}
