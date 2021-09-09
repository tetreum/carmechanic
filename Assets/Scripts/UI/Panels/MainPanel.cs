using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{
    public Slider loading_bar;
    public TMP_Text percentage;
    public TMP_Text version;
    public GameObject menu;
    public GameObject loadingScreen;
    public TMP_Text loading_object;
    bool active;

    private void Start()
    {
        loadingScreen.SetActive(false);
        menu.SetActive(true);
        version.text = "v" + Application.version;
    }

    public void Play(string scene)
    {
        menu.SetActive(false);
        loadingScreen.SetActive(true);
        StartCoroutine(LoadMainMenu(scene));

        if (Application.platform == RuntimePlatform.LinuxEditor ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXEditor)
            Cursor.lockState = CursorLockMode.Locked;
    }

    public void Quit()
    {
        Application.Quit();
    }

    private IEnumerator LoadMainMenu(string scene)
    {
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone)
        {
            var progress = Mathf.Clamp01(operation.progress);
            Debug.Log(progress);
            loading_bar.value = progress;
            loading_object.text = "Loading scene";
            percentage.text = progress * 100 / .9f + "%";

            yield return null;
        }

        if (operation.isDone)
            if (GameObject.Find("LoadingScreen") == true)
                active = true;
            else
                active = false;
                if(active)
                    loadingScreen.SetActive(false);
    }
}