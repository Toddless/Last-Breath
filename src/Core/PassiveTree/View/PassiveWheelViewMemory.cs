namespace Core.PassiveTree.View
{
    using Session;

    /// <summary>
    /// Where the wheel was last left standing. The window is a fresh instance every time it opens, so the
    /// zoom and the pan a player worked his way to have to live outside it — long enough to close the
    /// wheel, do something else and come back to the same corner of the tree, and no longer: this is not
    /// saved, and a new playthrough opens on the whole tree again.
    /// <para>Spread is deliberately not here: it is the author's decision about the layout and arrives
    /// with the document, so remembering it would let a reader overrule the file.</para>
    /// </summary>
    public sealed class PassiveWheelViewMemory : ISessionResettable
    {
        private float _zoom;
        private float _panX;
        private float _panY;

        /// <summary>Nobody has left a view here yet, so the wheel opens fitted to its tree.</summary>
        public bool IsEmpty { get; private set; } = true;

        public void Remember(CanvasTransform view)
        {
            _zoom = view.Zoom;
            _panX = view.PanX;
            _panY = view.PanY;
            IsEmpty = false;
        }

        /// <summary>Puts the remembered view back and says whether there was one to put back. The bounds
        /// are the transform's own, so a stored zoom is clamped exactly as a wheel click is.</summary>
        public bool Restore(CanvasTransform view)
        {
            if (IsEmpty) return false;

            view.Restore(_zoom, _panX, _panY);
            return true;
        }

        public void ResetSession() => IsEmpty = true;
    }
}
