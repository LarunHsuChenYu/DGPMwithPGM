@echo off
REM =============================================================================
REM grant-inetpub-logs.bat
REM 以「系統管理員」身分執行：建立 C:\inetpub\logs 並授與寫入權，讓 NLog 可寫入
REM   DGPM_SPM_{api|web}_yyyy-MM-dd.log / PGM_{api|web}_yyyy-MM-dd.log
REM   nlog-internal-*.log
REM 完成後請重啟相關 IIS App Pool（或回收站台），再確認 log 是否出現。
REM =============================================================================
setlocal

net session >nul 2>&1
if errorlevel 1 (
  echo [ERROR] 請以系統管理員身分執行本腳本（右鍵 → 以系統管理員身分執行）。
  exit /b 1
)

set "LOGDIR=C:\inetpub\logs"

if not exist "%LOGDIR%" (
  echo [INFO] 建立 %LOGDIR% ...
  mkdir "%LOGDIR%"
  if errorlevel 1 (
    echo [ERROR] 無法建立 %LOGDIR%
    exit /b 1
  )
)

echo [INFO] 授與 IIS_IUSRS 修改權...
icacls "%LOGDIR%" /grant "IIS_IUSRS:(OI)(CI)M" /T
if errorlevel 1 (
  echo [WARN] IIS_IUSRS 授權失敗（本機若未安裝 IIS 可忽略）。
)

echo [INFO] 授與 Users 修改權（本機 Debug / 互動登入用）...
icacls "%LOGDIR%" /grant "Users:(OI)(CI)M" /T
if errorlevel 1 (
  echo [ERROR] Users 授權失敗。
  exit /b 1
)

echo.
echo [OK] 已處理 %LOGDIR%
echo      請重啟 DGPM_SPM / PGM 的 IIS App Pool，或重新啟動本機 Api／Web。
echo      驗證：應出現 DGPM_SPM_api_*.log / PGM_api_*.log 與 nlog-internal-*.log
echo      若仍無檔：開啟 nlog-internal-*.log 查看權限／路徑錯誤訊息。
exit /b 0
