namespace Battle.Source
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Godot;

    public class ChooseRandomTarget(IGameServiceProvider provider) : ITargetChooser
    {
        public IFightable Choose(List<IFightable> targets)
        {
            var rnd = provider.GetService<RandomNumberGenerator>();

            return targets[rnd.RandiRange(0, targets.Count - 1)];
        }
    }
}
