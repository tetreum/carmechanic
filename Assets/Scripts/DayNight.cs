using UnityEngine;

public class DayNight : MonoBehaviour
{
    public Transform sun;
    public float x_speed = 5f;
    public float y_speed = 5f;

    [Range(0,5)]
    public float minToSecond = 1f;

    [SerializeField]
    private int currentMin;

    private const int minInDay = 1440;

    private int dayCount = 0;
    private float timeSinceLastMin;
	
	
    void Update ()
    {
        float t = Time.deltaTime;
        // Rotate the sun
        sun.Rotate(new Vector3(x_speed * t, y_speed * t, 0f));

        // If a miute in the game world has passed...
        if(timeSinceLastMin >= minToSecond)
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
        if(currentMin >= minInDay)
        {
            // Next day
            ++dayCount;
            currentMin = 0;
            Debug.Log("NextDay!");
            
        }

        int min = currentMin % 60;
        int hrs = currentMin / 60;
            
        Debug.Log("Next minute! " + hrs.ToString() + "   :   " + min.ToString());
    }
}