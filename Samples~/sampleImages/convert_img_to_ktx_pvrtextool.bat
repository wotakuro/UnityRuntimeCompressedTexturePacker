@echo off
setlocal enabledelayedexpansion
cd /d %~dp0

:: --- 設定項目 ---
:: 出力ディレクトリ
set OUTPUT_DIR=ktx_converted

:: astcenc.exe へのパス（パスが通っていない場合はフルパスを記載してください）
set PVRTexToolCLI_EXE=PVRTexToolCLI.exe

:: Encode format
:: ASTC_4X4, ETC2_RGB, ETC2_RGBA, ETC2_RGB_A1
set ENCODE_FORMAT=ASTC_4X4

:: Encode quality
:: etcfast, etcnormal, etcslow,
:: astcthorough,astcexhaustive,
set ENCODE_QUALITY=etcslow


:: ----------------
if not exist %OUTPUT_DIR% (
    mkdir %OUTPUT_DIR%
)
if not exist "%OUTPUT_DIR%\\%ENCODE_FORMAT%" (
    mkdir "%OUTPUT_DIR%\\%ENCODE_FORMAT%"
)


echo Encoding ....

:: 現在のフォルダ内の .png ファイルをループ処理
for /r %%f in (*.png) do (
    echo Encording : %%f
    
    "%PVRTexToolCLI_EXE%" -i "%%f" -o "%OUTPUT_DIR%\\%ENCODE_FORMAT%\\%%~nf_%ENCODE_FORMAT%.ktx" -f %ENCODE_FORMAT%,UBN,sRGB -flip y -ics sRGB
    
    if !errorlevel! equ 0 (
        echo Complete: %%~nf.astc
    ) else (
        echo [Error] %%f 
    )
)

echo.
echo All done
pause