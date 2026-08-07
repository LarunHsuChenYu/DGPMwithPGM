using PGM.Core.Common.Attributes;

namespace PGM.Core.Application.Models.Enums;

public enum ErrorCodes
{
    // 系統錯誤 (9xxx)
    [MultiDescription("9999", "code")]
    [MultiDescription("未預期錯誤", "message")]
    InternalError = 9999,

    [MultiDescription("9998", "code")]
    [MultiDescription("key 無權限或來源 IP 不允許", "message")]
    Forbidden = 9998,

    // 業務邏輯錯誤
    [MultiDescription("200", "code")]
    [MultiDescription("必要參數缺漏或格式錯誤", "message")]
    InvalidParameter = 200,

    [MultiDescription("100", "code")]
    [MultiDescription("Success", "message")]
    Success = 1,

    // 登入驗證錯誤
    [MultiDescription("200", "code")]
    [MultiDescription("Account not found", "message")]
    AccountNotFound = 2,

    [MultiDescription("300", "code")]
    [MultiDescription("Incorrect password", "message")]
    IncorrectPassword = 3,

    [MultiDescription("400", "code")]
    [MultiDescription("Unauthorized access", "message")]
    UnauthorizedAccess = 4,

    /// <summary>對外統一：帳號不存在／密碼錯／停用（避免枚舉）。</summary>
    [MultiDescription("AUTH_INVALID", "code")]
    [MultiDescription("帳號或密碼錯誤", "message")]
    AuthInvalid = 7,

    [MultiDescription("AUTH_NO_ROLE", "code")]
    [MultiDescription("尚未設定角色，請聯絡管理員", "message")]
    AuthNoRole = 8,

    // 一般資料操作錯誤
    [MultiDescription("404", "code")]
    [MultiDescription("資料不存在", "message")]
    DataNotFound = 5,

    [MultiDescription("409", "code")]
    [MultiDescription("資料重複", "message")]
    DuplicateData = 6,
}
