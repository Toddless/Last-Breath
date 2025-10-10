namespace Core.Interfaces.UI
{
    using System;

    public interface IClosable
    {
        event Action? Close;
    }
}
