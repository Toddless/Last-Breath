namespace Playground.Script.Repositories
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Playground.Script.Passives;

    public class AbilityFactory 
    {
        public static IAbility CreateAbilities(dynamic abilityData)
        {
            var typeName = abilityData.GetType();

            var abilityType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(o=>o.Name == typeName && typeof(IAbility).IsAssignableFrom(o));

            return abilityType == null ? throw new Exception() : (IAbility)Activator.CreateInstance(abilityType)!;
        }
    }
}
