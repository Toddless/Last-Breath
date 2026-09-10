namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Modifiers;
    using Decorator;
    using Godot;
    using Module;

    public class EntityParametersComponent : IEntityParametersComponent
    {
        /// <summary>Hard ceiling of a resistance maximum: at 1 the mitigation formula would turn a hit into
        /// healing, so the parameter that raises the cap has a cap of its own.</summary>
        private const float ResistanceMaximumCeiling = 0.9f;

        /// <summary>Effective-value bounds — the single owner of every parameter cap. Applied in the
        /// indexer, so ALL read channels (named properties, GetValueForParameter, ParameterChanged)
        /// agree; decorators cannot push a value past its cap (tracker #42).
        /// Parameters without an entry are floored at zero: every member of the enum is a magnitude
        /// or a chance, and a negative value corrupts downstream formulas (negative armor amplifies
        /// damage). The modifier-recalc path already clamped this way; the decorator path must agree.
        /// Resistances themselves are deliberately unbounded above: their ceiling is a parameter
        /// (<see cref="ResistanceParameters"/>) read at mitigation, and a clamped total would erase the
        /// overcap that shred eats first.</summary>
        private static readonly Dictionary<EntityParameter, (float Min, float Max)> s_bounds = new()
        {
            [EntityParameter.BlockChance] = (0f, 0.9f),
            [EntityParameter.CriticalChance] = (0f, 1f),
            [EntityParameter.AdditionalHitChance] = (0f, 0.75f),
            [EntityParameter.ArmorPenetration] = (0f, 1f),
            [EntityParameter.Suppress] = (0f, 0.75f),
            [EntityParameter.CriticalDamageMitigation] = (0f, 1f),
            [EntityParameter.LightningResistanceMaximum] = (0f, ResistanceMaximumCeiling),
            [EntityParameter.FireResistanceMaximum] = (0f, ResistanceMaximumCeiling),
            [EntityParameter.ColdResistanceMaximum] = (0f, ResistanceMaximumCeiling),
            [EntityParameter.PoisonResistanceMaximum] = (0f, ResistanceMaximumCeiling),
            [EntityParameter.SuppressChance] = (0f, 1f),
            [EntityParameter.FireResistancePenetration] = (0f, 1f),
            [EntityParameter.ColdResistancePenetration] = (0f, 1f),
            [EntityParameter.LightningResistancePenetration] = (0f, 1f),
            [EntityParameter.PoisonResistancePenetration] = (0f, 1f)
        };

        /// <summary>Bases an entity is born with. A resistance maximum is a rule of the game rather than a
        /// line of a stat profile, so it stands here instead of in every entity's data: a seeding pass that
        /// leaves the parameter unnamed leaves the standard cap in place.</summary>
        private static readonly Dictionary<EntityParameter, float> s_defaultBases =
            ResistanceParameters.Maximums.ToDictionary(maximum => maximum, _ => ResistanceParameters.DefaultMaximum);

        private readonly Dictionary<EntityParameter, (float Base, float Current)> _parameterValues =
            Enum.GetValues<EntityParameter>().ToDictionary(key => key, key => (DefaultBase(key), DefaultBase(key)));

        /// <summary>Luck sources per parameter, counted signed: lucky and unlucky sources coexist and cancel
        /// one another, so the roll asks for a single verdict instead of the list.</summary>
        private readonly Dictionary<EntityParameter, int> _chanceLuck = new();

        /// <summary>Denial sources per parameter, counted the same way: while any of them stands, a roll on
        /// the parameter is taken and lost. Counted rather than flagged so two sources of one denial (an item
        /// grant beside a tree node) hand the chance back only when both are gone.</summary>
        private readonly Dictionary<EntityParameter, int> _chanceDenials = new();

        /// <summary>Pools a conversion has taken over. A parameter in here reads nothing whatever its
        /// numbers say — the verdict is categorical, so it is taken after the formula and after the
        /// decorators rather than inside either, where a multiplier or an added figure would leave part of
        /// a pool that has already been given away alive. Kept as a set instead of asked of the modifier
        /// list on every read: the indexer answers every parameter read of a fight.</summary>
        private readonly HashSet<EntityParameter> _convertedPools = [];

        private readonly IModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator> _moduleManager;
        private float this[EntityParameter type] => Present(type, _moduleManager.GetModule(type).GetValue());
        private Func<EntityParameter, IReadOnlyList<IModifier>>? _getModifiersForParameter;

        public float MaxHealth => this[EntityParameter.Health];
        public float HealthRecovery => this[EntityParameter.HealthRecovery];
        public float PhysicalDamage => this[EntityParameter.PhysicalDamage];
        public float ColdDamage => this[EntityParameter.ColdDamage];
        public float LightningDamage => this[EntityParameter.LightningDamage];
        public float FireDamage => this[EntityParameter.FireDamage];
        public float BlockChance => this[EntityParameter.BlockChance];
        public float CriticalDamage => this[EntityParameter.CriticalDamage];
        public float CriticalChance => this[EntityParameter.CriticalChance];
        public float AdditionalHit => this[EntityParameter.AdditionalHitChance];
        public float MulticastChance => this[EntityParameter.MulticastChance];
        public float SpellDamage => this[EntityParameter.SpellDamage];
        public float Accuracy => this[EntityParameter.Accuracy];
        public float Armor => this[EntityParameter.Armor];
        public float ArmorPenetration => this[EntityParameter.ArmorPenetration];
        public float Evade => this[EntityParameter.Evade];
        public float MaxBarrier => this[EntityParameter.Barrier];
        public float BarrierRecovery => this[EntityParameter.BarrierRecovery];
        public float Suppress => this[EntityParameter.Suppress];
        public float MaxMana => this[EntityParameter.Mana];
        public float ManaRecovery => this[EntityParameter.ManaRecovery];
        public float MoveSpeed => this[EntityParameter.MoveSpeed];
        public float SuppressChance => this[EntityParameter.SuppressChance];
        public float CriticalDamageMitigation => this[EntityParameter.CriticalDamageMitigation];

        public event Action<EntityParameter, float>? ParameterChanged;

        public EntityParametersComponent()
        {
            _moduleManager = new ModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator>(_parameterValues.ToDictionary(kv => kv.Key,
                IParameterModule<EntityParameter> (kv) => new Module<EntityParameter>(() => _parameterValues[kv.Key].Current, kv.Key)));
        }

        public float GetValueForParameter(EntityParameter parameter) => this[parameter];
        public void Initialize(Func<EntityParameter, IReadOnlyList<IModifier>> getModifiers)
        {
            _getModifiersForParameter = getModifiers;
            _moduleManager.ModuleChanges += RaiseParameterChanges;
        }

        public void AddModuleDecorator(EntityParameterModuleDecorator decorator) => _moduleManager.AddDecorator(decorator);
        public void RemoveModuleDecorator(string id, EntityParameter param) => _moduleManager.RemoveDecorator(id, param);
        public void AddChanceLuck(EntityParameter parameter, ChanceLuck luck) => ShiftChanceLuck(parameter, (int)luck);
        public void RemoveChanceLuck(EntityParameter parameter, ChanceLuck luck) => ShiftChanceLuck(parameter, -(int)luck);
        public ChanceLuck GetChanceLuck(EntityParameter parameter) => (ChanceLuck)Math.Sign(_chanceLuck.GetValueOrDefault(parameter));
        public void AddChanceDenial(EntityParameter parameter) => ShiftChanceDenial(parameter, 1);
        public void RemoveChanceDenial(EntityParameter parameter) => ShiftChanceDenial(parameter, -1);
        public bool IsChanceDenied(EntityParameter parameter) => _chanceDenials.GetValueOrDefault(parameter) > 0;
        /// <summary>The pool as it stands before any conversion touched it — deliberately blind to the mark
        /// that empties it, since this is the very measure a conversion takes of what it is taking over.</summary>
        public float GetUnconvertedValueForParameter(EntityParameter parameter) =>
            Resolve(parameter, BaseFor(parameter), [.. ModifiersFor(parameter).Where(modifier => modifier is not PoolConversionModifier)]);

        public void SetBaseValueForParameter(EntityParameter parameter, float baseValue)
        {
            if (!_parameterValues.ContainsKey(parameter)) return;

            Recalculate(parameter, SeededBase(parameter, baseValue), ModifiersFor(parameter));
        }

        public void OnParameterModifiersChange(object? sender, IModifiersChangedEventArgs args)
        {
            if (!_parameterValues.TryGetValue(args.EntityParameter, out (float Base, float Current) value)) return;

            Recalculate(args.EntityParameter, value.Base, args.Modifiers);
        }

        private void ShiftChanceLuck(EntityParameter parameter, int delta) => _chanceLuck[parameter] = _chanceLuck.GetValueOrDefault(parameter) + delta;
        private void ShiftChanceDenial(EntityParameter parameter, int delta) =>
            _chanceDenials[parameter] = Math.Max(0, _chanceDenials.GetValueOrDefault(parameter) + delta);
        private IReadOnlyList<IModifier> ModifiersFor(EntityParameter parameter) => _getModifiersForParameter?.Invoke(parameter) ?? [];
        private float BaseFor(EntityParameter parameter) =>
            _parameterValues.TryGetValue(parameter, out (float Base, float Current) value) ? value.Base : DefaultBase(parameter);

        /// <summary>The one place a parameter's stored value is rebuilt: its number, and whether a
        /// conversion has taken the pool over — both read off the same list, so the mark and the figure can
        /// never fall out of step.</summary>
        private void Recalculate(EntityParameter parameter, float baseValue, IReadOnlyList<IModifier> modifiers)
        {
            _parameterValues[parameter] = (baseValue, Calculations.CalculateFloatValue(modifiers, baseValue));
            MarkConverted(parameter, CarriesConversionMark(modifiers));
            RaiseParameterChanges(parameter);
        }

        private void MarkConverted(EntityParameter parameter, bool converted)
        {
            if (converted) _convertedPools.Add(parameter);
            else _convertedPools.Remove(parameter);
        }

        /// <summary>Whether a conversion has taken this pool over. Walked by hand rather than asked with a
        /// predicate: every modifier change of a fight comes through here, and the closure and enumerator a
        /// query allocates are not worth a single type test.</summary>
        private static bool CarriesConversionMark(IReadOnlyList<IModifier> modifiers)
        {
            for (int index = 0; index < modifiers.Count; index++)
                if (modifiers[index] is PoolConversionModifier { IsDrain: true }) return true;

            return false;
        }

        /// <summary>The formula and then the parameter's own bounds — what a list of modifiers is worth,
        /// without the categorical verdict the indexer adds.
        /// <para>Decorators are asked but count for nothing: every decorator in the game overrides the
        /// value it hands UP the chain and leaves this pass-through alone, so the measure is the modifier
        /// list alone. Deliberate on the one road that uses it — a conversion hands over the pool the
        /// character's own sources built, and a temporary buff riding a decorator does not swell the gift.</para></summary>
        private float Resolve(EntityParameter parameter, float baseValue, IReadOnlyList<IModifier> modifiers) =>
            ApplyBounds(parameter, _moduleManager.GetModule(parameter).ApplyDecoratorsForValue(Calculations.CalculateFloatValue(modifiers, baseValue)));

        /// <summary>What every reader of a parameter is given: a pool a conversion has taken over is
        /// nothing, whatever the numbers under it say, and then the parameter's own bounds. The verdict is
        /// taken here — past the formula and past the decorators — because it is categorical: an emptying
        /// written as a −1 multiplier would share the <c>1 + Σ</c> bucket with every other multiplicative
        /// line on the pool, and a pool that had already been given away would live on at half strength.</summary>
        private float Present(EntityParameter parameter, float value) =>
            ApplyBounds(parameter, _convertedPools.Contains(parameter) ? 0f : value);

        private void RaiseParameterChanges(EntityParameter args) =>
            ParameterChanged?.Invoke(args, this[args]);

        private static float DefaultBase(EntityParameter parameter) => s_defaultBases.GetValueOrDefault(parameter);

        /// <summary>A seeding pass walks the whole enum and names zero for everything its data leaves out,
        /// so zero means "unnamed" and hands the parameter back its default base.</summary>
        private static float SeededBase(EntityParameter parameter, float baseValue) =>
            baseValue == 0f ? DefaultBase(parameter) : baseValue;

        private static float ApplyBounds(EntityParameter parameter, float value) =>
            s_bounds.TryGetValue(parameter, out (float Min, float Max) bounds) ? Mathf.Clamp(value, bounds.Min, bounds.Max) : Mathf.Max(0f, value);
    }
}
