namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract class ComponentBase : ObservableObject, IGameComponent
    {
        private Func<Parameter, (float, float)> _getModifiers;

        protected ComponentBase(Func<Parameter, (float, float)> getModifiers)
        {
            _getModifiers = getModifiers;
        }

        public virtual void UpdateProperties()
        {

        }

        protected virtual void UpdateProperty(ref float field, float newValue, Action<float> setter)
        {
            if (field != newValue)
            {
                field = newValue;
                setter(field);
            }
        }

        //  this will be called each time some of "Current-" property is needed
        protected float CalculateValues(float baseValue, float AdditionalValue, float increaseModifier, Parameter parameter)
        {
            var (buff, debuff) = _getModifiers.Invoke(parameter);
            return buff * Math.Max(0, debuff) * ((baseValue + AdditionalValue) * increaseModifier);
        }
    }
}
