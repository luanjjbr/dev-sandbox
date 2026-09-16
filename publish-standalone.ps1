# Script para gerar o executável único autocontido
# Pega apenas o arquivo executável necessário para rodar e faz uma cópia para a pasta 'app' no diretório principal.

Write-Host "🚀 Gerando executável único otimizado (apenas o essencial para rodar)..." -ForegroundColor Cyan

# 1. Compila gerando apenas o executável único com compressão e sem símbolos de debug (.pdb)
dotnet publish src/DevSandbox.UI/DevSandbox.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o dist/standalone

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Erro ao compilar o executável." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 2. Cria a pasta 'app' no diretório principal (rastreada pelo Git)
$pastaDestino = "app"
if (-not (Test-Path $pastaDestino)) {
    New-Item -ItemType Directory -Path $pastaDestino -Force | Out-Null
}

$exeOrigem = "dist/standalone/DevSandbox.UI.exe"
$exeDestino = "$pastaDestino/DevSandbox.UI.exe"

# 3. Copia apenas o arquivo .exe para a pasta app/
Copy-Item -Path $exeOrigem -Destination $exeDestino -Force

$tamanhoMB = [math]::Round(((Get-Item $exeDestino).Length / 1MB), 1)

Write-Host "`n✅ Executável copiado com sucesso para o diretório principal:" -ForegroundColor Green
Write-Host "   $exeDestino ($tamanhoMB MB)" -ForegroundColor Yellow
Write-Host "`n👉 Agora, quando você rodar o 'git add' e 'git commit', a pasta '$pastaDestino' subirá junto para o GitHub!" -ForegroundColor White
