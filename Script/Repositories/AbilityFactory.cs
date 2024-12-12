namespace Playground.Script.Repositories
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Playground.Script.Passives.Attacks;

    public class AbilityFactory 
    {
        public static Ability CreateAbilities(dynamic abilityData)
        {
            var typeName = abilityData.GetType();

            var abilityType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(o=>o.Name == typeName && typeof(Ability).IsAssignableFrom(o));

            return abilityType == null ? throw new Exception() : (Ability)Activator.CreateInstance(abilityType)!;
        }
    }
}
