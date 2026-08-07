using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using DGPM_SPM.Web.Models;

namespace DGPM_SPM.Web.Services;

/// <summary>
/// 以 protected browser session storage 保存登入後的 AuthSession（JWT + 使用者 + 選單）。
/// Scoped（每 circuit 一份），circuit 內以 in-memory 快取避免重複 JS interop。
/// </summary>
public class AuthTokenStore
{
    private const string StorageKey = "dgpm_spm_auth_session";

    private readonly ProtectedSessionStorage _storage;
    private AuthSession? _cached;
    private bool _loaded;

    public AuthTokenStore(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public async Task<AuthSession?> GetAsync()
    {
        if (_loaded)
            return _cached;

        try
        {
            var result = await _storage.GetAsync<AuthSession>(StorageKey);
            _cached = result.Success ? result.Value : null;
            _loaded = true;

            if (_cached is not null && _cached.IsExpired)
            {
                _cached = null;
                await _storage.DeleteAsync(StorageKey);
            }
        }
        catch (InvalidOperationException)
        {
            // prerender 階段尚無 JS interop；視為未登入，不快取，待 circuit 建立後重讀。
            return null;
        }
        catch (CryptographicException)
        {
            // Data Protection key 已輪替或 storage 內容毀損：視為未登入。
            _cached = null;
            _loaded = true;
        }

        return _cached;
    }

    public async Task SetAsync(AuthSession session)
    {
        _cached = session;
        _loaded = true;
        await _storage.SetAsync(StorageKey, session);
    }

    public async Task ClearAsync()
    {
        _cached = null;
        _loaded = true;

        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch (InvalidOperationException)
        {
            // prerender 階段無 JS interop，僅清除記憶體狀態即可。
        }
    }
}
