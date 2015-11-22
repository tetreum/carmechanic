using UnityEngine;
using System.Collections;

public class MainPanel : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Service.init ();
		#if UNITY_EDITOR
		play();
		#endif
	}

	public void play () {
		Application.LoadLevel("Garage");

		#if !UNITY_EDITOR
			Cursor.lockState = CursorLockMode.Locked;
		#endif

		// TEST - Allow custom cars (modding)
		/*Lift lift = GameObject.Find("LiftButton").GetComponent<Lift>();
		GameObject car = Instantiate(Service.carList["MURCIELAGO"].prefab) as GameObject;
		car.transform.SetParent(lift.arms);*/
	}
}
