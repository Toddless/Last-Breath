namespace Utilities
{
    using Godot;
    using System;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using System.Collections.Generic;

    public class WeightedRandomPicker
    {

        public static (List<WeightedObject<T>> WeightedObjects, float TotalWeight) CalculateWeights<T>(IEnumerable<T> objects)
            where T : IWeighable
        {
            List<WeightedObject<T>> weights = [];
            float currentMaxWeight = 0;
            float from = 0;
            foreach (var obj in objects)
            {
                currentMaxWeight += obj.Weight;
                weights.Add(new(obj, from, currentMaxWeight, obj.Weight));
                from = currentMaxWeight;
              //  DebugLogger.LogDebug($"Percent: {MathF.Round(obj.Weight / currentMaxWeight * 100),2}% object: {obj.GetType().Name}");
            }
            return (weights, currentMaxWeight);
        }

        public static T PickRandom<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, RandomNumberGenerator rnd)
            where T : class
        {
            var rNumb = rnd.RandfRange(0, totalWeight);
            return elements.First(x => rNumb >= x.From && rNumb < x.To).Obj;
        }

        public static IEnumerable<T> PickRandomMultiple<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, int requestedCount, RandomNumberGenerator rnd)
            where T : class
        {
            HashSet<T> taken = [];
            const int maxAttemps = 15;

            while (requestedCount > 0)
            {
                TryTakeOne(maxAttemps);

                void TryTakeOne(int attemp)
                {
                    while (attemp > 0)
                    {
                        if (taken.Add(PickRandom(elements, totalWeight, rnd))) return;
                        attemp--;
                    }
                    PickAnyCallBack(taken, elements);
                }
                requestedCount--;
            }

            return taken;
        }

        private static void PickAnyCallBack<T>(HashSet<T> taken, IEnumerable<WeightedObject<T>> elements)
            where T : class
        {
            foreach (var mod in elements)
                if (taken.Add(mod.Obj)) break;
        }
    }
}
