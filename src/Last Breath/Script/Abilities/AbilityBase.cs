namespace Playground.Script.Abilities
{
    using System;
    using Godot;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public abstract class AbilityBase(ICharacter owner, int cooldown, int cost, ResourceType type, bool activateOnlyOnCaster) : IAbility
    {
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
        // ability change action to update ability state on ui?
        // ability cannot be activate if:
        // 1. Player/Enemy has not enough resources // on resource change
        // 2. Wrong target // when i change target?
        // 3. Cooldown // this will be animated
        // 4. Wrong current resource // on stance change

        // i habe 3 types of Costs for each ability depends on stance:
        // 1. Dex => Combopoints
        // 2. Str => Fury
        // 3. Int => Mana

        /* Each type of costs have own regeneration mechanics
         *  Dex => Obtain 1 point for each additional strike
         *  Str => Obtain 1 point for each taken hit
         *  // TODO: Int stance is the weakest, need to invest more time in it
         *  Int => Obtaint 1 Mana at the turn end
         *  
         *  I need typical triangle 
         *  
         *  Int => Str => Dex => Int
         */

        public event Action? OnCooldown, OnCost, OnTarget;

        public virtual void Activate(ICharacter target)
        {
            _owner.Resource.CurrentResource.OnSpend(Cost);
            Cooldown = _baseCooldown;
            var config = ConfigureEffects();
            ApplyOwnerEffects(config);
            ApplyTargetEffects(target, config);
            ApplyEffectsOnMultipleTargets(config);
        }

        public void UpdateCooldown()
        {
            if (Cooldown == 0) return;
            Cooldown--;
            GD.Print($"Cooldown reduced: {Cooldown} for ability {this.GetType().Name}");
        }

        public bool AbilityCanActivate(ICharacter target)
        {
            if (Cooldown > 0)
            {
                // TODO: Give player info what is wrong
                OnCooldown?.Invoke();
                return false;
            }
            if (_owner.Resource.CurrentResource.IsEnough(Cost))
            {
                // TODO: Give player info what is wrong
                OnCost?.Invoke();
                return false;
            }
            if (_owner.Resource.IsResourceHasRightType(Type))
            {
                GD.Print($"You cannot use this ability with it stance: {Type}");
                // TODO: Give player info what is wrong
                return false;
            }
            if (ActivateOnlyOnCaster && target != _owner)
            {
                // TODO: Give player info what is wrong
                OnTarget?.Invoke();
                return false;
            }
            if (target == null)
            {
                return false;
            }
            return true;
        }

        protected abstract AbilityEffectConfig ConfigureEffects();

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
                        casterTarget.Effects.AddEffects(effect);
                    }
                }
            }
        }

        private static void ApplyTargetEffects(ICharacter target, AbilityEffectConfig config)
        {
            foreach (var effect in config.TargetEffects)
            {
                target.Effects.AddEffects(effect);
            }
        }

        private void ApplyOwnerEffects(AbilityEffectConfig config)
        {
            foreach (var effect in config.SelfTarget)
            {
                _owner.Effects.AddEffects(effect);
            }
        }
    }
}
