#!/bin/bash



# --- 設定項目 ---
# astcenc へのパス（パスが通っていない場合はフルパスを記載してください）
# ※ Linux/macOS環境を想定し、.exe を外しています。必要に応じて変更してください。
ASTCENC_EXE="astcenc"

# ブロックサイズ (例: 4x4, 6x6, 8x8 など)
BLOCK_SIZE="4x4"

# 圧縮設定 (fast, medium, thorough, exhaustive)
QUALITY="-exhaustive"
# ----------------
# 出力フォルダの作成 (存在しない場合は作成)
mkdir -p "astc_converted"

echo "Encoding ...."

# サブディレクトリも含めて .png ファイルを検索してループ処理
# パスにスペースが含まれている場合も安全に処理できるよう print0 を使用しています
find . -type f -name "*.png" -print0 | while IFS= read -r -d $'\0' f; do
    echo "Encoding : $f"
    
    # 拡張子を除いたファイル名を取得 (例: ./dir/image.png -> image)
    filename=$(basename "$f" .png)
    
    # 変換コマンドを実行
    "$ASTCENC_EXE" -cs "$f" "astc_converted/${filename}_${BLOCK_SIZE}.astc" "$BLOCK_SIZE" "$QUALITY" -yflip
    
    # 終了コード ($?) で成否を判定
    if [ $? -eq 0 ]; then
        echo "Complete: ${filename}.astc"
    else
        echo "[Error] $f"
    fi
done

echo ""
echo "All done"

# バッチファイルの pause に相当する処理
# ※Shell Scriptでは省略されることも多いですが、元の挙動に合わせています
read -n 1 -s -r -p "Press any key to continue..."
echo ""