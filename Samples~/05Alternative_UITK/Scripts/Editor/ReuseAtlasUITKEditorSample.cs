using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UTJ.RuntimeCompressedTexturePacker;

namespace UTJ.Sample
{

    /// <summary>
    /// Editorで
    /// </summary>
    public class ReuseAtlasUITKEditorSample : EditorWindow
    {
        // 固定サイズの画像をAtlasを再利用しながら読み込むオブジェクト
        private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

        // アイテムデータリスト
        private List<IconItemData> itemDatas;

        // Atlas画像表示用
        private Image image;
        // ListView 
        ListView listView;

        /// <summary>
        /// EditorWindowの作成
        /// </summary>
        [MenuItem("Samples/RuntimeCompressedTexturePacker/ReuseAtlasUITKEditorSample")]
        public static void Create()
        {
            ReuseAtlasUITKEditorSample.GetWindow<ReuseAtlasUITKEditorSample>();
        }

        /// <summary>
        /// Enable時の処理
        /// </summary>
        private void OnEnable()
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlFilePath);

            VisualElement root = uxmlAsset.Instantiate();
            this.rootVisualElement.Add(root);

            // アイテムセットアップ
            string loadingIconPath;
            string[] iconDataPaths;
            TextureFormat textureFormat;
            IconPathUtility.GetLoadTextureInfo(out iconDataPaths, out loadingIconPath, out textureFormat);

            //  固定サイズの画像をAtlasを再利用しながら読み込むオブジェクトの作成
            this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);

            // setup item
            this.itemDatas = new List<IconItemData>(iconDataPaths.Length);
            foreach (string iconDataPath in iconDataPaths)
            {
                var itemData = new IconItemData(recycleAtlasForFixed, iconDataPath, loadingIconPath);
                this.itemDatas.Add(itemData);
            }
            // UI Setup
            this.listView = root.Q<ListView>("ItemList");
            var atlasInfo = root.Q<Label>("AtlasTextureInfo");
            if (atlasInfo != null)
            {
                atlasInfo.text = "Atlas " + this.recycleAtlasForFixed.texture2D.width + "x" + this.recycleAtlasForFixed.texture2D.height + " " + textureFormat;
            }
            this.image = root.Q<Image>();

            // listViewのイベント登録
            this.listView.bindItem += (item, idx) =>
            {
                this.itemDatas[idx].OnBind();
            };
            this.listView.unbindItem += (item, idx) =>
            {
                this.itemDatas[idx].OnUnbind();
            };

            // listViewにBinding
            this.listView.itemsSource = this.itemDatas;

            EditorApplication.playModeStateChanged += this.OnPlayerStateChanged;
        }

        /// <summary>
        /// プレイ状態が変わった時
        /// </summary>
        /// <param name="stateChange"></param>
        private void OnPlayerStateChanged(PlayModeStateChange stateChange)
        {
            // Player->Editor時にSpriteやTextureが勝手に破棄されるので対応が必要
            if(stateChange == PlayModeStateChange.EnteredEditMode)
            {
                this.itemDatas.Clear();

                // アイテムセットアップ
                string loadingIconPath;
                string[] iconDataPaths;
                TextureFormat textureFormat;
                IconPathUtility.GetLoadTextureInfo(out iconDataPaths, out loadingIconPath, out textureFormat);

                //  固定サイズの画像をAtlasを再利用しながら読み込むオブジェクトの作成
                this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);

                // setup item
                this.itemDatas = new List<IconItemData>(iconDataPaths.Length);
                foreach (string iconDataPath in iconDataPaths)
                {
                    var itemData = new IconItemData(recycleAtlasForFixed, iconDataPath, loadingIconPath);
                    this.itemDatas.Add(itemData);
                }

                // listViewにBinding
                this.listView.itemsSource = this.itemDatas;
            }

        }

        /// <summary>
        /// Disable時の処理
        /// </summary>
        private void OnDisable()
        {
            if(recycleAtlasForFixed != null)
            {
                recycleAtlasForFixed.DestroyTextureImmediate();
                recycleAtlasForFixed.Dispose();
                recycleAtlasForFixed = null;
            }
            EditorApplication.playModeStateChanged -= this.OnPlayerStateChanged;
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {

            // アイテムのアップデート処理（SpriteのRequest及び ロードアイコン回転処理)
            if (itemDatas != null)
            {
                this.recycleAtlasForFixed.SetForceUpdateDirty();
                foreach (var item in itemDatas)
                {
                    item.OnUpdate(Time.deltaTime);
                }
            }
            // Textureのセット
            if (image != null)
            {
                image.image = this.recycleAtlasForFixed.texture2D;
            }
        }

        /// <summary>
        /// UXMLを探して返します
        /// </summary>
        private string UxmlFilePath
        {
            get
            {
                var guids = AssetDatabase.FindAssets("ListItemUI");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith("05Alternative_UITK/UXML/ListItemUI.uxml"))
                    {
                        return path;
                    }
                }
                return "";
            }
        }
    }
}