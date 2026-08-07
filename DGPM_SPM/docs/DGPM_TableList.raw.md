## TableList

| 項 | 資料表名稱 | 資料表中文名稱 | 備註 |
| --- | --- | --- | --- |
| 1 | SysFun | 系統功能設定檔 |  |
| 2 |  |  |  |

## SysFun

| 返回 | 系統名稱：DGPM System |  | 轉入頻率：不定時 |  |  |  |  | 保留期限：永久 |  |  |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
|  | 英文檔案名稱：SysFun |  | 提供者：介面輸入 |  |  |  |  | 中文檔案名稱：系統功能設定檔 |  |  |
| 序 | 英文欄位 | 中文欄位 | 資料型態 | 長度 | 主鍵 | 副鍵 | 允許NULL | 資料描述 |  |  |
| 1 | Fun_ID | 功能代碼 | varchar | 20 | 1 |  |  | 主鍵 / 功能代碼，新增後不能修改 |  |  |
| 2 | Fun_Name | 功能名稱 | nvarchar | 50 |  |  |  | 功能名稱（如 系統功能管理） |  |  |
| 3 | Parent_ID | 上層選單功能代碼 | varchar | 20 |  |  | Y | 上層選單 ID（頂層選單填 0 or NULL，如 PERMISSION） |  |  |
| 4 | Action_Type | 功能類型 | char | 1 |  |  |  | 功能類型 M (標題)、P (頁面)、B (按鈕) |  |  |
| 5 | Url_Path | 前端路由或 URL | nvarchar | 50 |  |  | Y | 前端路由或 URL（如 PERMISSION 或 PERMISSION/DealerList） |  |  |
| 6 | Icon | 選單圖示代碼 | nvarchar | 50 |  |  | Y | 選單圖示代碼（如 MasIcon, SecIcon），暫不設定及使用 |  |  |
| 7 | Sort_Order | 階層序號 | decimal | (6,2) |  |  |  | 排序（同階層下的顯示順序，如 1.1, 1.2, 1.3 ,2.1, 2.2） |  |  |
| 8 | Is_Menu | 選單否 | char | 1 |  |  |  | 是否顯示於選單（Y/N) |  |  |
| 9 | Is_Enabled | 啟用否 | char | 1 |  |  |  | 是否啟動（Y/N)，預設為N |  |  |
| 10 | Fun_Desc | 說明 | nvarchar | 500 |  |  | Y | 功能說明 |  |  |
| 11 | Del_YN | 刪除否 | char | 1 |  |  |  | 是否刪除（Y/N)，預設為N |  |  |
| 12 | Cre_Person | 建立人員 | nvarchar | 50 |  |  |  | 建立人員ID (或程式名稱) |  |  |
| 13 | Cre_Date | 建立日期 | datetime |  |  |  |  | 建立日期 |  |  |
| 14 | Chg_Person | 修改人員 | nvarchar | 50 |  |  |  | 修改人員ID (或程式名稱) |  |  |
| 15 | Chg_Date | 修改日期 | datetime |  |  |  |  | 修改日期 |  |  |
