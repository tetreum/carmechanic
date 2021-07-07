using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{
	public Slider loading_bar;
	public TMP_Text percentage;
	public GameObject menu;
	public GameObject loadingScreen;
	
	void Start () {
		loadingScreen.SetActive(false);
		menu.SetActive(true);
	}

	public void Play (string scene)
	{
		menu.SetActive(false);
		loadingScreen.SetActive(true);
		StartCoroutine(LoadMainMenu(scene));

		if (Application.platform == RuntimePlatform.LinuxEditor ||
		    Application.platform == RuntimePlatform.WindowsEditor ||
		    Application.platform == RuntimePlatform.OSXEditor)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}

		//TEST - Allow custom cars (modding)
		/*Lift lift = GameObject.Find("LiftButton").GetComponent<Lift>();
		GameObject car = Instantiate(Service.carList["MURCIELAGO"].prefab) as GameObject;
		car.transform.SetParent(lift.arms);*/
	}

	public void Quit()
	{
		Application.Quit();
	}

	IEnumerator LoadMainMenu(string scene)
	{
		AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
		while (!operation.isDone)
		{
			float progress = Mathf.Clamp01(operation.progress);
			Debug.Log(progress);
			loading_bar.value = progress;
			percentage.text = progress * 100 / .9f + "%";
			
			yield return null;
		}
		if (operation.isDone)
		{
			loadingScreen.SetActive(false);
		}
	}
}
