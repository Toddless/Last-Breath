namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract class ComponentBase(Func<float, float, float, Parameter, float> calculateValue) : ObservableObject, IGameComponent
    {
        private Func<float, float, float, Parameter, float> _calculateValue = calculateValue;
        // maybe later can i find another way to update values
        protected Func<float, float, float, Parameter, float> CalculateValues => _calculateValue;

        public abstract void UpdateProperties();
    }
}
