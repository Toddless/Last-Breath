namespace Core.Interfaces.Entity
{
    public interface IItemEffectsModifier: INpcModifier
    {
      string EffectId { get; }
    }
}
