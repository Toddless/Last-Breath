namespace Playground.Script.Passives
{
    using Playground.Script.Enums;

    public interface IEffect
    {
        string Name
        {
            get;
        }
        string Description
        {
            get;
        }
        float Modifier
        {
            get;
        }
        Stats Stat
        {
            get;
        }
        EffectType EffectType
        {
            get;
        }
        int Duration
        {
            get;
            set;
        }
    }
}
