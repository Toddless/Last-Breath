namespace Core.Interfaces.Entity
{
    using System.Collections.Generic;

    public interface IItemEffectsModifier: INpcModifier
    {
      string EffectId { get; }
    }
}
