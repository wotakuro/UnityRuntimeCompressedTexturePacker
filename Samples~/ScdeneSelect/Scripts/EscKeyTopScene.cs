using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UTJ.Sample
{
    public class EscKeyTopScene : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            var gmo = new GameObject("EscKeyTopScene", typeof(EscKeyTopScene));
        }
        private float tapTimer = 0f;
        private int tapCount = 0;

        private void Awake()
        {
            GameObject.DontDestroyOnLoad(this.gameObject);
        }
        private void Update()
        {
            bool isEscapePressed = false;
            bool isTapped = false;
            // 1. 新しい Input System (New) が有効な場合の判定
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isEscapePressed = true;
            }
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                isTapped = true;
            }
            // 2. 従来の Input Manager (Old) が有効な場合の判定
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isEscapePressed = true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                isTapped = true;
            }
#endif

            tapTimer -= Time.deltaTime;
            if (tapTimer < 0)
            {
                tapCount = 0;
            }
            if (isTapped)
            {
                tapTimer = 0.3f;
                tapCount++;
            }

            // Escキーが押されたらシーン遷移処理を実行
            if (isEscapePressed || tapCount >= 5)
            {
                SceneManager.LoadScene(0);
                tapCount = 0;
            }
        }
    }
}