namespace Playground.Script.Passives.Attacks
{
    public abstract class AttackPassives
    {
        public string? Name;
        public string? Description;
        public float? Weight;

        public abstract void OnActivated(AttackComponent attack);

    }
}
