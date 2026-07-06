namespace Core.Interfaces.Skills
{
    public interface ISkillProvider
    {
        ISkill? CreateSkill(string id);
    }
}
