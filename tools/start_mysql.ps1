# Chạy MySQL 8 riêng cho đồ án trên cổng 3307, không cần quyền admin.
# Dữ liệu demo riêng nằm ở tools\mysql\data; không dùng data directory của máy.
$binCandidates = @(
    'C:\Program Files\MySQL\MySQL Server 8.0\bin',
    'C:\Program Files\MySQL\MySQL Server 8.4\bin'
)
$fromPath = Get-Command mysqld.exe -ErrorAction SilentlyContinue
if ($fromPath) { $binCandidates = @((Split-Path -Parent $fromPath.Source)) + $binCandidates }
$bin = $null
foreach ($candidate in ($binCandidates | Select-Object -Unique)) {
    $server = Join-Path $candidate 'mysqld.exe'
    if (-not (Test-Path $server)) { continue }
    $version = (& $server --version 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -eq 0 -and $version -match '\bVer\s+8\.\d+(?:\.|\s)') {
        $bin = $candidate
        break
    }
}
if (-not $bin) {
    Write-Error 'Không tìm thấy MySQL Server 8 mysqld.exe. Cài MySQL Server 8 hoặc thêm đúng thư mục bin vào PATH. XAMPP/MariaDB không được dùng làm máy chủ tự khởi động này.'
    exit 2
}
$data = Join-Path $PSScriptRoot 'mysql\data'
if (-not (Test-Path (Join-Path $data 'mysql'))) {
    & "$bin\mysqld.exe" --no-defaults --initialize-insecure --datadir="$data" --console
}
& "$bin\mysqld.exe" --no-defaults --datadir="$data" --port=3307 --bind-address=127.0.0.1 `
    --mysqlx=OFF --character-set-server=utf8mb4 --collation-server=utf8mb4_vietnamese_ci --console
