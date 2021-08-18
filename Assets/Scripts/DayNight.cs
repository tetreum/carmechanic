using UnityEngine;

public class DayNight : MonoBehaviour
{
    private const int minInDay = 1440;
    public Transform sun;
    public float x_speed = 5f;
    public float y_speed = 5f;

    [Range(0, 5)] public float minToSecond = 1f;

    [SerializeField] private int currentMin;

    private int dayCount;
    private float timeSinceLastMin;


    private void Update()
    {
        var t = Time.deltaTime;
        // Rotate the sun
        sun.Rotate(new Vector3(x_speed * t, y_speed * t, 0f));

        // If a miute in the game world has passed...
        if (timeSinceLastMin >= minToSecond)
        {
            timeSinceLastMin = 0f;
            MinutePassed();
        }
        else
        {
            timeSinceLastMin += t;
        }
    }

    private void MinutePassed()
    {
        ++currentMin;
        if (currentMin >= minInDay)
        {
            // Next day
            ++dayCount;
            currentMin = 0;
            Debug.Log("NextDay!");
        }

        var min = currentMin % 60;
        var hrs = currentMin / 60;

        Debug.Log("Next minute! " + hrs + "   :   " + min);
    }
}