using UnityEngine;
using UnityEngine.SceneManagement;

public class MainPanel : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Service.init ();
		if (Application.platform == RuntimePlatform.LinuxEditor ||
		    Application.platform == RuntimePlatform.WindowsEditor ||
		    Application.platform == RuntimePlatform.OSXEditor)
		{
			
			Play();
		}
	}

	public void Play () {
		SceneManager.LoadScene("Garage");

		if (Application.platform == RuntimePlatform.LinuxEditor ||
		    Application.platform == RuntimePlatform.WindowsEditor ||
		    Application.platform == RuntimePlatform.OSXEditor)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}

		// TEST - Allow custom cars (modding)
		/*Lift lift = GameObject.Find("LiftButton").GetComponent<Lift>();
		GameObject car = Instantiate(Service.carList["MURCIELAGO"].prefab) as GameObject;
		car.transform.SetParent(lift.arms);*/
	}

	public void Quit()
	{
		Application.Quit();
	}

	IEnumerator()
	{
		
	}
}
