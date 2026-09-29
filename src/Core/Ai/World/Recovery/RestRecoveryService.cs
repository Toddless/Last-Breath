namespace Core.Ai.World.Recovery
{
    using System;
    using System.Collections.Generic;
    using Core.World.Spaces;
    using Entity;
    using Godot;
    using Time;

    public class RestRecoveryService(IRecoveryConfigProvider configProvider, IWorldClock? clock = null, ISpatialQuery? spatial = null) : IRestRecoveryService
    {
        private readonly ISpatialQuery _spatial = spatial ?? NativeSpatialQuery.Instance;
        private readonly record struct Zone(object Owner, Func<Vector2> Position, float Radius, Func<IFightable, bool>? CanRest);
        private readonly record struct Participant(IFightable Entity, Func<Vector2> Position);

        private readonly List<Zone> _zones = [];
        private readonly List<Participant> _participants = [];
        private double _lastAbsoluteMinute = -1;
        private double _fallbackMinutes;

        // TODO:
        // Now Minutes уже несколько раз повторялся в коде в нескольких местах. Вынести в статичный метод
        /// <summary>Now in absolute game minutes; without a clock (sandbox scenes) one real second
        /// approximates one game minute — the spawn points' convention.</summary>
        private double NowMinutes => clock != null ? clock.Day * 1440 + clock.MinuteOfDay : _fallbackMinutes;

        public void RegisterZone(object owner, Func<Vector2> position, float radius, Func<IFightable, bool>? canRest = null)
        {
            UnregisterZone(owner);
            _zones.Add(new Zone(owner, position, radius, canRest));
        }

        public void UnregisterZone(object owner) => _zones.RemoveAll(zone => ReferenceEquals(zone.Owner, owner));

        // TODO:
        // регистрировать участников только в момент входа в зону
        public void RegisterParticipant(IFightable entity, Func<Vector2> position)
        {
            UnregisterParticipant(entity);
            _participants.Add(new Participant(entity, position));
        }

        // TODO:
        // удалять участников только в момент выхода из зоны
        public void UnregisterParticipant(IFightable entity) => _participants.RemoveAll(p => ReferenceEquals(p.Entity, entity));

        // TODO:
        // Перевести восстановление ресурсов с тиков каждый кадр на минуты WorldClock.
        public void Tick(float realDelta)
        {
            if (clock == null) _fallbackMinutes += realDelta;

            double now = NowMinutes;
            if (_lastAbsoluteMinute < 0) _lastAbsoluteMinute = now;
            double minutes = now - _lastAbsoluteMinute;
            if (minutes <= 0) return;
            _lastAbsoluteMinute = now;

            foreach (var participant in _participants)
            {
                var entity = participant.Entity;
                if (!entity.IsAlive || entity.IsFighting) continue;
                if (!InsideAnyZone(entity, participant.Position())) continue;
                Restore(entity, (float)minutes);
            }
        }

        public void Reconcile(IFightable entity, Vector2 position, float minutes)
        {
            if (minutes > 0 && entity is { IsAlive: true, IsFighting: false } && InsideAnyZone(entity, position)) Restore(entity, minutes);
        }

        private bool InsideAnyZone(IFightable entity, Vector2 position)
        {
            foreach (var zone in _zones)
                if (_spatial.SharesSpace(entity, zone.Owner) && position.DistanceTo(zone.Position()) <= zone.Radius && zone.CanRest?.Invoke(entity) != false)
                    return true;
            return false;
        }

        /// <summary>Untouched channels stay untouched — full vitals must not spam change events.</summary>
        private void Restore(IFightable entity, float minutes)
        {
            var config = configProvider.Config;

            float maxHealth = entity.Parameters.MaxHealth;
            if (entity.CurrentHealth < maxHealth)
                entity.CurrentHealth = Math.Min(maxHealth, entity.CurrentHealth + maxHealth * config.HealthPercentPerMinute * minutes);

            float maxMana = entity.Parameters.MaxMana;
            if (entity.CurrentMana < maxMana)
                entity.CurrentMana = Math.Min(maxMana, entity.CurrentMana + maxMana * config.ManaPercentPerMinute * minutes);

            float maxBarrier = entity.Parameters.MaxBarrier;
            if (entity.CurrentBarrier < maxBarrier)
                entity.CurrentBarrier = Math.Min(maxBarrier, entity.CurrentBarrier + maxBarrier * config.BarrierPercentPerMinute * minutes);
        }
    }
}
