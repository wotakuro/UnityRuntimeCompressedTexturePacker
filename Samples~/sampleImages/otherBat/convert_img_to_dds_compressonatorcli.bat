@echo off
setlocal enabledelayedexpansion
:: compressonatorcliはY判定が出来ないので…

:: --- 設定項目 ---

:: 出力ディレクトリ
set OUTPUT_DIR=dds_converted

:: compressonatorcli.exe へのパス（パスが通っていない場合はフルパスを記載してください）
set COMPRESSONATOR_EXE=compressonatorcli.exe

:: (BC1 / BC3 / BC5 / BC7 ),*DXT1,DXT5
set TEXTURE_FORMAT_TYPE=BC1

:: 圧縮設定 (0.0 - 1.0 *BC7のみ?)
set QUALITY=1.0
:: ----------------

::
if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)


echo Encoding Files....

:: 現在のフォルダ内の .png ファイルをループ処理
for /r %%f in (*.png) do (
    echo Encording : %%f
    
    "%COMPRESSONATOR_EXE%" -fd %TEXTURE_FORMAT_TYPE% -Quality %QUALITY% "%%f" "%OUTPUT_DIR%\\%%~nf_%TEXTURE_FORMAT_TYPE%.dds"
    
    if !errorlevel! equ 0 (
        echo Complete: %%~nf.astc
    ) else (
        echo [Error] %%f 
    )
)

echo.
echo All done
pause