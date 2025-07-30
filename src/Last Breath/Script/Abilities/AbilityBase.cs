namespace LastBreath.Script.Abilities
{
    using System;
    using Godot;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.Enums;
    using LastBreath.Script.Helpers;

    public abstract class AbilityBase(ICharacter owner, int cooldown, int cost, ResourceType type, bool activateOnlyOnCaster) : IAbility
    {
        public Texture2D? Icon { get; protected set; }
        private ICharacter? _target;
        // TODO: Icons, Animations etc
        private readonly int _baseCooldown = cooldown;
        private readonly ICharacter _owner = owner;
        public string Name { get; } = string.Empty;
        public string Description { get; } = string.Empty;
        public int Cooldown { get; set; } = 0;
        // TODO: Reduce or increase ability cost modifiers?
        public int Cost { get; set; } = cost;
        public ResourceType Type { get; } = type;
        public bool ActivateOnlyOnCaster { get; } = activateOnlyOnCaster;

        public ICharacter Target
        {
            get => _target ??= _owner;
            set
            {
                if (ObservableProperty.SetProperty(ref _target, value))
                {
                    AbilityUpdateState?.Invoke();
                }
            }
        }

        public event Action? OnCooldown, OnCost, OnTarget, AbilityUpdateState;

        public virtual void Activate()
        {
           // _owner.Resource.CurrentResource.OnSpend(Cost);
            Cooldown = _baseCooldown;
            AbilityUpdateState?.Invoke();
            var config = ConfigureEffects();
            ApplyOwnerEffects(config);
            ApplyTargetEffects(Target, config);
            ApplyEffectsOnMultipleTargets(config);
        }

        public void UpdateCooldown()
        {
            Cooldown--;
            AbilityUpdateState?.Invoke();
            GD.Print($"Cooldown reduced: {Cooldown} for ability {GetType().Name}");
        }

        public bool AbilityCanActivate()
        {
            if (Cooldown > 0)
            {
                // TODO: Give player info what is wrong
                OnCooldown?.Invoke();
                return false;
            }
           
            if (ActivateOnlyOnCaster && Target != _owner)
            {
                // TODO: Give player info what is wrong
                OnTarget?.Invoke();
                return false;
            }
            return true;
        }

        public void UpdateState() => AbilityUpdateState?.Invoke();

        protected abstract AbilityEffectConfig ConfigureEffects();
        protected abstract void LoadData();

        private void ApplyEffectsOnMultipleTargets(AbilityEffectConfig config)
        {
            if (config.MultipleTargetsSelector != null)
            {
                // TODO: Method to get caster targets
                var targets = config.MultipleTargetsSelector(_owner);

                foreach (var casterTarget in targets)
                {
                    foreach (var effect in config.MultiEffects)
                    {
                        casterTarget.Effects.AddTemporaryEffect(effect);
                    }
                }
            }
        }

        private void ApplyTargetEffects(ICharacter target, AbilityEffectConfig config)
        {
            foreach (var effect in config.TargetEffects)
            {
                target.Effects.AddTemporaryEffect(effect);
            }
        }

        private void ApplyOwnerEffects(AbilityEffectConfig config)
        {
            foreach (var effect in config.SelfTarget)
            {
                _owner.Effects.AddTemporaryEffect(effect);
            }
        }
    }
}
