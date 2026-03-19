@echo off
setlocal enabledelayedexpansion
cd /d %~dp0

:: --- 設定項目 ---
:: 出力ディレクトリ
set OUTPUT_DIR=dds_converted

:: astcenc.exe へのパス（パスが通っていない場合はフルパスを記載してください）
set TEXCONV_EXE=Texconv.exe

:: Encode format
:: BC1_UNORM , BC3_UNORM , BC7_UNORM
set ENCODE_FORMAT=BC7_UNORM



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
    
    "%TEXCONV_EXE%" -f %ENCODE_FORMAT% "%%f" -o "%OUTPUT_DIR%\\%ENCODE_FORMAT%" -nogpu -y -srgbi -srgbo -vflip
    move /Y "%OUTPUT_DIR%\\%ENCODE_FORMAT%\\%%~nf.dds" "%OUTPUT_DIR%\\%ENCODE_FORMAT%\\%%~nf_%ENCODE_FORMAT%.dds"
    
    if !errorlevel! equ 0 (
        echo Complete: %%~nf.dds
    ) else (
        echo [Error] %%f 
    )
)

echo.
echo All done
pause