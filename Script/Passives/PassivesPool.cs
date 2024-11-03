namespace Playground.Script.Passives
{
    using System.Collections.Generic;
    using Playground.Script.Passives.Attacks;

    public abstract class PassivesPool
    {

        private static List<Passive> _attackPassives =
            [
            new VampireStrike(),
            new BuffAttack(),
            new AdditionalAttack(),
            ];



        public static void TotalWeight()
        {
        }
    }
}
