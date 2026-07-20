namespace Core.Battle.Abilities
{
    using Skills;

    /// <summary>Effect factory for item grants: maps an effect id to a constructor fed by numeric
    /// properties from item JSON (mirror of <see cref="Skills.ISkillProvider"/> for effects).
    /// A missing property refuses the effect loudly instead of constructing a mis-tuned instance.</summary>
    public interface IGrantEffectProvider
    {
        IEffect? CreateEffect(string id, SkillProperties properties);
    }
}
