namespace Core.Entity
{
    public interface IItemEffectsModifier: INpcModifier
    {
      string EffectId { get; }
    }
}
