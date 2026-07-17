namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;

    public partial class AbilityProvider
    {
        /// <summary>
        /// Id → constructor. Base values, targeting and every abilityProperties key come from the
        /// data record itself (auto-registered into the parameter set); per-ability defaults live in
        /// the ability's RegisterBaseParameters as RegisterDefault fallbacks.
        /// </summary>
        private readonly Dictionary<string, Func<AbilityBaseData, IAbility>> _abilityFactories = new()
        {
            ["Ability_Ice_Shards"] = data => new IceShards.IceShards(data),
            ["Ability_Series_Of_Attacks"] = data => new SeriesOfAttacks.SeriesOfAttacks(data),
            ["Ability_Poison_Explosion"] = data => new PoisonExplosion.PoisonExplosion(data),
            ["Ability_Poison_Coating"] = data => new PoisonCoating.PoisonCoating(data),
            ["Ability_Jar_Of_Poison"] = data => new JarOfPoison.JarOfPoison(data),
            ["Ability_Increasing_Pressure"] = data => new IncreasingPressure.IncreasingPressure(data),
            ["Ability_Dark_Shroud"] = data => new DarkShroud.DarkShroud(data),
            ["Ability_Head_Butt"] = data => new HeadButt.HeadButt(data),
            ["Ability_Ice_Block"] = data => new IceBlock.IceBlocks(data),
            ["Ability_Overload"] = data => new Overload.Overload(data),
            ["Ability_Chain_Lightning"] = data => new ChainLightning.ChainLightning(data),
            ["Ability_Ice_Aegis"] = data => new IceAegis.IceAegis(data),
            ["Ability_Armageddon"] = data => new Armageddon.Armageddon(data),
            ["Ability_Porcupine"] = data => new Porcupine.Porcupine(data),
            ["Ability_Sacrifice"] = data => new Sacrifice.Sacrifice(data),
            ["Ability_Berserk_Fury"] = data => new BerserkFury.BerserkFury(data),
            ["Ability_Ares_Blessing"] = data => new AresBlessing.AresBlessing(data),
            ["Ability_Double_Strike"] = data => new DoubleStrike.DoubleStrike(data),
            ["Ability_Critical_Calculation"] = data => new CriticalCalculation.CriticalCalculation(data),
            ["Ability_Twin_Assist_Attack"] = data => new TwinAssist.TwinAssistAttack(data),
            ["Ability_Twin_Assist_Shield"] = data => new TwinAssist.TwinAssistShield(data),
            ["Ability_Summon_Bone_Wolves"] = data => new Summon.SummonBoneWolves(data)
        };
    }
}
