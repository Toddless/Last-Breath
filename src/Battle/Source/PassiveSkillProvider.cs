namespace Battle.Source
{
    using System.Collections.Generic;
    using Core.Battle.Skills;

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
