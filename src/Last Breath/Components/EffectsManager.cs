namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;

    public class EffectsManager(ICharacter owner)
    {
        private readonly List<IEffect> _effects = [];
        private readonly ICharacter _owner = owner;

        public void AddEffects(IEffect effect)
        {
            if (_effects.Contains(effect))
            {
                var existingEffect = _effects.First(x => x.Effect == effect.Effect);
                existingEffect.OnStacks(effect);
                //existingEffect.OnTick(_owner);
            }
            effect.OnApply(_owner);
            _effects.Add(effect);
        }

        public void RemoveEffect(IEffect effect)
        {
            if (!_effects.Contains(effect))
            {
                // log
                return;
            }
            effect.OnRemove(_owner);
            _effects.Remove(effect);
        }

        public void UpdateEffects()
        {
            // ToArray preventing Collection Modified exception
            foreach (var effect in _effects.ToArray())
            {
                effect.OnTick(_owner);
                if (effect.Expired) RemoveEffect(effect);
            }
        }
    }
}
