namespace LootGeneration.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Ai;
    using Core.Ai.World;
    using Core.Battle;
    using Core.Context;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Items;
    using Core.Views.UI;
    using Godot;

    public partial class ExampleNpc : CharacterBody2D, IFightableNpc, IInitializable
    {
        private const string UID = "uid://b6kjn8vsd4jjv";

        private enum State
        {
            Idle,
            Moving
        }

        private State CurrentState
        {
            get;
            set
            {
                if (value == field) return;
                field = value;
                UpdateState();
            }
        } = State.Idle;

        private void UpdateState()
        {
        }

        private Label? _label;

        private Vector2 _lastPosition;

        private bool _mouseInside;
        [Export] private Area2D? _area;
        public IGameEventBus? GameEventBus { get; set; }
        public EntityType EntityType { get; set; }
        public Fractions Fraction { get; set; }
        public INpcLifecycle? Lifecycle { get; }
        public void ApplyDefinition(NpcDefinition definition) => throw new NotImplementedException();

        public void RestoreAsBody(NpcLifeStage stage, float resurrectDelay, float elapsed) => throw new NotImplementedException();

        public void RestoreAsRisen(float parameterBonus) => throw new NotImplementedException();

        public Rarity Rarity { get; set; }
        public int Level { get; set; }
        public string Id { get; } = "Example_Npc";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Texture2D? Icon { get; }
        public ICombatEventBus CombatEvents { get; }
        public IStance CurrentStance { get; }
        public ITargetChooser? TargetChooser { get; set; }
        public IEffectsComponent Effects { get; }
        public IParameterModifiersComponent ParameterModifiers { get; }
        public IEntityParametersComponent Parameters { get; }
        public IPassiveSkillsComponent PassiveSkills { get; }
        public IAnimationsComponent Animations { get; }
        public IModifierHandlerComponent ModifierHandler { get; }
        public ICombatComponent CombatComponent { get; }
        public IAbilityBookComponent AbilityBook { get; }
        public INpcModifiersComponent NpcModifiers { get; set; }
        public IBehaviorProfile? Behavior { get; set; }
        public float RisingBonus { get; }
        public bool IsRisen { get; }
        public IEntityAttribute Dexterity { get; }
        public IEntityAttribute Strength { get; }
        public IEntityAttribute Intelligence { get; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; }
        public string Description { get; }
        public string DisplayName { get; }
        public bool IsFighting { get; set; }
        public bool IsAlive { get; }

        public bool CanMove { get; set; }
        public float CurrentHealth { get; set; }
        public float CurrentBarrier { get; set; }
        public float CurrentMana { get; set; }
        public event Action<float>? CurrentManaChanged;
        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;
        public event Action<IFightable>? Dead;

        public override void _Ready()
        {
            _area?.MouseEntered += OnMouseEnter;
            _area?.MouseExited += OnMouseExit;
        }

        private void OnMouseExit()
        {
            _mouseInside = false;
            _label?.QueueFree();
        }

        private void OnMouseEnter()
        {
            _mouseInside = true;
            var label = new Label();
            string modifiers = "";
            for (int index = 0; index < NpcModifiers.AllModifiers.Count; index++)
            {
                INpcModifier mod = NpcModifiers.AllModifiers[index];
                modifiers += $"{index + 1}. Modifier: {mod.Id}, Current difficulty :{mod.DifficultyMultiplier}, Base difficulty: {mod.BaseDifficultyMultiplier}\n";
            }

            label.Text = $"Rarity: {Rarity}\nFraction: {Fraction}\nType: {EntityType}\nLevel: {Level}\n{modifiers}";
            _label = label;
            AddChild(label);
        }


        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mouseButton || !_mouseInside)
                return;

            switch (true)
            {
                case var _ when mouseButton is { ButtonIndex: MouseButton.Left, Pressed: true }:
                    NotifyDead();
                    break;
                case var _ when mouseButton is { ButtonIndex: MouseButton.Right, Pressed: true }:
                    QueueFree();
                    break;
            }

            GetViewport().SetInputAsHandled();
        }

        private void NotifyDead()
        {
            GameEventBus?.Publish<EntityDiedEvent>(new(this));
            Dead?.Invoke(this);
        }

        private Vector2 GetPointToMoveTo()
        {
            var viewRect = GetViewport().GetVisibleRect();
            float x = (float)GD.RandRange(viewRect.Position.X, viewRect.End.X);
            float y = (float)GD.RandRange(viewRect.Position.Y, viewRect.End.Y);

            return new Vector2(x, y);
        }

        public Task Attack(IAttackContext context) => throw new NotImplementedException();

        public void SetupBattleEventBus(IBattleEventBus bus) => throw new NotImplementedException();

        public void OnTurnEnd() => throw new NotImplementedException();

        public void OnTurnStart() => throw new NotImplementedException();

        public Task ReceiveAttack(IAttackContext context) => throw new NotImplementedException();

        public Task TakeDamage(IDamageContext context) => throw new NotImplementedException();

        public IFightable ChoseTarget(List<IFightable> targets) => throw new NotImplementedException();

        public void Kill(bool isDebug = false) => throw new NotImplementedException();

        public void AddItemToInventory(IItem item) => throw new NotImplementedException();

        public float GetDamage() => throw new NotImplementedException();

        public void Heal(IHealContext context) => throw new NotImplementedException();

        public void ConsumeResource(Costs type, float amount) => throw new NotImplementedException();

        public bool TryApplyStatusEffect(StatusEffects statusEffect) => throw new NotImplementedException();

        public bool TryRemoveStatusEffect(StatusEffects statusEffect) => throw new NotImplementedException();

        public bool IsSame(string otherId) => throw new NotImplementedException();
        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
        public void InjectServices(IGameServiceProvider provider) => throw new NotImplementedException();
    }
}
