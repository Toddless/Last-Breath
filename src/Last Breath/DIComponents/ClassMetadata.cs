using System;
using System.Collections.Generic;
using System.Reflection;

public class ClassMetadata(Type type, List<FieldInfo> fields)
{
    public Type Type { get; set; } = type;

    public List<FieldInfo> Fields { get; set; } = fields;

}
