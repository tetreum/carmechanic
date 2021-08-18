using Unity.Cloud.UserReporting;
using Unity.Cloud.UserReporting.Plugin;
using UnityEngine;

/// <summary>
///     Represents a behavior that monitors the application for issues and automatically submits a user report.
/// </summary>
public class UserReportingMonitor : MonoBehaviour
{
    #region Constructors

    /// <summary>
    ///     Creates a new instance of the <see cref="UserReportingMonitor" /> class.
    /// </summary>
    public UserReportingMonitor()
    {
        IsEnabled = true;
        IsHiddenWithoutDimension = true;
        var type = GetType();
        MonitorName = type.Name;
    }

    #endregion

    #region Virtual Methods

    /// <summary>
    ///     Called when the monitor is triggered.
    /// </summary>
    protected virtual void Triggered()
    {
        // Empty
    }

    #endregion

    #region Fields

    /// <summary>
    ///     Gets or sets a value indicating whether the monitor is enabled.
    /// </summary>
    public bool IsEnabled;

    /// <summary>
    ///     Gets or sets a value indicating whether the monitor is enabled after it is triggered.
    /// </summary>
    public bool IsEnabledAfterTrigger;

    /// <summary>
    ///     Gets or sets a value indicating whether the user report has IsHiddenWithoutDimension set.
    /// </summary>
    public bool IsHiddenWithoutDimension;

    /// <summary>
    ///     Gets or sets the monitor name.
    /// </summary>
    public string MonitorName;

    /// <summary>
    ///     Gets or sets the summary.
    /// </summary>
    public string Summary;

    #endregion

    #region Methods

    private void Start()
    {
        if (UnityUserReporting.CurrentClient == null) UnityUserReporting.Configure();
    }

    /// <summary>
    ///     Triggers the monitor.
    /// </summary>
    public void Trigger()
    {
        if (!IsEnabledAfterTrigger) IsEnabled = false;

        UnityUserReporting.CurrentClient.TakeScreenshot(2048, 2048, s => { });
        UnityUserReporting.CurrentClient.TakeScreenshot(512, 512, s => { });
        UnityUserReporting.CurrentClient.CreateUserReport(br =>
        {
            if (string.IsNullOrEmpty(br.ProjectIdentifier))
                Debug.LogWarning(
                    "The user report's project identifier is not set. Please setup cloud services using the Services tab or manually specify a project identifier when calling UnityUserReporting.Configure().");

            br.Summary = Summary;
            br.DeviceMetadata.Add(new UserReportNamedValue("Monitor", MonitorName));
            var platform = "Unknown";
            var version = "0.0";
            foreach (var deviceMetadata in br.DeviceMetadata)
            {
                if (deviceMetadata.Name == "Platform") platform = deviceMetadata.Value;

                if (deviceMetadata.Name == "Version") version = deviceMetadata.Value;
            }

            br.Dimensions.Add(new UserReportNamedValue("Monitor.Platform.Version",
                string.Format("{0}.{1}.{2}", MonitorName, platform, version)));
            br.Dimensions.Add(new UserReportNamedValue("Monitor", MonitorName));
            br.IsHiddenWithoutDimension = IsHiddenWithoutDimension;
            UnityUserReporting.CurrentClient.SendUserReport(br, (success, br2) => { Triggered(); });
        });
    }

    #endregion
}