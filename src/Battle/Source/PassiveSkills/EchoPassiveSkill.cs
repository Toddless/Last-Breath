namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    public class EchoPassiveSkill(
        float delayedDamagePercent,
        int turns)
        : Skill(id: "Passive_Skill_Echo")
    {
        private record DamageEntry
        {
            public int Turns { get; set; }
            public float Damage { get; init; }
        }

        private readonly List<DamageEntry> _delayedDamage = [];
        public float DelayedDamagePercent { get; } = delayedDamagePercent;
        public int Turns { get; } = turns;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<BeforeDamageTakenEvent>(OnBeforeDamageTaken);
            Owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnds);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<BeforeDamageTakenEvent>(OnBeforeDamageTaken);
            owner.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnds);
            Owner = null;
        }

        public override ISkill Copy() => new EchoPassiveSkill(DelayedDamagePercent, Turns);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not EchoPassiveSkill later) return false;

            return later.DelayedDamagePercent > DelayedDamagePercent;
        }

        private void OnBeforeDamageTaken(BeforeDamageTakenEvent evnt)
        {
            // Only echo real attack damage — not our own deferred hit (Passive/Pure) or DoTs,
            // otherwise the event now firing inside TakeDamage would re-process them endlessly.
            if (evnt.Context.Cause is not DamageCause.Attack) return;
            var context = evnt.Context;
            // Defers DelayedDamagePercent of every component; the rest is taken now.
            // E.g. 185 damage at 0.3 -> 129.5 now, 55.5 dealt after the delay. (Snapshot: Set mutates the collection.)
            float toDealLater = 0;
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
            {
                float deferred = damage * DelayedDamagePercent;
                toDealLater += deferred;
                context.Set(type, damage - deferred);
            }

            _delayedDamage.Add(new DamageEntry { Turns = Turns, Damage = toDealLater });
        }

        private void OnTurnEnds(TurnEndEvent turnEndEvent)
        {
            if (Owner == null) return;

            float totalDamage = 0;
            foreach (var damageEntry in _delayedDamage.ToList())
            {
                damageEntry.Turns--;
                if (damageEntry.Turns > 0) continue;
                totalDamage += damageEntry.Damage;
                _delayedDamage.Remove(damageEntry);
            }

            if (totalDamage <= 0) return;
            var context = new DamageContext { Source = Owner, Cause = DamageCause.Passive, IsCrit = false };
            context.Add(DamageType.Pure, totalDamage);
            Owner.TakeDamage(context);
        }
    }
}
