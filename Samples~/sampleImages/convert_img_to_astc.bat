@echo off
setlocal enabledelayedexpansion

::
if not exist "astc_converted" (
    mkdir "astc_converted"
)

:: --- 設定項目 ---
:: astcenc.exe へのパス（パスが通っていない場合はフルパスを記載してください）
set ASTCENC_EXE=astcenc.exe

:: ブロックサイズ (例: 4x4, 6x6, 8x8 など)
set BLOCK_SIZE=4x4

:: 圧縮設定 (fast, medium, thorough, exhaustive)
set QUALITY=-exhaustive
:: ----------------

echo Encoding ASTC....

:: 現在のフォルダ内の .png ファイルをループ処理
for /r %%f in (*.png) do (
    echo Encording : %%f
    
    "%ASTCENC_EXE%" -cs "%%f" "astc_converted\\%%~nf_%BLOCK_SIZE%.astc" %BLOCK_SIZE% %QUALITY% -yflip
    
    if !errorlevel! equ 0 (
        echo Complete: %%~nf.astc
    ) else (
        echo [Error] %%f 
    )
)

echo.
echo All done
pause