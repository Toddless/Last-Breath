namespace Core.Interfaces.Skills
{
    public interface ISkillProvider
    {
        ISkill? GetSkill(string id);
    }
}
