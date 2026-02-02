namespace Utilities
{
    using Godot;
    using Core.Data;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;

    public abstract class WeightedRandomPicker
    {
        // We are fine until we have fewer than 10k elements in the list.
        public static (List<WeightedObject<T>> WeightedObjects, float TotalWeight) CalculateWeights<T>(IEnumerable<T> objects)
            where T : IWeightable
        {
            List<WeightedObject<T>> weightedObjs = [];
            float currentMaxWeight = 0;
            float from = 0;
            foreach (var obj in objects)
            {
                currentMaxWeight += obj.Weight;
                weightedObjs.Add(new(obj, from, currentMaxWeight, obj.Weight));
                from = currentMaxWeight;
              //  DebugLogger.LogDebug($"Percent: {MathF.Round(obj.Weight / currentMaxWeight * 100),2}% object: {obj.GetType().Name}");
            }
            return (weightedObjs, currentMaxWeight);
        }

        public static T PickRandom<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, RandomNumberGenerator rnd)
            where T : class
        {
            var rNumb = rnd.RandfRange(0, totalWeight);
            return elements.First(x => rNumb >= x.From && rNumb < x.To).Obj;
        }

        public static HashSet<T> PickRandomMultipleWithoutDuplicate<T>(IEnumerable<WeightedObject<T>> elements, float totalWeight, int requestedCount, RandomNumberGenerator rnd)
            where T : class
        {
            HashSet<T> taken = [];
            const int MaxAttempts = 15;
            var toPickFrom = elements.ToList();
            while (requestedCount > 0)
            {
                TryTakeOne(MaxAttempts);

                void TryTakeOne(int attempts)
                {
                    while (attempts > 0)
                    {
                        if (taken.Add(PickRandom(toPickFrom, totalWeight, rnd))) return;
                        attempts--;
                    }
                    PickAnyCallBack(taken, toPickFrom);
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
