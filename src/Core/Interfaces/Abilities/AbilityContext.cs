namespace Core.Interfaces.Abilities
{
    using Entity;
    using System.Collections.Generic;

    public struct AbilityContext
    {
        public IEntity Caster { get; set; }
        public List<IEntity> Targets { get; set; }
        public float Damage { get; set; }
        public bool IsCritical { get; set; }

        public Dictionary<string, object> Metadata { get; set; }
    }
}
