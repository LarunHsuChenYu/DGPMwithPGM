using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DGPM_SPM.Core.Common.Attributes;

namespace DGPM_SPM.Core.Common.Extensions;

public static class EnumExtension
{
    public static string GetEnumDisplayName(this Enum enumType)
        => enumType.GetType().GetMember(enumType.ToString())
            .FirstOrDefault()
            ?.GetCustomAttribute<DisplayAttribute>()
            ?.Name ?? string.Empty;

    public static string GetEnumDescription(this Enum enumType)
        => enumType.GetType().GetMember(enumType.ToString())
            .FirstOrDefault()
            ?.GetCustomAttribute<DisplayAttribute>()
            ?.Description ?? string.Empty;

    public static int GetEnumValue(this Enum enumType) => Convert.ToInt32(enumType);

    public static string ToUnderlyingString<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var underlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(typeof(TEnum)));
        return underlyingValue?.ToString() ?? string.Empty;
    }

    public static T ConvertFromString<T>(this string strValue) where T : struct
    {
        if (typeof(Enum) != typeof(T).BaseType) return default;
        return (T)Enum.Parse(typeof(T), strValue);
    }

    /// <summary>取得 MultiDescription 屬性 (依 category 分類)</summary>
    public static string GetDescription(this Enum value, string category = "中文")
    {
        var field = value.GetType().GetField(value.ToString());
        var attributes = field?.GetCustomAttributes(typeof(MultiDescriptionAttribute), false)
                               .Cast<MultiDescriptionAttribute>();

        return attributes?.FirstOrDefault(a => a.Category == category)?.Description
               ?? value.ToString();
    }

    public static IEnumerable<MultiDescriptionAttribute> GetAllDescriptions(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttributes(typeof(MultiDescriptionAttribute), false)
                    .Cast<MultiDescriptionAttribute>() ?? Enumerable.Empty<MultiDescriptionAttribute>();
    }
}
