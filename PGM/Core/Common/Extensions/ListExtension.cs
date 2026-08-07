namespace PGM.Core.Common.Extensions;

public static class ListExtension
{
    public static bool IsAny<T>(this List<T>? list) => list is { Count: > 0 };

    public static bool IsAny<T>(this IEnumerable<T>? list) => list is not null && list.Any();
}
