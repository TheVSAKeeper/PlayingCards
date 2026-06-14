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
$BlazorImage  = 'thevsakeeper/durak:latest'
$ClassicImage = 'thevsakeeper/durak-classic:latest'
$BlazorPort   = 2180
$ClassicPort  = 2181
$Domain       = 'durak.keep2space.ru'

$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$DeployDir = Join-Path $RepoRoot 'deploy\durak'

function Exec([string]$file, [string[]]$argv) {
    Write-Host "▶ $file $($argv -join ' ')" -ForegroundColor Cyan
    & $file @argv
    if ($LASTEXITCODE -ne 0) { throw "Команда упала (код $LASTEXITCODE): $file $($argv -join ' ')" }
}

$doBlazor  = -not $ClassicOnly
$doClassic = -not $BlazorOnly

Push-Location $RepoRoot
try {
    # 1. Pre-flight
    if (-not $SkipBuild) {
        Exec 'dotnet' @('build', 'PlayingCards.sln', '-c', 'Release', '--nologo')
        if (-not $SkipTests) {
            Exec 'dotnet' @('test', 'PlayingCards.sln', '-c', 'Release', '--nologo')
        }
    }

    # 2. Build образов (контекст — корень репозитория)
    if ($doBlazor) {
        Exec 'docker' @('build', '-t', $BlazorImage,  '-f', 'PlayingCards.Durak.Blazor/Dockerfile', '.')
    }
    if ($doClassic) {
        Exec 'docker' @('build', '-t', $ClassicImage, '-f', 'PlayingCards.Durak.Web/Dockerfile',    '.')
    }

    # 3. Push в Docker Hub (нужен ! docker login под thevsakeeper)
    if ($doBlazor)  { Exec 'docker' @('push', $BlazorImage) }
    if ($doClassic) { Exec 'docker' @('push', $ClassicImage) }

    # 4. Каталог + compose.yaml на vps2 (idempotent, льём каждый раз)
    Exec 'ssh' @($SshAlias, "sudo mkdir -p $RemoteDir && sudo chown `$(whoami) $RemoteDir")
    Exec 'scp' @((Join-Path $DeployDir 'compose.yaml'), "${SshAlias}:$RemoteDir/compose.yaml")

    # 4a. (опц., одноразовое) angie http.d + reload + напоминание про DNS
    if ($Setup) {
        Exec 'scp' @((Join-Path $DeployDir 'angie-durak.conf'), "${SshAlias}:/tmp/durak.conf")
        Exec 'ssh' @($SshAlias, "sudo mv /tmp/durak.conf /etc/angie/http.d/durak.conf && sudo angie -t && sudo systemctl reload angie")
        Write-Host "DNS: заведи A-запись 'durak' -> 2.26.255.227 (reg.ru). Wildcard-cert *.keep2space.ru поддомен покрывает." -ForegroundColor Yellow
    }

    # 5. Cutover
    Exec 'ssh' @($SshAlias, "cd $RemoteDir && docker compose pull && docker compose up -d")

    # 6. Smoke — внутренние порты на vps2, с ожиданием старта (Blazor net10 на 1 vCPU
    # поднимается несколько секунд; без ретраев curl ловит reset сразу после up -d).
    $smokeTpl = 'code=000; for i in $(seq 1 20); do code=$(curl -s -o /dev/null -w "%{http_code}" "URL"); [ "$code" = "200" ] && { echo "LABEL OK 200"; exit 0; }; sleep 1; done; echo "LABEL FAIL last=$code"; exit 1'
    if ($doBlazor) {
        Exec 'ssh' @($SshAlias, $smokeTpl.Replace('URL', "http://127.0.0.1:$BlazorPort/").Replace('LABEL', 'blazor '))
    }
    if ($doClassic) {
        Exec 'ssh' @($SshAlias, $smokeTpl.Replace('URL', "http://127.0.0.1:$ClassicPort/classic").Replace('LABEL', 'classic'))
    }

    Write-Host "Готово: https://$Domain/ (Blazor), https://$Domain/classic (MVC)" -ForegroundColor Green
}
finally {
    Pop-Location
}
