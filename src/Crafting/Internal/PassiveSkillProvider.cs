namespace Crafting.Internal
{
    using System.Collections.Generic;
    using System.IO;
    using Core.Interfaces.Skills;
    using Godot;
    using Utilities;

    public class PassiveSkillProvider
    {
        private const string SkillDataPath = "res://TestResources/Skills/";
        private Dictionary<string, ISkill> _skills = [];

        public static PassiveSkillProvider? Instance { get; private set; }

        public PassiveSkillProvider()
        {
            Instance = this;
            LoadData();
        }

        public ISkill? CreateSkill(string id)
        {
            if (!_skills.TryGetValue(id, out var def))
            {
                Tracker.TrackNotFound($"Skill definition with id: {id}", this);
                return null;
            }

            return def.Copy();
        }

        private void LoadData()
        {
            var skillData = ResourceLoader.ListDirectory(SkillDataPath);

            foreach (var data in skillData)
            {
                var file = Path.Combine(SkillDataPath, data);

                if (string.IsNullOrWhiteSpace(file)) continue;
                if (!file.EndsWith(".tres")) continue;
                var loaded = ResourceLoader.Load(file);
                if (loaded is ISkill skill) _skills.Add(skill.Id, skill);

            }

        }
    }
}
