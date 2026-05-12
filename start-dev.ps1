# start-dev.ps1
# Chạy đồng thời API (.NET) và FE (Vite) trong cùng 1 cửa sổ PowerShell.
# Yêu cầu: PostgreSQL đã chạy + ConnectionStrings:DefaultConnection trong src/Krik.Api/appsettings.json đúng.
# Lần đầu sẽ tự `npm install` cho fekrik nếu chưa có node_modules.
# Ngưng bằng Ctrl+C — script sẽ kill cả 2 job.

param(
    [string]$FekrikPath = "..\fekrik"
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$fekrik = Resolve-Path (Join-Path $here $FekrikPath)

Write-Host "[krik] API   : $here" -ForegroundColor Cyan
Write-Host "[krik] FE    : $fekrik" -ForegroundColor Cyan

if (-not (Test-Path (Join-Path $fekrik "node_modules"))) {
    Write-Host "[krik] node_modules chưa có. Chạy npm install ..." -ForegroundColor Yellow
    Push-Location $fekrik
    npm install
    Pop-Location
}

$apiJob = Start-Job -Name "krik-api" -ScriptBlock {
    param($cwd)
    Set-Location $cwd
    dotnet run --project src/Krik.Api
} -ArgumentList $here

$feJob = Start-Job -Name "krik-fe" -ScriptBlock {
    param($cwd)
    Set-Location $cwd
    npm run dev
} -ArgumentList $fekrik.Path

Write-Host "[krik] API job=$($apiJob.Id), FE job=$($feJob.Id). Nhấn Ctrl+C để dừng." -ForegroundColor Green
Write-Host "[krik] Swagger: http://localhost:5207/swagger" -ForegroundColor Green
Write-Host "[krik] FE     : http://localhost:5173" -ForegroundColor Green

try {
    while ($apiJob.State -eq 'Running' -or $feJob.State -eq 'Running') {
        Receive-Job -Job $apiJob -Keep:$false | ForEach-Object { "[api] $_" } | Write-Host
        Receive-Job -Job $feJob  -Keep:$false | ForEach-Object { "[fe ] $_" } | Write-Host -ForegroundColor DarkGray
        Start-Sleep -Milliseconds 500
    }
}
finally {
    Write-Host "[krik] Đang dừng các job ..." -ForegroundColor Yellow
    Stop-Job -Job $apiJob, $feJob -ErrorAction SilentlyContinue
    Remove-Job -Job $apiJob, $feJob -Force -ErrorAction SilentlyContinue
}
