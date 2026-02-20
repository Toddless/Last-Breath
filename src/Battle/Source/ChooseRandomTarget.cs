namespace Battle.Source
{
    using Godot;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces.Battle;

    public class ChooseRandomTarget(IGameServiceProvider provider) : ITargetChooser
    {
        public IEntity Choose(List<IEntity> targets)
        {
            var rnd = provider.GetService<RandomNumberGenerator>();

            return targets[rnd.RandiRange(0, targets.Count - 1)];
        }
    }
}
