using System.Collections;
using UTJ.RuntimeCompressedTexturePacker;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UI;

#if RCTP_DEVMODE

namespace UTJ.RuntimeCompressedTexturePacker.Editor
{

    /// <summary>
    /// ファイルリストを渡して自動的にAtlasを生成してくれます
    /// </summary>
    public class RecycleAtlasDebugWindow : EditorWindow
    {
        [MenuItem("Tools/RuntimeCompressedTexturePacker/RecycleAtlasDebugWindow")]
        public static void Create()
        {
            RecycleAtlasDebugWindow.GetWindow<RecycleAtlasDebugWindow>();
        }
        VisualElement itemView;
        Label currentLoadingLabel;
        Label currentState;
        Label currentOrder;
        Label readStatus;

        Foldout loadingQueueFoldout;
        Foldout requestedFiles;
        private void OnEnable()
        {
            this.itemView = new VisualElement();
            this.itemView.style.marginLeft = 5;
            this.rootVisualElement.Add(itemView);

            this.SetupLable(this.itemView, "CurrentLoad", out currentLoadingLabel);
            this.SetupLable(this.itemView, "CurrentState", out currentState);
            this.SetupLable(this.itemView, "ReadStatues", out readStatus);
            this.SetupLable(this.itemView, "CurrentOrder", out currentOrder);

            loadingQueueFoldout = new Foldout();
            loadingQueueFoldout.text = "Loading Queue";
            this.itemView.Add(loadingQueueFoldout);
            loadingQueueFoldout.value = false;


            requestedFiles = new Foldout();
            requestedFiles.text = "RequestedFiles";
            this.itemView.Add(requestedFiles);
        }

        private void SetupLable(VisualElement parent,string name,out Label label)
        {
            VisualElement visualElement = new VisualElement();
            visualElement.style.flexDirection = FlexDirection.Row;
            var nameLabel = new Label(name);
            nameLabel.style.width = 200;
            visualElement.Add(nameLabel);
            label = new Label();
            visualElement.Add(label);
            label.style.marginLeft = 5;

            parent.Add(visualElement);

        }

        private void Update()
        {
            var instance = RecycleAtlasForFixedSizeImages.Instance;
            itemView.visible = (instance != null);
            if (instance != null)
            {
                this.currentLoadingLabel.text = System.IO.Path.GetFileName(instance.CurrentFile);
                this.currentState.text = instance.State.ToString();
                this.currentOrder.text = instance.CurrentOder.ToString();
                this.readStatus.text = instance.readStatus.ToString();

                SetupLoadQueue(instance);
                SetupRequestFiles(instance);
            }
        }

        void SetupLoadQueue(RecycleAtlasForFixedSizeImages instance)
        {
            var loadQueue = instance.LoadingQueue;
            var result = loadingQueueFoldout.contentContainer.Query<Label>().ToList();
            int childCount = result.Count;

            for (int i = childCount; i < loadQueue.Count; ++i)
            {
                loadingQueueFoldout.Add(new Label());
            }

            for (int i = loadQueue.Count; i < childCount; ++i)
            {
                result[i].style.visibility = Visibility.Hidden;
            }
            result = loadingQueueFoldout.contentContainer.Query<Label>().ToList();
            for (int i = 0; i < loadQueue.Count; ++i)
            {
                result[i].text = System.IO.Path.GetFileName(loadQueue[i]);
            }
        }


        void SetupRequestFiles(RecycleAtlasForFixedSizeImages instance)
        {
            var requestFiles = instance.RequestedFiles;
            var result = requestedFiles.contentContainer.Query<Label>().ToList();
            int childCount = result.Count;

            requestedFiles.text = "RequestFiles(" + requestFiles.Count + ")";

            for (int i = childCount; i < requestFiles.Count; ++i)
            {
                requestedFiles.Add(new Label());
            }

            for (int i = requestFiles.Count; i < childCount; ++i)
            {
                result[i].style.visibility = Visibility.Hidden;
            }
            result = requestedFiles.contentContainer.Query<Label>().ToList();
            for (int i = 0; i < requestFiles.Count; ++i)
            {
                var order = instance.GetOrderValueInRequestFile(requestFiles[i]);
                result[i].text =requestFiles[i] + "  " + order;
            }
        }
    }
}

#endif
