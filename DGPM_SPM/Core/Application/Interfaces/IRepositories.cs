using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IUserRepository
{
    /// <summary>供 KPI 資料權限等業務功能查詢啟用中使用者（非 Local Auth 登入）。</summary>
    Task<User?> GetByUserIdAsync(string userId, CancellationToken ct = default);
}

public interface IParameterRepository
{
    Task<IReadOnlyList<Parameter>> GetAllByItemAsync(string setItem, CancellationToken ct = default);
}

public interface IExchangeRateRepository
{
    Task<PagedResult<ExchangeRate>> GetPagedAsync(ExchangeRateFilter filter, CancellationToken ct = default);
    Task<ExchangeRate?> GetByIdAsync(int rateId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string currencyCode, string rateYm, int? excludeRateId = null, CancellationToken ct = default);
    Task<ExchangeRate> AddAsync(ExchangeRate entity, CancellationToken ct = default);
    Task<ExchangeRate?> UpdateAsync(ExchangeRate entity, CancellationToken ct = default);
    Task<ExchangeRate?> SetStatusAsync(int rateId, string status, string modifiedBy, CancellationToken ct = default);
}

public interface IDealerRepository
{
    Task<PagedResult<Dealer>> GetPagedAsync(DealerFilter filter, CancellationToken ct = default);
    Task<Dealer?> GetByIdAsync(int dealerId, CancellationToken ct = default);

    /// <summary>經銷商代碼是否已存在（excludeDealerId 供編輯時排除自身）。</summary>
    Task<bool> ExistsCodeAsync(string dealerCode, int? excludeDealerId = null, CancellationToken ct = default);

    /// <summary>新增經銷商，回傳新的 DEALER_ID。</summary>
    Task<int> AddAsync(Dealer entity, CancellationToken ct = default);

    Task<int> UpdateAsync(Dealer entity, CancellationToken ct = default);
    Task<int> UpdateStatusAsync(Dealer entity, CancellationToken ct = default);

    /// <summary>依代碼取得啟用中的經銷商（KPI 匯入時解析代碼用）。</summary>
    Task<IReadOnlyList<Dealer>> GetActiveByCodesAsync(
        IReadOnlyCollection<string> dealerCodes,
        CancellationToken ct = default);
}

public interface IRegionRepository
{
    Task<IReadOnlyList<Region>> GetActiveAsync(CancellationToken ct = default);
    Task<PagedResult<Region>> GetPagedAsync(RegionFilter filter, CancellationToken ct = default);
    Task<Region?> GetByIdAsync(int regionId, CancellationToken ct = default);
    Task<IReadOnlyList<Region>> GetActiveOptionsAsync(int? excludeRegionId, CancellationToken ct = default);
    Task<bool> ExistsCodeAsync(string regionCode, int? excludeRegionId, CancellationToken ct = default);
    Task<bool> IsDescendantAsync(int regionId, int candidateRegionId, CancellationToken ct = default);
    Task<bool> HasActiveChildrenAsync(int regionId, CancellationToken ct = default);
    Task<bool> HasActiveDealersAsync(int regionId, CancellationToken ct = default);
    Task<int> AddAsync(Region entity, CancellationToken ct = default);
    Task<int> UpdateAsync(Region entity, CancellationToken ct = default);
    Task<int> UpdateStatusAsync(Region entity, CancellationToken ct = default);
}

public interface IKpiUserDataScopeRepository
{
    /// <summary>取得使用者目前的 KPI 資料權限範圍（含 JOIN 區域/經銷商顯示欄位）。</summary>
    Task<IReadOnlyList<KpiUserDataScope>> GetByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>回傳指定 ids 中實際存在於 org.REGION 的 REGION_ID（供驗證授權標的）。</summary>
    Task<IReadOnlyList<int>> GetExistingRegionIdsAsync(IReadOnlyCollection<int> regionIds, CancellationToken ct = default);

    /// <summary>回傳指定 ids 中實際存在於 org.DEALER 的 DEALER_ID（供驗證授權標的）。</summary>
    Task<IReadOnlyList<int>> GetExistingDealerIdsAsync(IReadOnlyCollection<int> dealerIds, CancellationToken ct = default);

    /// <summary>全量覆寫使用者的授權範圍：先刪除既有紀錄，再寫入 scopes。呼叫端須管理交易。</summary>
    Task ReplaceByUserIdAsync(string userId, IReadOnlyCollection<KpiUserDataScope> scopes, CancellationToken ct = default);
}

public interface IKpiIndicatorRepository
{
    Task<PagedResult<KpiIndicator>> GetPagedAsync(KpiIndicatorFilter filter, CancellationToken ct = default);
    Task<KpiIndicator?> GetByIdAsync(int indicatorId, CancellationToken ct = default);

    /// <summary>指標代碼是否已存在（excludeIndicatorId 供編輯時排除自身）。</summary>
    Task<bool> ExistsByCodeAsync(string indicatorCode, int? excludeIndicatorId = null, CancellationToken ct = default);

    Task<KpiIndicator> AddAsync(KpiIndicator entity, CancellationToken ct = default);
    Task<KpiIndicator?> UpdateAsync(KpiIndicator entity, CancellationToken ct = default);
    Task<KpiIndicator?> SetStatusAsync(int indicatorId, string status, string modifiedBy, CancellationToken ct = default);

    /// <summary>依代碼取得啟用中的 KPI 指標（KPI 匯入時解析代碼用）。</summary>
    Task<IReadOnlyList<KpiIndicator>> GetActiveByCodesAsync(
        IReadOnlyCollection<string> indicatorCodes,
        CancellationToken ct = default);
}

public interface IKpiImportRepository
{
    /// <summary>新增匯入批次，回傳新的 BATCH_ID。</summary>
    Task<long> AddBatchAsync(KpiImportBatch entity, CancellationToken ct = default);

    /// <summary>回寫批次結果（狀態、筆數統計、錯誤摘要、結束時間）。</summary>
    Task<int> UpdateBatchResultAsync(KpiImportBatch entity, CancellationToken ct = default);

    Task<KpiImportBatch?> GetBatchByIdAsync(long batchId, CancellationToken ct = default);

    /// <summary>分頁查詢匯入批次（與「KPI 匯入日誌查詢」共用）。</summary>
    Task<PagedResult<KpiImportBatch>> GetBatchPagedAsync(
        KpiImportBatchFilter filter,
        CancellationToken ct = default);

    /// <summary>取得指定年月既有的 KPI 數據（供匯入判斷新增/更新/鎖定）。</summary>
    Task<IReadOnlyList<KpiData>> GetDataByPeriodAsync(string periodYm, CancellationToken ct = default);

    /// <summary>新增 KPI 數據，回傳新的 DATA_ID。</summary>
    Task<long> AddDataAsync(KpiData entity, CancellationToken ct = default);

    /// <summary>更新既有 KPI 數據的數值與批次資訊。</summary>
    Task<int> UpdateDataValueAsync(KpiData entity, CancellationToken ct = default);

    /// <summary>新增 KPI 異動紀錄（匯入留痕 ACTION_TYPE = 'I'）。</summary>
    Task<long> AddChangeLogAsync(KpiChangeLog entity, CancellationToken ct = default);
}

public interface IKpiChangeLogRepository
{
    /// <summary>分頁查詢 KPI 異動紀錄（含經銷商 / 指標名稱），供系統資料查詢使用。</summary>
    Task<PagedResult<KpiChangeLog>> GetPagedAsync(KpiChangeLogFilter filter, CancellationToken ct = default);
}

public interface IKpiDataRepository
{
    /// <summary>分頁查詢 KPI 數據（含經銷商 / 指標名稱），供覆核作業使用。</summary>
    Task<PagedResult<KpiData>> GetPagedAsync(KpiDataFilter filter, CancellationToken ct = default);

    Task<KpiData?> GetByIdAsync(long dataId, CancellationToken ct = default);

    /// <summary>更新覆核狀態並記錄覆核人 / 時間，回傳影響筆數。</summary>
    Task<int> UpdateReviewStatusAsync(long dataId, string reviewStatus, string reviewUser, CancellationToken ct = default);

    /// <summary>寫入 KPI 異動紀錄（覆核 R / 解鎖 U 留痕），回傳新的 LOG_ID。</summary>
    Task<long> AddChangeLogAsync(KpiChangeLog entity, CancellationToken ct = default);
}
