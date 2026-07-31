namespace Core.Modifiers.Conditions
{
    using Data;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// "While I fight in this stance". The ability book announces its visible loadout whenever it
    /// changes, and a stance switch is one of those changes; the predicate re-reads the book on every
    /// announcement and publishes only real flips, so equipping an ability costs a comparison and
    /// nothing else.
    /// </summary>
    public sealed class StanceCondition(Stance stance) : OwnerCondition
    {
        protected override bool Evaluate(bool wasMet) => Owner?.AbilityBook.CurrentStance == stance;

        protected override void Subscribe(IFightable owner) => owner.AbilityBook.ActiveAbilitiesChanged += Reevaluate;

        protected override void Unsubscribe(IFightable owner) => owner.AbilityBook.ActiveAbilitiesChanged -= Reevaluate;
    }

    public class StanceConditionFactory : IConditionFactory
    {
        public string Type => ConditionTypes.Stance;

        public OwnerCondition Create(JObject json) =>
            new StanceCondition(EnumParser.ParseEnum<Stance>(json.Value<string>(ConditionFields.Stance) ?? string.Empty));
    }
}
