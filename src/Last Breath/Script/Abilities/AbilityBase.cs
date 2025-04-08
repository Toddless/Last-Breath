namespace Playground.Script.Abilities
{
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public abstract class AbilityBase(ICharacter owner, int cooldown, int cost) : IAbility
    {
        private readonly ICharacter _owner = owner;
        public string Name { get; } = string.Empty;
        public string Description { get; } = string.Empty;

        public int Cooldown { get; set; } = cooldown;
        public int Cost { get; set; } = cost;
        public ResourceType Type { get; }
        public bool CanActivateOnCaster { get; } = false;

        // i habe 3 types of Costs for each ability depends on stance:
        // 1. Dex => Combopoints
        // 2. Str => Furi
        // 3. Int => Mana

        /* Each type of costs have own regeneration mechanics
         *  Dex => Obtain 1 point for each additional strike
         *  Str => Obtain 1 point for each taken hit
         *  // TODO: Int stance is the weakest, need invest more time in it
         *  Int => Obtaint 1 Mana at the turn end
         *  
         *  I need typical triangle 
         *  
         *  Int => Str => Dex => Int
         */

        public virtual void Activate(ICharacter target)
        {
            if (!AbilityCanActivate()) return;
            var config = ConfigureEffects();
            ApllyOwnerEffects(config);
            ApllyTargetEffects(target, config);
            ApllyEffectsOnMultipleTargets(config);
        }
        public bool AbilityCanActivate() => Cooldown == 0 && _owner.Resource.CurrentResource.IsEnough(Cost) && _owner.Resource.IsResourceHasRightType(Type);
        protected abstract AbilityEffectConfig ConfigureEffects();

        private void ApllyEffectsOnMultipleTargets(AbilityEffectConfig config)
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

        private static void ApllyTargetEffects(ICharacter target, AbilityEffectConfig config)
        {
            foreach (var effect in config.TargetEffects)
            {
                target.Effects.AddEffects(effect);
            }
        }

        private void ApllyOwnerEffects(AbilityEffectConfig config)
        {
            foreach (var effect in config.SelfTarget)
            {
                _owner.Effects.AddEffects(effect);
            }
        }
    }
}
