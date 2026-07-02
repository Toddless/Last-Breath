namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class EchoPassiveSkill(
        float delayedDamagePercent,
        int turns)
        : Skill(id: "Passive_Skill_Echo")
    {
        private struct DamageEntry
        {
            public int Turns;
            public float Damage;
        }

        private readonly List<IFightable> _toRemove = [];
        private readonly Dictionary<IFightable, List<DamageEntry>> _damageSources = new();
        public float DelayedDamagePercent { get; } = delayedDamagePercent;
        public int Turns { get; } = turns;

        public override void Attach(IFightable owner)
        {
            // TODO: i need another event for this passive
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
            float actualDamage = context.Damage * DelayedDamagePercent;
            float toDealLater = context.Damage - actualDamage;
            context.Damage = actualDamage;
            if (!_damageSources.TryGetValue(context.Source, out List<DamageEntry>? sources))
            {
                sources = [];
                _damageSources[context.Source] = sources;
            }

            sources.Add(new DamageEntry { Turns = Turns, Damage = toDealLater });
        }

        private void OnTurnEnds(TurnEndEvent turnEndEvent)
        {
            if (_damageSources.Count == 0 || Owner == null) return;

            _toRemove.Clear();
            float totalDamage = 0;
            foreach ((IFightable source, List<DamageEntry> damages) in _damageSources)
            {
                if (!source.IsAlive)
                {
                    _toRemove.Add(source);
                    continue;
                }

                for (int i = damages.Count - 1; i >= 0; i--)
                {
                    var damage = damages[i];
                    damage.Turns--;
                    if (damage.Turns <= 0)
                    {
                        totalDamage += damage.Damage;
                        damages.RemoveAt(i);
                    }

                    damages[i] = damage;
                }

                if (damages.Count == 0)
                    _toRemove.Add(source);
            }

            var context = new DamageContext
            {
                Source = Owner,
                Cause = DamageCause.Passive,
                Damage = totalDamage,
                IsCrit = false,
                Type = DamageType.Pure
            };
            Owner.TakeDamage(context);

            foreach (IFightable entity in _toRemove)
                _damageSources.Remove(entity);
        }
    }
}
