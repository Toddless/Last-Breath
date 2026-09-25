namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Entity;

    public class ChoosePlayerAsTarget : ITargetChooser
    {
        public IFightable Choose(List<IFightable> targets) => targets.First(x => x is IPlayer);
    }
}
