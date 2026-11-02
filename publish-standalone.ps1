# Gera o executável único autocontido (win-x64) em dist/standalone/DevSandbox.UI.exe.
# O .exe NÃO é versionado no Git (dist/ e app/ estão no .gitignore): ele é publicado como anexo
# de um GitHub Release. Rode na raiz do repositório.

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$pastaSaida = 'dist/standalone'
$exe = Join-Path $pastaSaida 'DevSandbox.UI.exe'

Write-Host "🚀 Gerando executável único otimizado (apenas o essencial para rodar)..." -ForegroundColor Cyan

# Executável único com compressão e sem símbolos de debug (.pdb)
dotnet publish src/DevSandbox.UI/DevSandbox.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $pastaSaida

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Erro ao compilar o executável." -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not (Test-Path $exe)) {
    Write-Host "`n❌ O publish terminou, mas $exe não foi gerado." -ForegroundColor Red
    exit 1
}

$arquivo = Get-Item $exe
$tamanhoMB = [math]::Round($arquivo.Length / 1MB, 1)

Write-Host "`n✅ Executável gerado:" -ForegroundColor Green
Write-Host "   $($arquivo.FullName) ($tamanhoMB MB)" -ForegroundColor Yellow

Write-Host "`n📦 Roteiro de release (o .exe vai como anexo do GitHub Release, não no Git):" -ForegroundColor White
Write-Host "   1. git tag -a vX.Y.Z -m `"Release vX.Y.Z`"" -ForegroundColor Gray
Write-Host "   2. git push origin vX.Y.Z" -ForegroundColor Gray
Write-Host "   3. gh release create vX.Y.Z $pastaSaida/DevSandbox.UI.exe --title `"DevSandbox vX.Y.Z`" --notes `"<o que mudou>`"" -ForegroundColor Gray
