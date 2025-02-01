namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract class ComponentBase : ObservableObject, IGameComponent
    {
        private Func<float, float, float, Parameter, float> _calculateValue;
        // maybe later can i find another way to update values
        protected Func<float, float, float, Parameter, float> CalculateValues => _calculateValue;

        protected ComponentBase(Func<float, float, float, Parameter, float> calculateValue)
        {
            _calculateValue = calculateValue;
        }

        public abstract void UpdateProperties();
    }
}
