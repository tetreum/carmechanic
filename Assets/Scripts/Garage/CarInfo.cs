﻿using UnityEngine;

// this may contain more info in the future
public class CarInfo
{
    public string name;
    public string folder;

    public GameObject prefab => Resources.Load("Vehicles/" + folder + "/Vehicle.prefab") as GameObject;
}