namespace Playground.Components
{
    using Playground.Script.Helpers;

    public abstract partial class Attribute : ObservableNode
    {
        private int _total;

        public int Total
        {
            get => _total;
            set
            {
                SetProperty(ref _total, value < 0 ? 0 : value);
            }
        }
    }
}
