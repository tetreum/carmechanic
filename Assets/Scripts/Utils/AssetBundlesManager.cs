using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetBundlesManager : MonoBehaviour
{
    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Garage")
        {
            var carsbundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "cars"));
            var car1 = carsbundle.LoadAsset<GameObject>("car1");
            Instantiate(car1, new Vector3(-24, 0, -2), Quaternion.Euler(new Vector3(0, 90, 0)));
        }
    }
}