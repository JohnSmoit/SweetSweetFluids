using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


public static class ReflectionUtils {
    public static string Stringify(object o) {
        Type oType = o.GetType();
        List<FieldInfo> fields = new(oType.GetFields(BindingFlags.Public | BindingFlags.Instance));

        StringBuilder b = new();
        b.AppendLine("{");

        foreach (FieldInfo f in fields) {
            if (f.GetType().IsSubclassOf(typeof(ValueType)) || f.GetType().IsSubclassOf(typeof(System.Collections.IEnumerable))) {
                b.AppendLine($"\t{f.Name}: {f.GetValue(o)}");
            } else {
                b.AppendLine($"\t{Stringify(f.GetValue(o))}");
            }
        }

        b.AppendLine("}");

        return b.ToString();
    }

    public static Array ExtractRange<T>(this T[] arr, Type fieldType, string fieldName, int startIndex = 0, int count = -1) {
        throw new NotImplementedException("FUCK");
    }
}