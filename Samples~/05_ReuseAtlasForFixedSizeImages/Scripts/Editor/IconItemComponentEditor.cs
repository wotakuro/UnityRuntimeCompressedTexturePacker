using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UTJ.RuntimeCompressedTexturePacker;
using UnityEditor;
using UnityEngine.UIElements;

namespace UTJ.Sample
{

    /// <summary>
    /// Listアイテムのコンテナ
    /// </summary>
    [CustomEditor(typeof(IconItemComponent))]
    public class IconItemComponentEditor: Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            IconItemComponent iconItemComponent = (IconItemComponent)target;
            EditorGUILayout.LabelField( iconItemComponent.GetIconName());
        }
    }
}