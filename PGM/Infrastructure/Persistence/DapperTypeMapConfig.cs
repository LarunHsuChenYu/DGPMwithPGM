using Dapper;
using PGM.Core.Domain.Entities;

namespace PGM.Infrastructure.Persistence;

/// <summary>
/// 讓 Dapper 自動把 SQL 的 SCREAMING_SNAKE_CASE 欄位對應到 Domain Entity 的 PascalCase 屬性。
///
/// 對應規則：忽略底線與大小寫。例如：
///   SQL 欄位 "SEQ_NO"        → 屬性 "SeqNo"
///   SQL 欄位 "EMP_NO"        → 屬性 "EmpNo"
///
/// 呼叫時機：Program.cs 啟動時呼叫 Register() 一次即可。
/// </summary>
public static class DapperTypeMapConfig
{
    private static readonly Type[] MappedTypes =
    {
        typeof(User),
        typeof(Role),
        typeof(SysFun),
        typeof(Parameter),
        typeof(ParamItem),
        typeof(AuthenticationLog),
    };

    public static void Register()
    {
        foreach (var type in MappedTypes)
        {
            // CustomPropertyTypeMap 的委派宣告為不可 null，但 Dapper 實際接受 null（表示欄位不對應）
            SqlMapper.SetTypeMap(type, new CustomPropertyTypeMap(type, ResolveProperty!));
        }
    }

    private static System.Reflection.PropertyInfo? ResolveProperty(Type type, string columnName)
    {
        var normalized = columnName.Replace("_", string.Empty);
        return type.GetProperties()
                   .FirstOrDefault(p => string.Equals(p.Name, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
