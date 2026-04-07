using UnityEngine;
using UnityEngine.SceneManagement;


namespace UTJ.Sample
{

    public class BackButtonInjection : MonoBehaviour
    {
        [SerializeField]
        private GameObject prefab;

        // instance
        private BackButtonInjection instance;


        private void Awake()
        {
            if (instance)
            {
                GameObject.Destroy(this.gameObject);
                return;
            }
            instance = this;
            GameObject.DontDestroyOnLoad(this.gameObject);
        }
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 0)
            {
                return;
            }
            var canvas = GameObject.Find("Canvas");
            var buttonObj = GameObject.Instantiate(prefab);
            buttonObj.transform.SetParent(canvas.transform, false);
        }
    }
}