namespace Core.Battle.Skills
{
    public interface ISkillProvider
    {
        ISkill? CreateSkill(string id);
    }
}
