# Script para gerar executável único autocontido (Single-File Self-Contained)
# O arquivo resultante roda em qualquer computador com Windows 10/11 sem precisar instalar o .NET.

Write-Host "🚀 Gerando executável único (Single-File Standalone)..." -ForegroundColor Cyan

dotnet publish src/DevSandbox.UI/DevSandbox.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/standalone

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Sucesso! Executável único gerado em:" -ForegroundColor Green
    Write-Host "   dist\standalone\DevSandbox.UI.exe" -ForegroundColor Yellow
    Write-Host "`n👉 Você pode copiar e enviar apenas esse arquivo .exe para qualquer pessoa!" -ForegroundColor White
} else {
    Write-Host "`n❌ Ocorreu um erro ao gerar o executável." -ForegroundColor Red
}
