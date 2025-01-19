using System;
using System.Collections.Generic;
using System.Reflection;

public class ClassMetadata(Type type, List<PropertyInfo> properties)
{
    public Type Type { get; set; } = type;

    public List<PropertyInfo> Properties { get; set; } = properties;

}
