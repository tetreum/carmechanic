using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetBundlesManager : MonoBehaviour
{
    private GameObject car1;
    private GameObject engine;
    private GameObject lift;
    private GameObject engine_crane;

    public void Awake()
    {
        var carsbundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "cars"));
        var objectsbundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "objects"));
        
        car1 = carsbundle.LoadAsset<GameObject>("car1");
        lift = objectsbundle.LoadAsset<GameObject>("lift");
        engine = objectsbundle.LoadAsset<GameObject>("engine1");
        engine_crane = objectsbundle.LoadAsset<GameObject>("engine_crane");
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Game")
            //var car = Instantiate(Service.carList["car1"].prefab,new Vector3(24, 0.1f, 25), Quaternion.Euler(new Vector3(0, -90, 0)));
            Instantiate(lift, new Vector3(37, 0, 35), Quaternion.Euler(new Vector3(-90, 0, 0)));
        Instantiate(car1, new Vector3(24, 0.1f, 25), Quaternion.Euler(new Vector3(0, -90, 0)));
        Instantiate(engine, new Vector3(30, 0, 15), Quaternion.Euler(new Vector3(-90, 0, 0)));
        Instantiate(engine_crane, new Vector3(25, 1, 5), Quaternion.Euler(new Vector3(0, 45, 0)));
        }
}