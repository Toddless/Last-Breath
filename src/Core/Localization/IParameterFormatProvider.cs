namespace Core.Localization
{
    using Enums;

    public enum ParameterUnit
    {
        /// <summary>Displayed as-is: +50 Damage.</summary>
        Number,

        /// <summary>Data stores a fraction (0.5 = 50%): displayed ×100 with a % sign.</summary>
        Percent,
    }

    /// <summary>Display units of entity parameters; source of truth = SharedData/Formatting/ParameterFormats.json.</summary>
    public interface IParameterFormatProvider
    {
        ParameterUnit GetUnit(EntityParameter parameter);
    }
}
