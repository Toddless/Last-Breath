namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The single parameter store of an ability: base values are registered by the ability
    /// (common keys — <see cref="AbilityParameter"/>, ability-specific ones — its nested
    /// <c>Parameters</c> constants), upgrades and effects decorate them. Description values are
    /// built from <see cref="Keys"/>, so every registered parameter is a placeholder for free.
    /// </summary>
    public class AbilityParameterSet
    {
        private readonly Dictionary<string, Func<float>> _baseValues = [];
        private readonly Dictionary<string, List<AbilityParameterDecorator>> _decorators = [];

        public event Action<string>? ParameterChanged;

        public IReadOnlyCollection<string> Keys => _baseValues.Keys;

        public float this[string parameter] => GetValue(parameter);

        public void Register(string parameter, float value) => Register(parameter, () => value);

        public void Register(string parameter, Func<float> baseValue)
        {
            if (_baseValues.TryAdd(parameter, baseValue)) return;
            Tracker.TrackError($"Ability parameter '{parameter}' is already registered", this);
        }

        /// <summary>The ability's fallback for a parameter the data may supply: silently skipped when
        /// the key is already registered (e.g. auto-registered from abilityProperties).</summary>
        public void RegisterDefault(string parameter, float value) => _baseValues.TryAdd(parameter, () => value);

        public float GetValue(string parameter)
        {
            if (!_baseValues.TryGetValue(parameter, out var baseValue))
            {
                Tracker.TrackNotFound($"Ability parameter '{parameter}'", this);
                return 0f;
            }

            if (!_decorators.TryGetValue(parameter, out var decorators)) return baseValue();
            return decorators.Aggregate(baseValue(), (value, decorator) => decorator.Decorate(value));
        }

        public void AddDecorator(AbilityParameterDecorator newDecorator)
        {
            // A decorator on a key nobody registered would never be read — that is a data/upgrade typo.
            if (!_baseValues.ContainsKey(newDecorator.Parameter))
                Tracker.TrackNotFound($"Ability parameter '{newDecorator.Parameter}' for decorator '{newDecorator.Id}'", this);

            if (!_decorators.TryGetValue(newDecorator.Parameter, out var decorators))
            {
                decorators = [];
                _decorators[newDecorator.Parameter] = decorators;
            }

            var existing = decorators.FirstOrDefault(decorator => decorator.Id == newDecorator.Id);
            if (existing != null)
            {
                if (existing.IsStronger(newDecorator)) return;
                decorators.Remove(existing);
            }

            decorators.Add(newDecorator);
            decorators.Sort((left, right) => left.Priority.CompareTo(right.Priority));
            ParameterChanged?.Invoke(newDecorator.Parameter);
        }

        public void RemoveDecorator(string decoratorId, string parameter)
        {
            if (!_decorators.TryGetValue(parameter, out var decorators)) return;
            var existing = decorators.FirstOrDefault(decorator => decorator.Id == decoratorId);
            if (existing == null) return;
            decorators.Remove(existing);
            ParameterChanged?.Invoke(parameter);
        }
    }
}
