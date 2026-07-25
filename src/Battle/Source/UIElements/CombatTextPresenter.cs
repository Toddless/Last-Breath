namespace Battle.Source.UIElements
{
    using System;
    using System.Linq;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Godot;

    /// <summary>
    /// The single battle-text subscriber: turns combat events into floating numbers.
    /// Game logic only publishes events; spots only anchor positions (resolved via <c>resolveAnchor</c>).
    /// Future display rules (per-component elemental colors, number stacking, status popups) belong here.
    /// </summary>
    public partial class CombatTextPresenter : Node
    {
        private IBattleEventBus? _eventBus;
        private Func<string, Node2D?>? _resolveAnchor;

        public void Setup(IBattleEventBus eventBus, Func<string, Node2D?> resolveAnchor)
        {
            _eventBus = eventBus;
            _resolveAnchor = resolveAnchor;
            _eventBus.Subscribe<DamageTakenEvent>(OnDamageTaken);
            _eventBus.Subscribe<EntityHealedEvent>(OnHealed);
            _eventBus.Subscribe<TurnSkippedEvent>(OnTurnSkipped);
            _eventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnDamageTaken(DamageTakenEvent evnt)
        {
            var context = evnt.Context;
            var numbers = SpawnNumbersAt(evnt.Target.InstanceId);
            numbers?.PlayDamageNumbers(Mathf.RoundToInt(context.TotalDamage), DominantType(context), context.IsCrit);
        }

        /// <summary>The number is tinted by the damage type that contributed the most to the hit.</summary>
        private static DamageType DominantType(IDamageContext context) =>
            context.DamageComponents.Count == 0
                ? DamageType.Physical
                : context.DamageComponents.MaxBy(component => component.Value).Key;

        private void OnHealed(EntityHealedEvent evnt)
        {
            var numbers = SpawnNumbersAt(evnt.Healed.InstanceId);
            numbers?.PlayHealNumbers(Mathf.RoundToInt(evnt.Amount));
        }

        private void OnTurnSkipped(TurnSkippedEvent evnt)
        {
            var numbers = SpawnNumbersAt(evnt.Fighter.InstanceId);
            numbers?.PlayStatusText(SkipTurnText(evnt.Cause));
        }

        // TODO(Todd): заменить хардкод на локализацию и поправить ключи/текст как надо.
        // private static string SkipTurnText(StatusEffects cause) => Localization.Localize($"Status_{cause}");
        private static string SkipTurnText(StatusEffects cause) =>
            (cause & StatusEffects.Freeze) != 0 ? "Заморожен" : "Оглушён";

        private FlyNumbers? SpawnNumbersAt(string instanceId)
        {
            var anchor = _resolveAnchor?.Invoke(instanceId);
            if (anchor == null) return null;

            var numbers = FlyNumbers.Initialize().Instantiate<FlyNumbers>();
            anchor.CallDeferred(Node.MethodName.AddChild, numbers);
            return numbers;
        }

        private void OnBattleEnd(BattleEndEvent obj) => Teardown();

        // An arena torn down without a battle end (app quit, scene reload) must not leave handlers on the bus.
        public override void _ExitTree() => Teardown();

        private void Teardown()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<DamageTakenEvent>(OnDamageTaken);
                _eventBus.Unsubscribe<EntityHealedEvent>(OnHealed);
                _eventBus.Unsubscribe<TurnSkippedEvent>(OnTurnSkipped);
                _eventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            }

            _eventBus = null;
            _resolveAnchor = null;
        }
    }
}
