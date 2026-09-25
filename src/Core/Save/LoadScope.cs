namespace Core.Save
{
    using System;

    /// <summary>Read side of <see cref="LoadScope"/>: systems check this to mute gameplay
    /// notifications (level-up toasts, etc.) while a save is being applied.</summary>
    public interface ILoadScope
    {
        bool IsLoading { get; }
    }

    public class LoadScope : ILoadScope
    {
        private int _depth;
        public bool IsLoading => _depth > 0;

        public IDisposable Begin()
        {
            _depth++;
            return new Ender(this);
        }

        private sealed class Ender(LoadScope owner) : IDisposable
        {
            private bool _disposed;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                owner._depth--;
            }
        }
    }
}
