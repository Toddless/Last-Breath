namespace Core.Battle.Skills
{
    public interface ISkillProvider
    {
        ISkill? CreateSkill(string id);

        /// <summary>Item grants pass their JSON-sourced numbers here. Providers with fixed presets
        /// (resource-based) keep the default and ignore the properties.</summary>
        ISkill? CreateSkill(string id, RecordProperties properties) => CreateSkill(id);
    }
}
