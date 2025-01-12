namespace Playground.Script.Attribute
{
    using System;

    public interface IAttribute
    {
        event Action OnAttributeChanged;

        void NotifyChange();
    }
}
