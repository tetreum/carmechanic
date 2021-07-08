using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FpsCounter : MonoBehaviour
{
    public Text fps;
    public int avgFrameRate;

    void Update()
    {
        float current = 0;
        current = (int) (1f / Time.unscaledDeltaTime);
        avgFrameRate = (int) current;
        fps.text = avgFrameRate.ToString() + " FPS";
    }
}
