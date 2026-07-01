namespace Core.Interfaces.Components
{
    public interface IModifierHandlerComponent
    {
        public void Add<T>(T modifier);
        public void Remove<T>(T modifier);
        public void Apply<T>(T context);
    }
}
