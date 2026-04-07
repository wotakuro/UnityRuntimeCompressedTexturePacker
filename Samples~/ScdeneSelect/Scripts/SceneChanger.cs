using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public string sceneName;
    private Button button;

    private float time;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.button = GetComponent<Button>();
        button.onClick.AddListener(OnClickItem);
    }
    void OnClickItem()
    {
        if(time > 0.3f)
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
    }
}
