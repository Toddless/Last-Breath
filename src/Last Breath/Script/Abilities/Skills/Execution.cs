namespace Playground.Script.Abilities.Skills
{
    using Godot;
    using Playground.Script.Enums;

    public class Execution : ISkill
    {
        private ICharacter? _owner;
        private float _threshold;


        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public Texture2D? Icon { get; private set; }

        public SkillType SkillType { get; } = SkillType.AfterAttack;

        public Execution()
        {
            LoadData();
        }


        public void OnLoss()
        {

        }

        public void OnObtaining(ICharacter owner)
        {
            _owner = owner;
        }

        private void LoadData()
        {

        }
    }
}
