namespace Battle.Source
{
    using Godot;
    using System;
    using Components;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Components;
    using Core.Interfaces.Events;
    using Utilities;

    internal partial class Enemy : CharacterBody2D, IFightable, IInitializable, IRequireServices
    {
        private const string UID = "uid://bssmtdwwycbpt";
        [Export] private AnimatedSprite2D? _animatedSprite;
        [Export] private Area2D? _interactionArea;
        private IMediator? _mediator;
        public IHealthComponent Health { get; private set; }

        public IDamageComponent Damage { get; private set; }

        public IDefenceComponent Defence { get; private set; }

        public bool IsFighting { get; set; }
        public bool IsAlive { get; set; }

        public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();
        public IStance CurrentStance { get; set; }

        public IEntityGroup? Group { get; set; }

        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;
        public event Action<IFightable>? Dead;

        public override void _Ready()
        {
            // TODO: Необходимо определить кто начал файт. Как?
            _animatedSprite?.Play("Idle");
            if (_interactionArea != null)
                _interactionArea.BodyEntered += OnBodyEnter;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IMediator>();
        }


        public void AllAttacks()
        {
        }

        public void OnBlockAttack()
        {
        }

        public void OnEvadeAttack()
        {
        }

        public void OnReceiveAttack(IAttackContext context)
        {
        }

        public void OnTurnEnd()
        {
        }

        public void OnTurnStart()
        {
        }

        public void TakeDamage(float damage, bool isCrit = false)
        {
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnBodyEnter(Node2D body)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);
                switch (body)
                {
                    case Player player:
                        {
                            var fighters = Group?.GetEntitiesInGroup<IFightable>() ?? [this];
                            if (!fighters.Contains(player))
                                fighters.Add(player);
                            _mediator.PublishAsync(new InitializeFightEvent<IFightable>(fighters));
                            break;
                        }
                    case IFightable fighter:
                        // TODO: Decide to begin fight or not
                        break;
                }
                // TODO: Change state to "Fighting"
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to handle body enter", e, this);
            }
        }
    }
}
