using UnityEditor;
using UnityEngine;

public class CarMechanic : MonoBehaviour
{
    [MenuItem("CarMechanic/Generate assembly Requirements")]
    public static void findInverseReferences()
    {
        var carParts = GameObject.FindGameObjectsWithTag("CarPart");
        CarPart carPart;

        cleanCurrentRequirements(carParts);

        foreach (var gPart in carParts)
        {
            carPart = gPart.GetComponent<CarPart>();

            foreach (var assemblyPart in carPart.disassemblyRequirements)
                assemblyPart.assemblyRequirements.Add(carPart);
        }
    }

    public static void cleanCurrentRequirements(GameObject[] carParts)
    {
        CarPart carPart;

        foreach (var gPart in carParts)
        {
            carPart = gPart.GetComponent<CarPart>();

            carPart.assemblyRequirements.Clear();
        }
    }

    [MenuItem("CarMechanic/Apply CarPart tag")]
    public static void applyCarPartTag()
    {
        var carParts = FindObjectsOfType(typeof(CarPart)) as CarPart[];

        if (carParts == null)
        {
            Debug.Log("No car parts found");
            return;
        }

        foreach (var gPart in carParts)
        {
            if (gPart.gameObject.name == "CarPart[]") continue;

            gPart.transform.tag = "CarPart";
        }

        Debug.Log("Car parts tagged");
    }
}