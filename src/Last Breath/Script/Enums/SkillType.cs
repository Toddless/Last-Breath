namespace Playground.Script.Enums
{
    public enum SkillType
    {
        /// <summary>
        /// Skill type that modifies the attack context before applying an attack
        /// </summary>
        PreAttack,
        /// <summary>
        /// Skill type that applies to attacks
        /// </summary>
        OnAttack,
        /// <summary>
        /// A skill type that works all the time. They need to be activated on earning.
        /// </summary>
        AlwaysActive,
    }
}
