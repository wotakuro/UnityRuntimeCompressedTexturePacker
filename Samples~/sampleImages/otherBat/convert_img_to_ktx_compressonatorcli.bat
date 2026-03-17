@echo off
setlocal enabledelayedexpansion

:: compressonatorcliはY判定が出来ないので…

:: --- 設定項目 ---

:: 出力ディレクトリ
set OUTPUT_DIR=ktx_v1_converted

:: compressonatorcli.exe へのパス（パスが通っていない場合はフルパスを記載してください）
set COMPRESSONATOR_EXE=compressonatorcli.exe

:: テクスチャフォーマット (例:ETC2_RGB/ETC2_RGBA/ETC2_RGBA1)
:: *リニアカラーとしてヘッダーには記載されます
:: *ETCはYLIPの必要あり、しかし
:: その他 (BC1 / BC3 / BC5 / BC7 ),*DXT1,DXT5
set TEXTURE_FORMAT_TYPE=DXT1

:: 圧縮設定 (0.0 - 1.0 *BC7のみ?)
set QUALITY=1.0
:: ----------------

::
if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)
::
if not exist "%OUTPUT_DIR%\\%TEXTURE_FORMAT_TYPE%" (
    mkdir "%OUTPUT_DIR%\\%TEXTURE_FORMAT_TYPE%"
)

echo Encoding Files....

:: 現在のフォルダ内の .png ファイルをループ処理
for /r %%f in (*.png) do (
    echo Encording : %%f
    
    "%COMPRESSONATOR_EXE%" -fd %TEXTURE_FORMAT_TYPE% -Quality %QUALITY% "%%f" "%OUTPUT_DIR%\\%TEXTURE_FORMAT_TYPE%\\%%~nf_%TEXTURE_FORMAT_TYPE%.ktx"
    
    if !errorlevel! equ 0 (
        echo Complete: %%~nf.ktx
    ) else (
        echo [Error] %%f 
    )
)

echo.
echo All done
pause