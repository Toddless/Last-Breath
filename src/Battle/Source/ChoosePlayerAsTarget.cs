namespace Battle.Source
{
    using System.Linq;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class ChoosePlayerAsTarget : ITargetChooser
    {
        public IEntity Choose(List<IEntity> targets) => targets.First(x => x is IPlayer);
    }
}
