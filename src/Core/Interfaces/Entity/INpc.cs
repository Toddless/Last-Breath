namespace Core.Interfaces.Entity
{
    using Components;

    public interface INpc
    {
        public IEntityAttribute Attribute { get; }
    }
}
