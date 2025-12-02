namespace Battle.Source
{
    using Godot;
    using System;
    using Stateless;
    using Services;
    using Core.Enums;
    using Components;
    using Core.Constants;
    using Core.Interfaces.Data;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    internal partial class Player : CharacterBody2D, IRequireServices
    {
        private enum State
        {
            Idle,
            Walk,
            Fight
        }

        private enum Trigger
        {
            Idle,
            Walk,
            Fight
        }

        private enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        private Direction _direction;
        private readonly StateMachine<State, Trigger> _stateMachine = new(State.Idle);
        private float _baseSpeed = 500;
        private readonly Dictionary<Stance, IStance> _stances = [];
        [Export] private AnimatedSprite2D? _animatedSprite;
        [Export] private Area2D? _interactionArea;

        private IMediator? _mediator;


        public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();
        public IStance CurrentStance { get; private set; }

        public IModifiersComponent ModifiersComponent { get; private set; }

        public bool IsFighting { get; set; }
        public bool IsAlive { get; set; }

        public float CurrentHealth
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Max(0, Mathf.Min(value, Parameters.MaxHealth));
                if (Mathf.Abs(clamped - field) < float.Epsilon) return;
                field = clamped;
                CurrentHealthChanged?.Invoke(field);
            }
        }

        public float CurrentBarrier
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Max(0, Mathf.Min(value, Parameters.MaxHealth));
                if (Mathf.Abs(clamped - field) < float.Epsilon) return;
                field = clamped;
                CurrentBarrierChanged?.Invoke(field);
            }
        }

        public event Action<float>? CurrentBarrierChanged, CurrentHealthChanged;


        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;
        public event Action<IFightable>? Dead;

        public override void _Ready()
        {
            GameServiceProvider.Instance.GetService<PlayerReference>().SetPlayerReference(this);
            if (_interactionArea != null) _interactionArea.BodyEntered += OnBodyEnter;
            ConfigureStateMachine();
        }

        public override void _Process(double delta)
        {
            Vector2 inputDirection =
                Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * _baseSpeed;
            SwitchState(inputDirection);
            MoveAndSlide();
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

        public void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false)
        {
        }


        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IMediator>();
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.Idle)
                .OnEntry(() => { _animatedSprite?.Play($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Idle)
                .Permit(Trigger.Walk, State.Walk)
                .Permit(Trigger.Fight, State.Fight);

            _stateMachine.Configure(State.Walk)
                .OnEntry(() => { _animatedSprite?.Play($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Walk)
                .Permit(Trigger.Idle, State.Idle)
                .Permit(Trigger.Fight, State.Fight);

            _stateMachine.Configure(State.Fight)
                .Permit(Trigger.Idle, State.Idle);
        }

        private void SwitchState(Vector2 direction)
        {
            if (direction == Vector2.Zero)
            {
                _stateMachine.Fire(Trigger.Idle);
                return;
            }

            Direction newDirection = DefineDirection(direction);
            _direction = newDirection;
            _stateMachine.Fire(Trigger.Walk);
        }

        private Direction DefineDirection(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                return _direction;

            if (Mathf.Abs(direction.Y) >= Mathf.Abs(direction.X))
                return direction.Y < 0 ? Direction.Up : Direction.Down;
            else
                return direction.X < 0 ? Direction.Left : Direction.Right;
        }

        private void OnBodyEnter(Node2D body)
        {
            // if (body is IFightable fightable)
            //     _mediator?.PublishAsync(new InitializeFightEvent<IFightable>([fightable, this]));
        }
    }
}
