using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetBundlesManager : MonoBehaviour
{
    private GameObject car1;
    private GameObject lift;
    
    public void Awake()
    {
        var carsbundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "cars"));
        var objectsbundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "objects"));
        
        car1 = carsbundle.LoadAsset<GameObject>("car1");
        lift = objectsbundle.LoadAsset<GameObject>("lift");
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Game")
        {
            Instantiate(lift, new Vector3(37, 0, 35), Quaternion.Euler(new Vector3(-90, 0, 0)));
            Instantiate(car1, new Vector3(24, (int) 0.1, 25), Quaternion.Euler(new Vector3(0, -90, 0)));
        }
    }
}