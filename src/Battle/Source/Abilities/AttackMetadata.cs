namespace Battle.Source.Abilities
{
    public record AttackMetadata(int Index, int TotalCount)
    {
        public bool IsFirst => Index == 0;
        public bool IsLast => Index == TotalCount - 1;
    }
}
