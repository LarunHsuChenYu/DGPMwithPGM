using Dapper;
using PGM.Core.Application.Interfaces;
using PGM.Core.Domain.Entities;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

/// <summary>
/// 選單與角色授權用功能清單（讀取 dbo.SET_FUNCTION）。
/// 欄位別名對齊既有 SysFun 實體（Fun_ID／Url_Path…）供 Dapper／Mapperly 沿用。
/// </summary>
public class MenuRepository : IMenuRepository
{
    private readonly IDbSession _session;

    public MenuRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<SysFun>> GetMenuByUserIdAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT
                   F.FUNCTION_ID   AS Fun_ID,
                   F.FUNCTION_NAME AS Fun_Name,
                   F.PARENT_ID     AS Parent_ID,
                   F.ACTION_TYPE   AS Action_Type,
                   F.FUNCTION_URL  AS Url_Path,
                   CAST(F.SORT_ID AS decimal(18,0)) AS Sort_Order,
                   F.IS_MENU       AS Is_Menu,
                   F.IS_ENABLED    AS Is_Enabled,
                   F.SYSTEM_CODE   AS System_Code,
                   CASE WHEN F.DEL_FLG = 0 THEN 'N' ELSE 'Y' END AS Del_YN
            FROM dbo.[SET_FUNCTION] AS F
            INNER JOIN dbo.MAP_ROLE_FUNCTION AS MRF
                ON MRF.FUNCTION_ID = F.FUNCTION_ID
            INNER JOIN dbo.MAP_USER_ROLE AS MUR
                ON MUR.ROLE_ID = MRF.ROLE_ID
            INNER JOIN dbo.DIM_ROLE AS R
                ON R.ROLE_ID = MUR.ROLE_ID
               AND R.DEL_FLG = 0
            WHERE MUR.USER_ID = @UserId
              AND F.DEL_FLG = 0
              AND F.IS_MENU = 'Y'
              AND F.IS_ENABLED = 'Y'
            ORDER BY Sort_Order, Fun_ID;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { UserId = userId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        var result = await conn.QueryAsync<SysFun>(cmd);
        return result.ToList();
    }

    public async Task<IReadOnlyList<SysFun>> GetMenuByRoleIdAsync(
        string roleId,
        string? systemCode = null,
        CancellationToken ct = default)
    {
        // 授權列 ∪ 任一已授權子項之父模組 M（父層未勾選亦可顯示）
        const string sql = """
            ;WITH AuthLeaves AS
            (
                SELECT F.FUNCTION_ID,
                       F.FUNCTION_NAME,
                       F.PARENT_ID,
                       F.ACTION_TYPE,
                       F.FUNCTION_URL,
                       F.SORT_ID,
                       F.IS_MENU,
                       F.IS_ENABLED,
                       F.SYSTEM_CODE,
                       F.DEL_FLG
                FROM dbo.[SET_FUNCTION] AS F
                INNER JOIN dbo.MAP_ROLE_FUNCTION AS MRF
                    ON MRF.FUNCTION_ID = F.FUNCTION_ID
                INNER JOIN dbo.DIM_ROLE AS R
                    ON R.ROLE_ID = MRF.ROLE_ID
                   AND R.DEL_FLG = 0
                WHERE MRF.ROLE_ID = @RoleId
                  AND F.DEL_FLG = 0
                  AND F.IS_ENABLED = 'Y'
                  AND (@SystemCode IS NULL OR F.SYSTEM_CODE = @SystemCode)
            ),
            MenuRows AS
            (
                SELECT *
                FROM AuthLeaves
                WHERE IS_MENU = 'Y'

                UNION

                SELECT P.FUNCTION_ID,
                       P.FUNCTION_NAME,
                       P.PARENT_ID,
                       P.ACTION_TYPE,
                       P.FUNCTION_URL,
                       P.SORT_ID,
                       P.IS_MENU,
                       P.IS_ENABLED,
                       P.SYSTEM_CODE,
                       P.DEL_FLG
                FROM dbo.[SET_FUNCTION] AS P
                WHERE P.DEL_FLG = 0
                  AND P.IS_MENU = 'Y'
                  AND P.IS_ENABLED = 'Y'
                  AND P.ACTION_TYPE = 'M'
                  AND (@SystemCode IS NULL OR P.SYSTEM_CODE = @SystemCode)
                  AND EXISTS (
                      SELECT 1
                      FROM AuthLeaves AS A
                      WHERE A.PARENT_ID = P.FUNCTION_ID
                  )
            )
            SELECT DISTINCT
                   FUNCTION_ID   AS Fun_ID,
                   FUNCTION_NAME AS Fun_Name,
                   PARENT_ID     AS Parent_ID,
                   ACTION_TYPE   AS Action_Type,
                   FUNCTION_URL  AS Url_Path,
                   CAST(SORT_ID AS decimal(18,0)) AS Sort_Order,
                   IS_MENU       AS Is_Menu,
                   IS_ENABLED    AS Is_Enabled,
                   SYSTEM_CODE   AS System_Code,
                   CASE WHEN DEL_FLG = 0 THEN 'N' ELSE 'Y' END AS Del_YN
            FROM MenuRows
            ORDER BY Sort_Order, Fun_ID;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new
            {
                RoleId = roleId,
                SystemCode = string.IsNullOrWhiteSpace(systemCode) ? null : systemCode.Trim()
            },
            _session.CurrentTransaction,
            cancellationToken: ct);
        var result = await conn.QueryAsync<SysFun>(cmd);
        return result.ToList();
    }

    public async Task<IReadOnlyList<SysFun>> GetAllActiveAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT F.FUNCTION_ID   AS Fun_ID,
                   F.FUNCTION_NAME AS Fun_Name,
                   F.PARENT_ID     AS Parent_ID,
                   F.ACTION_TYPE   AS Action_Type,
                   F.FUNCTION_URL  AS Url_Path,
                   CAST(F.SORT_ID AS decimal(18,0)) AS Sort_Order,
                   F.IS_MENU       AS Is_Menu,
                   F.IS_ENABLED    AS Is_Enabled,
                   F.SYSTEM_CODE   AS System_Code,
                   CASE WHEN F.DEL_FLG = 0 THEN 'N' ELSE 'Y' END AS Del_YN,
                   F.CRT_USER      AS Cre_Person,
                   F.CRT_DATE      AS Cre_Date,
                   ISNULL(F.MDF_USER, F.CRT_USER) AS Chg_Person,
                   ISNULL(F.MDF_DATE, F.CRT_DATE) AS Chg_Date
            FROM dbo.[SET_FUNCTION] AS F
            WHERE F.DEL_FLG = 0
              AND F.IS_ENABLED = 'Y'
            ORDER BY F.SORT_ID, F.FUNCTION_ID;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            transaction: _session.CurrentTransaction,
            cancellationToken: ct);
        var result = await conn.QueryAsync<SysFun>(cmd);
        return result.ToList();
    }
}
