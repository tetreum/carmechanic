using UnityEngine;

/// <summary>
///     Represents a behavior that monitors the application for framerate issues and automatically submits a user report.
/// </summary>
public class FramerateMonitor : UserReportingMonitor
{
    #region Constructors

    /// <summary>
    ///     Creates a new instance of the <see cref="FramerateMonitor" /> class.
    /// </summary>
    public FramerateMonitor()
    {
        MaximumDurationInSeconds = 10;
        MinimumFramerate = 15;
    }

    #endregion

    #region Methods

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        var framerate = 1.0f / deltaTime;
        if (framerate < MinimumFramerate)
            duration += deltaTime;
        else
            duration = 0;

        if (duration > MaximumDurationInSeconds)
        {
            duration = 0;
            Trigger();
        }
    }

    #endregion

    #region Fields

    private float duration;

    /// <summary>
    ///     Gets or sets the maximum duration in seconds.
    /// </summary>
    public float MaximumDurationInSeconds;

    /// <summary>
    ///     Gets or sets the minimum framerate.
    /// </summary>
    public float MinimumFramerate;

    #endregion
}