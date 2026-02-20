namespace Battle.Source
{
    using Core.Interfaces.Skills;
    using System.Collections.Generic;

    public class PassiveSkillProvider
    {
        private const string DataPath = "res://Source/Data/";
        private Dictionary<string, ISkill> _passiveSkills = new();

        public ISkill GetSkill(string id)
        {
            return _passiveSkills[id];
        }


        public void LoadData()
        {

        }
    }
}
