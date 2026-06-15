#requires -Version 7
<#
.SYNOPSIS
  Деплой обеих версий «Дурака» на vps2: Blazor (durak.keep2space.ru/) и classic MVC (durak.keep2space.ru/classic).

.DESCRIPTION
  Pre-flight (dotnet build/test) -> docker build обоих образов -> push в Docker Hub ->
  sync compose.yaml на vps2 -> docker compose pull && up -d -> smoke внутренних портов.
  Интерактив за человеком: ! docker login  (аккаунт thevsakeeper).
  Первый деплой запускать с -Setup (зальёт angie http.d/durak.conf и напомнит про DNS).

.EXAMPLE
  pwsh tools/deploy/deploy-durak.ps1                 # полный цикл (build+test+push+cutover+smoke)
  pwsh tools/deploy/deploy-durak.ps1 -Setup          # + залить angie-конфиг и reload, напомнить про DNS
  pwsh tools/deploy/deploy-durak.ps1 -SkipTests      # без dotnet test
  pwsh tools/deploy/deploy-durak.ps1 -SkipBuild      # быстрый повтор: сразу docker build/push
  pwsh tools/deploy/deploy-durak.ps1 -BlazorOnly     # только новый фронт
  pwsh tools/deploy/deploy-durak.ps1 -ClassicOnly    # только старый фронт
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,    # пропустить dotnet build/test (быстрый повтор)
    [switch]$SkipTests,    # build делать, dotnet test пропустить
    [switch]$BlazorOnly,   # собрать/задеплоить только Blazor
    [switch]$ClassicOnly,  # собрать/задеплоить только classic MVC
    [switch]$Setup         # одноразовое: залить angie http.d/durak.conf + reload, напомнить про DNS
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --- Координаты vps2 (канон — ops/; при желании вынести в ops/deploy.config.ps1) -----
$SshAlias     = 'vps2'
$RemoteDir    = '/opt/durak'
$BlazorRepo   = 'thevsakeeper/durak'
$ClassicRepo  = 'thevsakeeper/durak-classic'
$BlazorPort   = 2180
$ClassicPort  = 2181
$Domain       = 'durak.keep2space.ru'

$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$DeployDir = Join-Path $RepoRoot 'deploy\durak'

# Иммутабельный тег для отката: short-SHA (+ -dirty при незакоммиченных правках в дереве).
$GitSha = (& git -C $RepoRoot rev-parse --short HEAD).Trim()
if (& git -C $RepoRoot status --porcelain) { $GitSha = "$GitSha-dirty" }
$BlazorTags  = @("${BlazorRepo}:latest",  "${BlazorRepo}:$GitSha")
$ClassicTags = @("${ClassicRepo}:latest", "${ClassicRepo}:$GitSha")

# Идентификатор сборки для UI (Blazor показывает его в «Настройках» — удостовериться, что фронт обновился).
$BuildDate  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm') + ' UTC'
$BlazorArgs = @("BUILD_VERSION=$GitSha", "BUILD_DATE=$BuildDate")

function Exec([string]$file, [string[]]$argv) {
    Write-Host "▶ $file $($argv -join ' ')" -ForegroundColor Cyan
    & $file @argv
    if ($LASTEXITCODE -ne 0) { throw "Команда упала (код $LASTEXITCODE): $file $($argv -join ' ')" }
}

# Сборка образа сразу под все теги (latest + SHA) одним проходом — контекст снимается один раз.
function DockerBuild([string[]]$tags, [string]$dockerfile, [string[]]$buildArgs = @()) {
    $argv = @('build')
    foreach ($t in $tags) { $argv += @('-t', $t) }
    foreach ($a in $buildArgs) { $argv += @('--build-arg', $a) }
    $argv += @('-f', $dockerfile, '.')
    Exec 'docker' $argv
}

function DockerPush([string[]]$tags) {
    foreach ($t in $tags) { Exec 'docker' @('push', $t) }
}

# Внешний smoke публичного пути (DNS -> angie -> TLS -> контейнер). curl.exe есть в Windows 10+.
function SmokeExternal([string]$url, [string]$label) {
    $PSNativeCommandUseErrorActionPreference = $false  # non-zero exit curl на старте не должен ронять цикл
    Write-Host "▶ external smoke ${label}: $url" -ForegroundColor Cyan
    for ($i = 1; $i -le 20; $i++) {
        $code = (& curl.exe -s -o NUL -w '%{http_code}' --max-time 5 $url 2>$null)
        if ($code -eq '200') { Write-Host "$label OK 200" -ForegroundColor Green; return }
        Start-Sleep -Seconds 1
    }
    throw "$label FAIL: $url не вернул 200 (проверь DNS/angie/cert)"
}

$doBlazor  = -not $ClassicOnly
$doClassic = -not $BlazorOnly

Push-Location $RepoRoot
try {
    # 1. Pre-flight
    if (-not $SkipBuild) {
        # RestoreLockedMode: pre-flight падает, если packages.lock.json разошёлся с csproj/props (как и Docker-restore).
        # У dotnet build нет ключа --locked-mode (это опция restore) — задаётся MSBuild-свойством.
        Exec 'dotnet' @('build', 'PlayingCards.sln', '-c', 'Release', '--nologo', '-p:RestoreLockedMode=true')
        if (-not $SkipTests) {
            Exec 'dotnet' @('test', 'PlayingCards.sln', '-c', 'Release', '--nologo')
        }
    }

    # 2. Build образов (контекст — корень репозитория)
    if ($doBlazor) {
        DockerBuild $BlazorTags  'PlayingCards.Durak.Blazor/Dockerfile' $BlazorArgs
    }
    if ($doClassic) {
        DockerBuild $ClassicTags 'PlayingCards.Durak.Web/Dockerfile'
    }

    # 3. Push в Docker Hub (нужен ! docker login под thevsakeeper) — оба тега: latest и SHA
    if ($doBlazor)  { DockerPush $BlazorTags }
    if ($doClassic) { DockerPush $ClassicTags }

    # 4. Каталог + compose.yaml на vps2 (idempotent, льём каждый раз)
    Exec 'ssh' @($SshAlias, "sudo mkdir -p $RemoteDir && sudo chown `$(whoami) $RemoteDir")
    Exec 'scp' @((Join-Path $DeployDir 'compose.yaml'), "${SshAlias}:$RemoteDir/compose.yaml")

    # 4a. (опц., одноразовое) angie http.d + reload + напоминание про DNS
    if ($Setup) {
        Exec 'scp' @((Join-Path $DeployDir 'angie-durak.conf'), "${SshAlias}:/tmp/durak.conf")
        Exec 'ssh' @($SshAlias, "sudo mv /tmp/durak.conf /etc/angie/http.d/durak.conf && sudo angie -t && sudo systemctl reload angie")
        Write-Host "DNS: заведи A-запись 'durak' -> 2.26.255.227 (reg.ru). Wildcard-cert *.keep2space.ru поддомен покрывает." -ForegroundColor Yellow
    }

    # 5. Cutover — только затронутые сервисы (при -BlazorOnly/-ClassicOnly второй не пересоздаётся)
    $services = @()
    if ($doBlazor)  { $services += 'durak' }
    if ($doClassic) { $services += 'durak-classic' }
    $svc = $services -join ' '
    Exec 'ssh' @($SshAlias, "cd $RemoteDir && docker compose pull $svc && docker compose up -d $svc")

    # 6. Smoke — внутренние порты на vps2, с ожиданием старта (Blazor net10 на 1 vCPU
    # поднимается несколько секунд; без ретраев curl ловит reset сразу после up -d).
    $smokeTpl = 'code=000; for i in $(seq 1 20); do code=$(curl -s -o /dev/null -w "%{http_code}" "URL"); [ "$code" = "200" ] && { echo "LABEL OK 200"; exit 0; }; sleep 1; done; echo "LABEL FAIL last=$code"; exit 1'
    if ($doBlazor) {
        Exec 'ssh' @($SshAlias, $smokeTpl.Replace('URL', "http://127.0.0.1:$BlazorPort/").Replace('LABEL', 'blazor '))
    }
    if ($doClassic) {
        Exec 'ssh' @($SshAlias, $smokeTpl.Replace('URL', "http://127.0.0.1:$ClassicPort/classic").Replace('LABEL', 'classic'))
    }

    # 7. Внешний smoke — публичный путь целиком (внутренние smoke выше проверяют только контейнер)
    if ($doBlazor)  { SmokeExternal "https://$Domain/"        'ext-blazor ' }
    if ($doClassic) { SmokeExternal "https://$Domain/classic" 'ext-classic' }

    Write-Host "Готово: https://$Domain/ (Blazor), https://$Domain/classic (MVC); теги: latest + $GitSha" -ForegroundColor Green
}
finally {
    Pop-Location
}
