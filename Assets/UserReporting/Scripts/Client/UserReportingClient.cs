using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Unity.Cloud.UserReporting.Client
{
    /// <summary>
    ///     Represents a user reporting client.
    /// </summary>
    public class UserReportingClient
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="UserReportingClient" /> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="projectIdentifier">The project identifier.</param>
        /// <param name="platform">The platform.</param>
        /// <param name="configuration">The configuration.</param>
        public UserReportingClient(string endpoint, string projectIdentifier, IUserReportingPlatform platform,
            UserReportingClientConfiguration configuration)
        {
            // Arguments
            Endpoint = endpoint;
            ProjectIdentifier = projectIdentifier;
            Platform = platform;
            Configuration = configuration;

            // Configuration Clean Up
            Configuration.FramesPerMeasure = Configuration.FramesPerMeasure > 0 ? Configuration.FramesPerMeasure : 1;
            Configuration.MaximumEventCount = Configuration.MaximumEventCount > 0 ? Configuration.MaximumEventCount : 1;
            Configuration.MaximumMeasureCount =
                Configuration.MaximumMeasureCount > 0 ? Configuration.MaximumMeasureCount : 1;
            Configuration.MaximumScreenshotCount =
                Configuration.MaximumScreenshotCount > 0 ? Configuration.MaximumScreenshotCount : 1;

            // Lists
            clientMetrics = new Dictionary<string, UserReportMetric>();
            currentMeasureMetadata = new Dictionary<string, string>();
            currentMetrics = new Dictionary<string, UserReportMetric>();
            events = new CyclicalList<UserReportEvent>(configuration.MaximumEventCount);
            measures = new CyclicalList<UserReportMeasure>(configuration.MaximumMeasureCount);
            screenshots = new CyclicalList<UserReportScreenshot>(configuration.MaximumScreenshotCount);

            // Device Metadata
            deviceMetadata = new List<UserReportNamedValue>();
            foreach (var kvp in Platform.GetDeviceMetadata()) AddDeviceMetadata(kvp.Key, kvp.Value);

            // Client Version
            AddDeviceMetadata("UserReportingClientVersion", "2.0");

            // Synchronized Action
            synchronizedActions = new List<Action>();
            currentSynchronizedActions = new List<Action>();

            // Update Stopwatch
            updateStopwatch = new Stopwatch();

            // Is Connected to Logger
            IsConnectedToLogger = true;
        }

        #endregion

        #region Fields

        private readonly Dictionary<string, UserReportMetric> clientMetrics;

        private readonly Dictionary<string, string> currentMeasureMetadata;

        private readonly Dictionary<string, UserReportMetric> currentMetrics;

        private readonly List<Action> currentSynchronizedActions;

        private readonly List<UserReportNamedValue> deviceMetadata;

        private readonly CyclicalList<UserReportEvent> events;

        private int frameNumber;

        private bool isMeasureBoundary;

        private int measureFrames;

        private readonly CyclicalList<UserReportMeasure> measures;

        private readonly CyclicalList<UserReportScreenshot> screenshots;

        private int screenshotsSaved;

        private int screenshotsTaken;

        private readonly List<Action> synchronizedActions;

        private readonly Stopwatch updateStopwatch;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the configuration.
        /// </summary>
        public UserReportingClientConfiguration Configuration { get; }

        /// <summary>
        ///     Gets the endpoint.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the client is connected to the logger. If true, log messages will be
        ///     included in user reports.
        /// </summary>
        public bool IsConnectedToLogger { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the client is self reporting. If true, event and metrics about the client
        ///     will be included in user reports.
        /// </summary>
        public bool IsSelfReporting { get; set; }

        /// <summary>
        ///     Gets the platform.
        /// </summary>
        public IUserReportingPlatform Platform { get; }

        /// <summary>
        ///     Gets the project identifier.
        /// </summary>
        public string ProjectIdentifier { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether user reporting events should be sent to analytics.
        /// </summary>
        public bool SendEventsToAnalytics { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Adds device metadata.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddDeviceMetadata(string name, string value)
        {
            lock (deviceMetadata)
            {
                var userReportNamedValue = new UserReportNamedValue();
                userReportNamedValue.Name = name;
                userReportNamedValue.Value = value;
                deviceMetadata.Add(userReportNamedValue);
            }
        }

        /// <summary>
        ///     Adds measure metadata. Measure metadata is associated with a period of time.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddMeasureMetadata(string name, string value)
        {
            if (currentMeasureMetadata.ContainsKey(name))
                currentMeasureMetadata[name] = value;
            else
                currentMeasureMetadata.Add(name, value);
        }

        /// <summary>
        ///     Adds a synchronized action.
        /// </summary>
        /// <param name="action">The action.</param>
        private void AddSynchronizedAction(Action action)
        {
            if (action == null) throw new ArgumentNullException("action");
            lock (synchronizedActions)
            {
                synchronizedActions.Add(action);
            }
        }

        /// <summary>
        ///     Clears the screenshots.
        /// </summary>
        public void ClearScreenshots()
        {
            lock (screenshots)
            {
                screenshots.Clear();
            }
        }

        /// <summary>
        ///     Creates a user report.
        /// </summary>
        /// <param name="callback">The callback. Provides the user report that was created.</param>
        public void CreateUserReport(Action<UserReport> callback)
        {
            LogEvent(UserReportEventLevel.Info, "Creating user report.");
            WaitForPerforation(screenshotsTaken, () =>
            {
                Platform.RunTask(() =>
                {
                    // Start Stopwatch
                    var stopwatch = Stopwatch.StartNew();

                    // Copy Data
                    var userReport = new UserReport();
                    userReport.ProjectIdentifier = ProjectIdentifier;

                    // Device Metadata
                    lock (deviceMetadata)
                    {
                        userReport.DeviceMetadata = deviceMetadata.ToList();
                    }

                    // Events
                    lock (events)
                    {
                        userReport.Events = events.ToList();
                    }

                    // Measures
                    lock (measures)
                    {
                        userReport.Measures = measures.ToList();
                    }

                    // Screenshots
                    lock (screenshots)
                    {
                        userReport.Screenshots = screenshots.ToList();
                    }

                    // Complete
                    userReport.Complete();

                    // Modify
                    Platform.ModifyUserReport(userReport);

                    // Stop Stopwatch
                    stopwatch.Stop();

                    // Sample Client Metric
                    SampleClientMetric("UserReportingClient.CreateUserReport.Task", stopwatch.ElapsedMilliseconds);

                    // Copy Client Metrics
                    foreach (var kvp in clientMetrics) userReport.ClientMetrics.Add(kvp.Value);

                    // Return
                    return userReport;
                }, result => { callback(result as UserReport); });
            });
        }

        /// <summary>
        ///     Gets the endpoint.
        /// </summary>
        /// <returns>The endpoint.</returns>
        public string GetEndpoint()
        {
            if (Endpoint == null) return "https://localhost";
            return Endpoint.Trim();
        }

        /// <summary>
        ///     Logs an event.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        public void LogEvent(UserReportEventLevel level, string message)
        {
            LogEvent(level, message, null, null);
        }

        /// <summary>
        ///     Logs an event.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="stackTrace">The stack trace.</param>
        public void LogEvent(UserReportEventLevel level, string message, string stackTrace)
        {
            LogEvent(level, message, stackTrace, null);
        }

        /// <summary>
        ///     Logs an event with a stack trace and exception.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="stackTrace">The stack trace.</param>
        /// <param name="exception">The exception.</param>
        private void LogEvent(UserReportEventLevel level, string message, string stackTrace, Exception exception)
        {
            lock (events)
            {
                var userReportEvent = new UserReportEvent();
                userReportEvent.Level = level;
                userReportEvent.Message = message;
                userReportEvent.FrameNumber = frameNumber;
                userReportEvent.StackTrace = stackTrace;
                userReportEvent.Timestamp = DateTime.UtcNow;
                if (exception != null) userReportEvent.Exception = new SerializableException(exception);
                events.Add(userReportEvent);
            }
        }

        /// <summary>
        ///     Logs an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void LogException(Exception exception)
        {
            LogEvent(UserReportEventLevel.Error, null, null, exception);
        }

        /// <summary>
        ///     Samples a client metric. These metrics are only sample when self reporting is enabled.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SampleClientMetric(string name, double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value)) return;
            if (!clientMetrics.ContainsKey(name))
            {
                var newUserReportMetric = new UserReportMetric();
                newUserReportMetric.Name = name;
                clientMetrics.Add(name, newUserReportMetric);
            }

            var userReportMetric = clientMetrics[name];
            userReportMetric.Sample(value);
            clientMetrics[name] = userReportMetric;

            // Self Reporting
            if (IsSelfReporting) SampleMetric(name, value);
        }

        /// <summary>
        ///     Samples a metric. Metrics can be sampled frequently and have low overhead.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SampleMetric(string name, double value)
        {
            if (Configuration.MetricsGatheringMode == MetricsGatheringMode.Disabled) return;
            if (double.IsInfinity(value) || double.IsNaN(value)) return;
            if (!currentMetrics.ContainsKey(name))
            {
                var newUserReportMetric = new UserReportMetric();
                newUserReportMetric.Name = name;
                currentMetrics.Add(name, newUserReportMetric);
            }

            var userReportMetric = currentMetrics[name];
            userReportMetric.Sample(value);
            currentMetrics[name] = userReportMetric;
        }

        /// <summary>
        ///     Saves a user report to disk.
        /// </summary>
        /// <param name="userReport">The user report.</param>
        public void SaveUserReportToDisk(UserReport userReport)
        {
            LogEvent(UserReportEventLevel.Info, "Saving user report to disk.");
            var json = Platform.SerializeJson(userReport);
            File.WriteAllText("UserReport.json", json);
        }

        /// <summary>
        ///     Sends a user report to the server.
        /// </summary>
        /// <param name="userReport">The user report.</param>
        /// <param name="callback">
        ///     The callback. Provides a value indicating whether sending the user report was successful and
        ///     provides the user report after it is modified by the server.
        /// </param>
        public void SendUserReport(UserReport userReport, Action<bool, UserReport> callback)
        {
            SendUserReport(userReport, null, callback);
        }

        /// <summary>
        ///     Sends a user report to the server.
        /// </summary>
        /// <param name="userReport">The user report.</param>
        /// <param name="progressCallback">The progress callback. Provides the upload and download progress.</param>
        /// <param name="callback">
        ///     The callback. Provides a value indicating whether sending the user report was successful and
        ///     provides the user report after it is modified by the server.
        /// </param>
        public void SendUserReport(UserReport userReport, Action<float, float> progressCallback,
            Action<bool, UserReport> callback)
        {
            try
            {
                if (userReport == null) return;
                if (userReport.Identifier != null)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "Identifier cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                if (userReport.ContentLength != 0)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "ContentLength cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                if (userReport.ReceivedOn != default)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "ReceivedOn cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                if (userReport.ExpiresOn != default)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "ExpiresOn cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                LogEvent(UserReportEventLevel.Info, "Sending user report.");
                var json = Platform.SerializeJson(userReport);
                var jsonData = Encoding.UTF8.GetBytes(json);
                var endpoint = GetEndpoint();
                var url = string.Format("{0}/api/userreporting", endpoint);
                Platform.Post(url, "application/json", jsonData, (uploadProgress, downloadProgress) =>
                {
                    if (progressCallback != null) progressCallback(uploadProgress, downloadProgress);
                }, (success, result) =>
                {
                    AddSynchronizedAction(() =>
                    {
                        if (success)
                        {
                            try
                            {
                                var jsonResult = Encoding.UTF8.GetString(result);
                                var userReportResult = Platform.DeserializeJson<UserReport>(jsonResult);
                                if (userReportResult != null)
                                {
                                    if (SendEventsToAnalytics)
                                    {
                                        var eventData = new Dictionary<string, object>();
                                        eventData.Add("UserReportIdentifier", userReport.Identifier);
                                        Platform.SendAnalyticsEvent("UserReportingClient.SendUserReport", eventData);
                                    }

                                    callback(success, userReportResult);
                                }
                                else
                                {
                                    callback(false, null);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogEvent(UserReportEventLevel.Error,
                                    string.Format("Sending user report failed: {0}", ex));
                                callback(false, null);
                            }
                        }
                        else
                        {
                            LogEvent(UserReportEventLevel.Error, "Sending user report failed.");
                            callback(false, null);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                LogEvent(UserReportEventLevel.Error, string.Format("Sending user report failed: {0}", ex));
                callback(false, null);
            }
        }

        /// <summary>
        ///     Takes a screenshot.
        /// </summary>
        /// <param name="maximumWidth">The maximum width.</param>
        /// <param name="maximumHeight">The maximum height.</param>
        /// <param name="callback">The callback. Provides the screenshot.</param>
        public void TakeScreenshot(int maximumWidth, int maximumHeight, Action<UserReportScreenshot> callback)
        {
            TakeScreenshotFromSource(maximumWidth, maximumHeight, null, callback);
        }

        /// <summary>
        ///     Takes a screenshot.
        /// </summary>
        /// <param name="maximumWidth">The maximum width.</param>
        /// <param name="maximumHeight">The maximum height.</param>
        /// <param name="source">
        ///     The source. Passing null will capture the screen. Passing a camera will capture the camera's view.
        ///     Passing a render texture will capture the render texture.
        /// </param>
        /// <param name="callback">The callback. Provides the screenshot.</param>
        public void TakeScreenshotFromSource(int maximumWidth, int maximumHeight, object source,
            Action<UserReportScreenshot> callback)
        {
            LogEvent(UserReportEventLevel.Info, "Taking screenshot.");
            screenshotsTaken++;
            Platform.TakeScreenshot(frameNumber, maximumWidth, maximumHeight, source, (passedFrameNumber, data) =>
            {
                AddSynchronizedAction(() =>
                {
                    lock (screenshots)
                    {
                        var userReportScreenshot = new UserReportScreenshot();
                        userReportScreenshot.FrameNumber = passedFrameNumber;
                        userReportScreenshot.DataBase64 = Convert.ToBase64String(data);
                        screenshots.Add(userReportScreenshot);
                        screenshotsSaved++;
                        callback(userReportScreenshot);
                    }
                });
            });
        }

        /// <summary>
        ///     Updates the user reporting client, which updates networking communication, screenshotting, and metrics gathering.
        /// </summary>
        public void Update()
        {
            // Stopwatch
            updateStopwatch.Reset();
            updateStopwatch.Start();

            // Update Platform
            Platform.Update(this);

            // Measures
            if (Configuration.MetricsGatheringMode != MetricsGatheringMode.Disabled)
            {
                isMeasureBoundary = false;
                var framesPerMeasure = Configuration.FramesPerMeasure;
                if (measureFrames >= framesPerMeasure)
                    lock (measures)
                    {
                        var userReportMeasure = new UserReportMeasure();
                        userReportMeasure.StartFrameNumber = frameNumber - framesPerMeasure;
                        userReportMeasure.EndFrameNumber = frameNumber - 1;
                        var evictedUserReportMeasure = measures.GetNextEviction();
                        if (evictedUserReportMeasure.Metrics != null)
                        {
                            userReportMeasure.Metadata = evictedUserReportMeasure.Metadata;
                            userReportMeasure.Metrics = evictedUserReportMeasure.Metrics;
                        }
                        else
                        {
                            userReportMeasure.Metadata = new List<UserReportNamedValue>();
                            userReportMeasure.Metrics = new List<UserReportMetric>();
                        }

                        userReportMeasure.Metadata.Clear();
                        userReportMeasure.Metrics.Clear();
                        foreach (var kvp in currentMeasureMetadata)
                        {
                            var userReportNamedValue = new UserReportNamedValue();
                            userReportNamedValue.Name = kvp.Key;
                            userReportNamedValue.Value = kvp.Value;
                            userReportMeasure.Metadata.Add(userReportNamedValue);
                        }

                        foreach (var kvp in currentMetrics) userReportMeasure.Metrics.Add(kvp.Value);
                        currentMetrics.Clear();
                        measures.Add(userReportMeasure);
                        measureFrames = 0;
                        isMeasureBoundary = true;
                    }

                measureFrames++;
            }
            else
            {
                isMeasureBoundary = true;
            }

            // Synchronization
            lock (synchronizedActions)
            {
                foreach (var synchronizedAction in synchronizedActions)
                    currentSynchronizedActions.Add(synchronizedAction);
                synchronizedActions.Clear();
            }

            // Perform Synchronized Actions
            foreach (var synchronizedAction in currentSynchronizedActions) synchronizedAction();
            currentSynchronizedActions.Clear();

            // Frame Number
            frameNumber++;

            // Stopwatch
            updateStopwatch.Stop();
            SampleClientMetric("UserReportingClient.Update", updateStopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        ///     Updates the user reporting client at the end of the frame, which updates networking communication, screenshotting,
        ///     and metrics gathering.
        /// </summary>
        public void UpdateOnEndOfFrame()
        {
            // Stopwatch
            updateStopwatch.Reset();
            updateStopwatch.Start();

            // Update Platform
            Platform.OnEndOfFrame(this);

            // Stopwatch
            updateStopwatch.Stop();
            SampleClientMetric("UserReportingClient.UpdateOnEndOfFrame", updateStopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        ///     Waits for perforation, a boundary between measures when no screenshots are in progress.
        /// </summary>
        /// <param name="currentScreenshotsTaken">The current screenshots taken.</param>
        /// <param name="callback">The callback.</param>
        private void WaitForPerforation(int currentScreenshotsTaken, Action callback)
        {
            if (screenshotsSaved >= currentScreenshotsTaken && isMeasureBoundary)
                callback();
            else
                AddSynchronizedAction(() => { WaitForPerforation(currentScreenshotsTaken, callback); });
        }

        #endregion
    }
}