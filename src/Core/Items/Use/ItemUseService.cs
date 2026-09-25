namespace Core.Items.Use
{
    using System.Collections.Generic;
    using System.Linq;

    public class ItemUseService(IEnumerable<IItemUseBehavior> behaviors) : IItemUseService
    {
        private readonly List<IItemUseBehavior> _behaviors = behaviors.ToList();

        public IItemUseBehavior? BehaviorFor(IItem item) =>
            _behaviors.FirstOrDefault(behavior => behavior.CanHandle(item));
    }
}
