namespace Playground.Script.Repositories
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Playground.Script.Passives.Attacks;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public static class AbilitiesLoader
    {
        public static List<Ability> LoadAbility(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            var yaml = File.ReadAllText(filePath);
            var deserealizer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            var rawAbilities = deserealizer.Deserialize<List<Ability>>(yaml);

            return rawAbilities.Select(AbilityFactory.CreateAbilities).ToList();
        }
    }
}
