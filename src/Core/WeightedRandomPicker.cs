namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using Data;
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

        public static HashSet<T> PickRandomMultipleWithoutDuplicate<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, int requestedCount, RandomNumberGenerator rnd)
            where T : class => PickMultiple(elements, requestedCount, () => rnd.RandfRange(0, totalWeight));

        public static HashSet<T> PickRandomMultipleWithoutDuplicate<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, int requestedCount, IRandomNumberGenerator rnd)
            where T : class => PickMultiple(elements, requestedCount, () => rnd.RandFloatRange(0, totalWeight));

        private static HashSet<T> PickMultiple<T>(IEnumerable<WeightedObject<T>> elements, int requestedCount, Func<float> roll)
            where T : class
        {
            HashSet<T> taken = [];
            const int MaxAttempts = 15;
            var toPickFrom = elements.Where(element => element.Weight > 0).ToList();
            if (toPickFrom.Count == 0) return taken;

            for (; requestedCount > 0; requestedCount--)
                if (!TryTakeRandom(MaxAttempts))
                    PickFirstNotTaken();

            return taken;

            bool TryTakeRandom(int attempts)
            {
                while (attempts-- > 0)
                    if (taken.Add(PickByRoll(toPickFrom, roll())))
                        return true;
                return false;
            }

            void PickFirstNotTaken()
            {
                foreach (var element in toPickFrom)
                    if (taken.Add(element.Obj))
                        return;
            }
        }

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
