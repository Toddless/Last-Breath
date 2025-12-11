namespace Core.Enums
{
    public enum SkillType : byte
    {
        /// <summary>
        /// A skill type that modifies the attack context
        /// </summary>
        BeforeAttack,
        /// <summary>
        /// A skill type that applies to attacks
        /// </summary>
        AfterAttack,
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
