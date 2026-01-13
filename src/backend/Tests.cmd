
@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo EXECUTANDO TESTES .
echo ==========================================

set ROOT=%cd%
set RESULTS=%ROOT%\TestResults

echo.
echo ROOT: %ROOT%
echo RESULTS: %RESULTS%
echo.

REM --- Limpar TestResults ---
if exist "%RESULTS%" (
    echo Limpando pasta TestResults...
    rmdir /s /q "%RESULTS%"
)

mkdir "%RESULTS%"

REM --- Executar testes ---
echo Executando testes...

dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults


REM --- Gerar relatorio ---
echo.
echo Gerando relatório de cobertura...

reportgenerator -reports:".\TestResults\**\coverage.cobertura.xml" -targetdir:".\TestResults" -reporttypes:TextSummary

echo.
echo Finalizado com sucesso
echo Relatorio disponivel em: %RESULTS%

endlocal
pause
