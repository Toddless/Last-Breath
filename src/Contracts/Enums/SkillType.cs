namespace Core.Enums
{
    public enum SkillType
    {
        /// <summary>
        /// Skill type that modifies the attack context
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
        /// <summary>
        /// A skill type that works when we getting an attack
        /// </summary>
        GettingAttack,
    }
}
