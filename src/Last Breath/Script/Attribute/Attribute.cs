namespace Playground.Script.Attribute
{
    using Playground.Script.Helpers;

    public abstract class Attribute : ObservableObject
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
