using UnityEngine;

// this may contain more info in the future
public class CarInfo
{
    public string folder;
    public string name;

    public GameObject prefab => Resources.Load("Vehicles/" + folder + "/Vehicle.prefab") as GameObject;
}