namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class EchoPassiveSkill(
        float percentFromDamageToDealLater,
        int turns)
        : Skill(id: "Passive_Skill_Echo")
    {
        private struct DamageEntry
        {
            public int Turns;
            public float Damage;
        }

        private readonly List<IEntity> _toRemove = [];
        private readonly Dictionary<IEntity, List<DamageEntry>> _damageSources = new();
        public float PercentFromDamageToDealLater { get; } = percentFromDamageToDealLater;
        public int Turns { get; } = turns;

        public override void Attach(IEntity owner)
        {
            // TODO: i need another event for this passive
            Owner = owner;
            Owner.CombatEvents.Subscribe<BeforeDamageTakenEvent>(OnBeforeDamageTaken);
            Owner.CombatEvents.Subscribe<TurnEndGameEvent>(OnTurnEnds);
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<BeforeDamageTakenEvent>(OnBeforeDamageTaken);
            owner.CombatEvents.Unsubscribe<TurnEndGameEvent>(OnTurnEnds);
            Owner = null;
        }

        public override ISkill Copy() => new EchoPassiveSkill(PercentFromDamageToDealLater, Turns);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not EchoPassiveSkill later) return false;

            return later.PercentFromDamageToDealLater > PercentFromDamageToDealLater;
        }

        private void OnBeforeDamageTaken(BeforeDamageTakenEvent evnt)
        {
            var context = evnt.Context;
            if (context.Result is not AttackResults.Succeed) return;
            float actualDamage = context.FinalDamage * PercentFromDamageToDealLater;
            float toDealLater = context.FinalDamage - actualDamage;
            context.FinalDamage = actualDamage;
            if (!_damageSources.TryGetValue(context.Attacker, out List<DamageEntry>? sources))
            {
                sources = [];
                _damageSources[context.Attacker] = sources;
            }

            sources.Add(new DamageEntry { Turns = Turns, Damage = toDealLater });
        }

        private void OnTurnEnds(TurnEndGameEvent turnEndEvent)
        {
            if (_damageSources.Count == 0 || Owner == null) return;

            _toRemove.Clear();
            float totalDamage = 0;
            foreach ((IEntity source, List<DamageEntry> damages) in _damageSources)
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

            Owner.TakeDamage(Owner, totalDamage, DamageType.Pure, DamageSource.Passive);

            foreach (IEntity entity in _toRemove)
                _damageSources.Remove(entity);
        }
    }
}
