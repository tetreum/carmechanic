using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AssetBundlesManager : MonoBehaviour
{
    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Garage")
        {
            var carsbundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "cars"));
            var car1 = carsbundle.LoadAsset<GameObject>("car1");
            Instantiate(car1,new Vector3(-24,(float) 0.1,-2),new Quaternion(0,45,0,0));
        }
    }
}