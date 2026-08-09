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
    ///
    /// Two decorators that do the same thing (<see cref="AbilityEffectIdentity"/>) do not add up: only
    /// the strongest of them works. The rest are not thrown away — they sit in the set doing nothing
    /// and take over the moment the one above them leaves, so an augment that lost its parameter to a
    /// better one is dormant rather than discarded, and the socket it came from is none the wiser.
    /// </summary>
    public class AbilityParameterSet
    {
        private readonly Dictionary<string, Func<float>> _baseValues = [];
        private readonly Dictionary<string, List<AbilityParameterDecorator>> _decorators = [];

        /// <summary>
        /// How many upgrades are currently standing on a parameter they LENT the ability
        /// (<see cref="TryRegister"/>). Only borrowed keys are counted: what the ability registered for
        /// itself is not anybody's to give back, and a lender leaving must not take a number out from
        /// under the lenders still seated. Two sockets of one tier on one ability is an ordinary board
        /// (the shipped tree already has a pair), so "the last one out takes the key with him" is the
        /// rule rather than an edge case.
        /// </summary>
        private readonly Dictionary<string, int> _lenders = [];

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
        /// the key is already registered (e.g. auto-registered from abilityProperties). The ability's
        /// own, so it is never counted as lent and no upgrade can take it away.</summary>
        public void RegisterDefault(string parameter, float value) => _baseValues.TryAdd(parameter, () => value);

        /// <summary>
        /// Lends the ability a parameter it does not own — an upgrade arriving with a number of its own,
        /// so that number stands where every other number of the cast stands and is decorated by
        /// whatever else is seated.
        ///
        /// True means the caller now holds a lease and owes exactly one <see cref="Unregister"/> when it
        /// leaves — including the SECOND upgrade lending the same key, which keeps the base value
        /// already there and merely joins it. Every lender owing a return is what makes the count
        /// balance; a second lender told "no" would be a lender nobody expects a return from, and the
        /// key would outlive every one of them.
        ///
        /// False means the ability itself registered the parameter. It keeps its own base value, the
        /// newcomer reads and decorates that one, and nothing an upgrade does can take it off again.
        /// </summary>
        public bool TryRegister(string parameter, float value)
        {
            if (!_baseValues.ContainsKey(parameter))
            {
                _baseValues[parameter] = () => value;
                _lenders[parameter] = 1;
                return true;
            }

            if (!_lenders.TryGetValue(parameter, out int held)) return false;

            _lenders[parameter] = held + 1;
            return true;
        }

        /// <summary>
        /// Gives back one lease taken through <see cref="TryRegister"/>. The base value goes only with
        /// the LAST of them: while any lender is still seated the number it is standing on has to be
        /// there, or its rider would be reading a key nobody registered and applying nothing at all.
        /// Decorators seated on the key are left where they are: they belong to the upgrades that laid
        /// them and come off with those, and one upgrade quietly dropping another's move is how a build
        /// stops matching its sockets.
        /// </summary>
        public void Unregister(string parameter)
        {
            if (!_lenders.TryGetValue(parameter, out int held)) return;

            if (held > 1)
            {
                _lenders[parameter] = held - 1;
                return;
            }

            _lenders.Remove(parameter);
            _baseValues.Remove(parameter);
        }

        public float GetValue(string parameter)
        {
            if (!_baseValues.TryGetValue(parameter, out var baseValue))
            {
                Tracker.TrackNotFound($"Ability parameter '{parameter}'", this);
                return 0f;
            }

            float value = baseValue();
            if (!_decorators.TryGetValue(parameter, out var decorators)) return value;
            return Working(decorators, value).Aggregate(value, (current, decorator) => decorator.Decorate(current));
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

            // Everything handed over is seated, rivals included: which of them works is settled on the
            // way out, where the parameter's own base value is at hand to weigh them on.
            decorators.Add(newDecorator);
            decorators.Sort(InChainOrder);
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

        /// <summary>
        /// The decorators that actually run, out of everything seated on the parameter. Two of one
        /// identity are one effect at two strengths, so only the strongest of them is let through
        /// (<see cref="AbilityParameterDecorator.IsStrongerThan"/>) and the losers are passed over where
        /// they sit — taking the winner off is all it takes for one of them to start working.
        /// The winner is picked out of a chain ordered the same however the sockets were filled, so the
        /// same set of augments is the same build whichever of them the player seated first.
        /// </summary>
        private static List<AbilityParameterDecorator> Working(List<AbilityParameterDecorator> seated, float baseValue)
        {
            if (seated.Count < 2) return seated;

            Dictionary<AbilityEffectIdentity, int> strongest = [];
            for (int index = 0; index < seated.Count; index++)
            {
                AbilityEffectIdentity identity = seated[index].Identity;
                if (!strongest.TryGetValue(identity, out int held) || seated[index].IsStrongerThan(seated[held], baseValue))
                    strongest[identity] = index;
            }

            List<AbilityParameterDecorator> working = new(strongest.Count);
            for (int index = 0; index < seated.Count; index++)
                if (strongest[seated[index].Identity] == index) working.Add(seated[index]);

            return working;
        }

        /// <summary>The order the parameter's decorators are read in: <see cref="Core.Enums.Priority"/>
        /// first and the id where two of them share one. The chain has to read the same however the
        /// player filled his sockets, and the order decorators arrive in is the order of those sockets.</summary>
        private static int InChainOrder(AbilityParameterDecorator left, AbilityParameterDecorator right)
        {
            int byPriority = left.Priority.CompareTo(right.Priority);
            return byPriority != 0 ? byPriority : string.CompareOrdinal(left.Id, right.Id);
        }
    }
}
