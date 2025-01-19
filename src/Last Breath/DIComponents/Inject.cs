using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Method)]
public class Inject : Attribute
{
}
