@echo off
chcp 65001 >nul
title VibeMovies Desktop — Buscador por Vibe e Rastreador de Filmes
echo ============================================================================
echo   🎬 Iniciando VibeMovies Desktop (WPF Nativo .NET 9.0)
echo ============================================================================

set "EXE_PATH=%~dp0backend\src\VibeMovies.Desktop\bin\Debug\net9.0-windows\VibeMovies.Desktop.exe"

if exist "%EXE_PATH%" (
    echo [OK] Executavel encontrado. Abrindo interface grafica nativa...
    start "" "%EXE_PATH%"
) else (
    echo [INFO] Compilando VibeMovies.Desktop pela primeira vez...
    dotnet build "%~dp0backend\src\VibeMovies.Desktop\VibeMovies.Desktop.csproj" -c Debug
    if exist "%EXE_PATH%" (
        start "" "%EXE_PATH%"
    ) else (
        echo [ERRO] Nao foi possivel localizar ou compilar VibeMovies.Desktop.exe.
        pause
    )
)
