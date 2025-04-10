namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public class EffectsManager(ICharacter owner)
    {
        // TODO: Decide: All effects are temporary and should be removed after fight ends or some of them can be permanent
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
            GD.Print($"Effect added: {effect.GetType().Name}");
        }

        public void RemoveEffect(IEffect effect)
        {
            if (!_effects.Contains(effect))
            {
                // log
                return;
            }
            GD.Print($"Effect removed: {effect.GetType().Name}");
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

        public void RemoveEffectByType(Effects effect)
        {
            var effectsToRemove = _effects.Where(x => x.Effect == effect).ToList();

            foreach (var eff in effectsToRemove)
            {
                eff.OnRemove(_owner);
                _effects.Remove(eff);
            }
        }

        public void RemoveAllEffects() => _effects.ForEach(RemoveEffect);
        public bool IsEffectApplied(Type effect) => _effects.Any(x => x.GetType() == effect);
    }
}
