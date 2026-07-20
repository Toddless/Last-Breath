namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Entity.Components;
    using Interfaces;
    using Godot;

    public abstract class WeightedRandomPicker
    {
        public static (List<WeightedObject<T>> WeightedObjects, float TotalWeight) CalculateWeights<T>(IEnumerable<T> objects)
            where T : IWeightable
        {
            List<WeightedObject<T>> weightedObjs = [];
            float currentMaxWeight = 0;
            float from = 0;
            foreach (var obj in objects)
            {
                // A negative weight would move the cumulative ranges backwards and silently mask other entries.
                currentMaxWeight += Mathf.Max(0f, obj.Weight);
                weightedObjs.Add(new(obj, from, currentMaxWeight, obj.Weight));
                from = currentMaxWeight;
            }
            return (weightedObjs, currentMaxWeight);
        }

        public static T PickRandom<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, RandomNumberGenerator rnd)
            where T : class => PickByRoll(elements, rnd.RandfRange(0, totalWeight));

        public static T PickRandom<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, IRandomNumberGenerator rnd)
            where T : class => PickByRoll(elements, rnd.RandFloatRange(0, totalWeight));

        // Rolls sit in [0, totalWeight] with BOTH ends reachable, while every range is [From, To):
        // a roll equal to totalWeight belongs to the last pickable (weight > 0) entry.
        private static T PickByRoll<T>(IEnumerable<WeightedObject<T>> elements, float roll)
            where T : class
        {
            WeightedObject<T>? lastPickable = null;
            foreach (var element in elements)
            {
                if (roll >= element.From && roll < element.To) return element.Obj;
                if (element.Weight > 0) lastPickable = element;
            }

            return lastPickable != null
                ? lastPickable.Obj
                : throw new InvalidOperationException("Cannot pick from a weighted list with no pickable (weight > 0) entries.");
        }
    }
}
