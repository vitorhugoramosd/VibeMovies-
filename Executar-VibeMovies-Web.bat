@echo off
chcp 65001 >nul
title VibeMovies Web - API + SPA em http://localhost:5150
setlocal

set "ROOT=%~dp0"
set "FRONTEND=%ROOT%frontend"
set "API_PROJ=%ROOT%backend\src\VibeMovies.Api\VibeMovies.Api.csproj"
set "API_OUT=%ROOT%backend\src\VibeMovies.Api\bin\Release\net9.0"

echo ============================================================================
echo   VibeMovies Web (API .NET 9 + React SPA) - http://localhost:5150
echo ============================================================================

if not exist "%FRONTEND%\dist\index.html" (
    echo [INFO] frontend\dist nao encontrado. Gerando build do frontend...
    where npm >nul 2>nul
    if errorlevel 1 (
        echo [AVISO] npm nao encontrado no PATH. A API sobe sem a interface web ^(so /api e /swagger^).
    ) else (
        pushd "%FRONTEND%"
        call npm install
        if errorlevel 1 ( echo [ERRO] npm install falhou. & popd & pause & exit /b 1 )
        call npm run build
        if errorlevel 1 ( echo [ERRO] npm run build falhou. & popd & pause & exit /b 1 )
        popd
    )
)

echo [INFO] Compilando a API...
dotnet build "%API_PROJ%" -c Release --nologo -v q
if errorlevel 1 ( echo [ERRO] Falha ao compilar a API. & pause & exit /b 1 )

echo [INFO] Abrindo o navegador em instantes...
start "" cmd /c "timeout /t 4 /nobreak >nul & start http://localhost:5150"

echo [INFO] API rodando em http://localhost:5150 (Swagger: /swagger). Feche esta janela ou pressione Ctrl+C para parar.
pushd "%API_OUT%"
dotnet VibeMovies.Api.dll --urls http://localhost:5150 --Frontend:DistPath "%FRONTEND%\dist"
popd
endlocal
