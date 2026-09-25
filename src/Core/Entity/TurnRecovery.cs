namespace Core.Entity
{
    using System;
    using Context;
    using Enums;

    /// <summary>Consumes the flat recovery parameters over the owner's turn: the barrier comes back at its
    /// start, health and mana at its end. Percent-based recovery ("5% of max/current per turn") is granted
    /// by passive skills instead.</summary>
    public static class TurnRecovery
    {
        public static void ApplyTurnStart(IFightable entity)
        {
            float barrier = entity.Parameters.BarrierRecovery;
            if (barrier <= 0) return;

            // Capped by the owner's maximum barrier, and silent once it is full: the restore must not
            // republish a barrier change every turn — the aegis and the resource conditions watch that signal.
            float maximum = entity.Parameters.MaxBarrier;
            if (entity.CurrentBarrier >= maximum) return;
            entity.CurrentBarrier = Math.Min(maximum, entity.CurrentBarrier + barrier);
        }

        public static void ApplyTurnEnd(IFightable entity)
        {
            float health = entity.Parameters.HealthRecovery;
            if (health > 0) entity.Heal(new HealContext(entity, entity) { Amount = health, Cause = RecoveryCause.Regen });

            float mana = entity.Parameters.ManaRecovery;
            if (mana > 0) entity.RestoreMana(new ManaRecoveryContext(entity, entity) { Amount = mana, Cause = RecoveryCause.Regen });
        }
    }
}
