namespace LastBreath.Script.Items.ItemData
{
    public class ItemStats
    {
        #region  Strong Essences
        public int Dexterity { get; set; } = 0;
        public int Strength { get; set; } = 0;
        public int Intelligence { get; set; } = 0;
        public int AllAttribute { get; set; } = 0;
        #endregion

        #region Weak or Regular Essences
        public int Movespeed { get; set; } = 0;
        public int CritDamage { get; set; } = 0;
        public float Health { get; set; } = 0;
        public float Armor { get; set; } = 0;
        public float Suppress { get; set; } = 0;
        public float EnergyBarrier { get; set; } = 0;
        public float SpellDamage { get; set; } = 0;
        public float Resource { get; set; } = 0;
        public float ResourceRecovery { get; set; } = 0;
        public float CritChance { get; set; } = 0;
        public float Damage { get; set; } = 0;
        public float Evade { get; set; } = 0;
        public float AdditionalHitChance { get; set; } = 0;
        public float MaxEnergyBarrier { get; set; } = 0;
        public float MaxEvadeChance { get; set; } = 0;
        public float MaxHealth { get; set; } = 0;
        public float MaxReduceDamage { get; set; } = 0;
        #endregion

        #region Powerfull Essences
        public float ExtraHealth { get; set; } = 0;
        public float ExtraArmor { get; set; } = 0;
        public float ExtraEnergyBarrier { get; set; } = 0;
        public float ExtraEvade { get; set; } = 0;
        public float ExtraDamage { get; set; } = 0;
        public float ExtraSuppress { get; set; } = 0;
        public float ExtraSpellDamage { get; set; } = 0;
        #endregion
    }
}
