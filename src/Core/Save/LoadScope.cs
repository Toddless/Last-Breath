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
        public bool IsLoading { get; private set; }

        public IDisposable Begin()
        {
            IsLoading = true;
            return new Ender(this);
        }

        private sealed class Ender(LoadScope owner) : IDisposable
        {
            public void Dispose() => owner.IsLoading = false;
        }
    }
}
