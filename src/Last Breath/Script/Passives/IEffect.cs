namespace Playground.Script.Passives
{
    using System.Collections.Generic;

    public interface IEffect
    {
        List<string>? Properties
        {
            get;
            set;
        }

        float? Modifier
        {
            get;
            set;
        }
    }
}
