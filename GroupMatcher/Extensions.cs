using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupMatcher;
public static class Extensions
{
    public static StringBuilder AppendCsvLine(this StringBuilder stringBuilder, IEnumerable<string> contents)
    {
        return stringBuilder.AppendLine(String.Join(";", contents));
    }

    public static StringBuilder AppendCsvLine<T>(this StringBuilder stringBuilder, IEnumerable<T> contents)
    {
        return stringBuilder.AppendCsvLine(contents.Select(c => c == null ? "" : c.ToString() ?? ""));
    }
}
