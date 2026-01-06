@echo off
setlocal enabledelayedexpansion

:: --- Settings (Corrected quoting style) ---
set "PROJECT_NAME=Onyx"
set "TEST_FOLDER=..\OnyxTests\sprt"
set "NEW_ENGINE_DIR=%TEST_FOLDER%\NewBuild"
set "OLD_ENGINE_DIR=%TEST_FOLDER%\OldBuild"

set "NEW_ENGINE=%NEW_ENGINE_DIR%\%PROJECT_NAME%.exe"
set "OLD_ENGINE=%OLD_ENGINE_DIR%\%PROJECT_NAME%.exe"

set "FASTCHESS=C:\Users\jacks\source\repos\Onyx\Onyx\Builds\fastchess-windows-x86-64\fastchess.exe"
set "STOCKFISH=Z:\Downloads\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe"
set "BOOK=C:\Users\jacks\source\repos\Onyx\Onyx\Builds\Games\opening.epd"

set ELOS=3 5 7 9 11
set GAMES_PER_LEVEL=12
set TIME_CONTROL=10+0.1
set CONCURRENCY=6


echo [Build]
dotnet publish -c Release
if %ERRORLEVEL% NEQ 0 (
    echo Build Failed!
    pause
    exit /b
)

:: Ensure folders exist
if not exist "%NEW_ENGINE_DIR%" mkdir "%NEW_ENGINE_DIR%"
if not exist "%OLD_ENGINE_DIR%" mkdir "%OLD_ENGINE_DIR%"

:: Move current New build to Old build
if exist "%NEW_ENGINE%" (
    echo [Moving old build to %OLD_ENGINE_DIR%]
    :: Copy all files (including DLLs) from New to Old
    xcopy /y /e "%NEW_ENGINE_DIR%\*" "%OLD_ENGINE_DIR%\" > nul
)

:: Copy fresh build to New folder
echo [Deploying fresh build to %NEW_ENGINE_DIR%]
:: Note: Check your .NET version path (net9.0). Use xcopy to get all dependencies/DLLs
xcopy /y /e ".\bin\Release\net9.0\publish\*" "%NEW_ENGINE_DIR%\" > nul


:: ENGINE V ENGINE
%FASTCHESS% ^
    -engine cmd="%NEW_ENGINE%" name=New_Build ^
    -engine cmd="%OLD_ENGINE%" name=Old_Build ^
    -each tc=10+0.1 -concurrency 1 -repeat -recover ^
    -openings file="%BOOK%" format=epd order=random ^
    -sprt elo0=0 elo1=5 alpha=0.05 beta=0.05 ^
    -rounds 1 ^
    -log file=%TEST_FOLDER%\results.log level=trace engine=true
pause


for %%E in (%ELOS%) do (
    echo.
    echo --- Testing against Stockfish Elo: %%E ---  
    %FASTCHESS% ^
        -engine cmd=%NEW_ENGINE% name=MyEngine ^
        -engine cmd=%STOCKFISH% name=SF_%%E option."Skill Level"=%%E ^
        -each tc=%TIME_CONTROL% ^
        -rounds %GAMES_PER_LEVEL% ^
        -repeat ^
	    -openings file="%BOOK%" format=epd order=random ^
        -concurrency %CONCURRENCY% ^
        -recover        
)

echo.
echo Tournament Finished. Check results.txt and PGN files.
pause

