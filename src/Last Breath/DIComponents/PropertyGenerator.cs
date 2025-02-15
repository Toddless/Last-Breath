namespace Playground.DIComponents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    public static class PropertyGenerator
    {
        public static List<string> GetAllFields(Type type)
        {
            var fields = new List<string>();
            CollectFields(type, fields, "");
            return fields;
        }

        private static void CollectFields(Type type, List<string> fields, string parentPath)
        {
            foreach (var field in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x=>x.GetCustomAttribute<Changeable>() != null))
            {
                string fullPath = string.IsNullOrEmpty(parentPath)
                    ? field.Name
                    : $"{field.Name}";

                if (IsSimpleType(field.PropertyType))
                {
                    fields.Add(fullPath);
                }
                else
                {
                    CollectFields(field.PropertyType, fields, fullPath);
                }
            }
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(int)
                || type.IsEnum;
        }
    }
}
