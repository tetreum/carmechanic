using UnityEngine;
using UnityEditor;
using System.Collections;

public class CarMechanic : MonoBehaviour {

	[MenuItem("CarMechanic/Generate assembly Requirements")]
	public static void findInverseReferences ()
	{
		GameObject[] carParts = GameObject.FindGameObjectsWithTag("CarPart");
		CarPart carPart;

		cleanCurrentRequirements(carParts);

		foreach (GameObject gPart in carParts)
		{
			carPart = gPart.GetComponent<CarPart>();

			foreach (CarPart assemblyPart in carPart.disassemblyRequirements)
			{
				assemblyPart.assemblyRequirements.Add(carPart);
			}
		}
	}

	public static void cleanCurrentRequirements (GameObject[] carParts)
	{
		CarPart carPart;

		foreach (GameObject gPart in carParts)
		{
			carPart = gPart.GetComponent<CarPart>();
			
			carPart.assemblyRequirements.Clear();
		}
	}

	[MenuItem("CarMechanic/Apply CarPart tag")]
	public static void applyCarPartTag ()
	{
		CarPart[] carParts = FindObjectsOfType(typeof(CarPart)) as CarPart[];

		if (carParts == null) {
			Debug.Log("No car parts found");
			return;
		}

		foreach (CarPart gPart in carParts)
		{
			if (gPart.gameObject.name == "CarPart[]") {
				continue;
			}

			gPart.transform.tag = "CarPart";
		}

		Debug.Log("Car parts tagged");
	}
}
