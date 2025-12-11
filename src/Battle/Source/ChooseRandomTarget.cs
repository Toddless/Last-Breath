namespace Battle.Source
{
    using Godot;
    using Services;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;

    public class ChooseRandomTarget : ITargetChooser
    {
        public IEntity Choose(List<IEntity> targets)
        {
            var rnd = GameServiceProvider.Instance.GetService<RandomNumberGenerator>();

            return targets[rnd.RandiRange(0, targets.Count - 1)];
        }
    }
}
