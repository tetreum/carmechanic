﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Assets.UserReporting.Scripts.Plugin;
using Unity.Cloud.UserReporting.Client;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.Cloud.UserReporting.Plugin
{
    /// <summary>
    ///     Represents the Unity user reporting platform.
    /// </summary>
    public class UnityUserReportingPlatform : IUserReportingPlatform, ILogListener
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="UnityUserReportingPlatform" /> class.
        /// </summary>
        public UnityUserReportingPlatform()
        {
            logMessages = new List<LogMessage>();
            postOperations = new List<PostOperation>();
            screenshotOperations = new List<ScreenshotOperation>();
            screenshotStopwatch = new Stopwatch();

            // Recorders
            profilerSamplers = new List<ProfilerSampler>();
            var samplerNames = GetSamplerNames();
            foreach (var kvp in samplerNames)
            {
                var sampler = Sampler.Get(kvp.Key);
                if (sampler.isValid)
                {
                    var recorder = sampler.GetRecorder();
                    recorder.enabled = true;
                    var profilerSampler = new ProfilerSampler();
                    profilerSampler.Name = kvp.Value;
                    profilerSampler.Recorder = recorder;
                    profilerSamplers.Add(profilerSampler);
                }
            }

            // Log Messages
            LogDispatcher.Register(this);
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     Represents a log message.
        /// </summary>
        private struct LogMessage
        {
            #region Fields

            /// <summary>
            ///     Gets or sets the log string.
            /// </summary>
            public string LogString;

            /// <summary>
            ///     Gets or sets the log type.
            /// </summary>
            public LogType LogType;

            /// <summary>
            ///     Gets or sets the stack trace.
            /// </summary>
            public string StackTrace;

            #endregion
        }

        /// <summary>
        ///     Represents a post operation.
        /// </summary>
        private class PostOperation
        {
            #region Properties

            /// <summary>
            ///     Gets or sets the callback.
            /// </summary>
            public Action<bool, byte[]> Callback { get; set; }

            /// <summary>
            ///     Gets or sets the progress callback.
            /// </summary>
            public Action<float, float> ProgressCallback { get; set; }

            /// <summary>
            ///     Gets or sets the web request.
            /// </summary>
            public UnityWebRequest WebRequest { get; set; }

            #endregion
        }

        /// <summary>
        ///     Represents a profiler sampler.
        /// </summary>
        private struct ProfilerSampler
        {
            #region Fields

            /// <summary>
            ///     Gets or sets the name.
            /// </summary>
            public string Name;

            /// <summary>
            ///     Gets or sets the recorder.
            /// </summary>
            public Recorder Recorder;

            #endregion

            #region Methods

            /// <summary>
            ///     Gets the value of the sampler.
            /// </summary>
            /// <returns>The value of the sampler.</returns>
            public double GetValue()
            {
                if (Recorder == null) return 0;
                return Recorder.elapsedNanoseconds / 1000000.0;
            }

            #endregion
        }

        /// <summary>
        ///     Represents a screenshot operation.
        /// </summary>
        private class ScreenshotOperation
        {
            #region Properties

            /// <summary>
            ///     Gets or sets the callback.
            /// </summary>
            public Action<int, byte[]> Callback { get; set; }

            /// <summary>
            ///     Gets or sets the frame number.
            /// </summary>
            public int FrameNumber { get; set; }

            /// <summary>
            ///     Gets or sets the maximum height.
            /// </summary>
            public int MaximumHeight { get; set; }

            /// <summary>
            ///     Gets or sets the maximum width.
            /// </summary>
            public int MaximumWidth { get; set; }

            /// <summary>
            ///     Gets or sets the PNG data.
            /// </summary>
            public byte[] PngData { get; set; }

            /// <summary>
            ///     Gets or sets the source.
            /// </summary>
            public object Source { get; set; }

            /// <summary>
            ///     Gets or sets the stage.
            /// </summary>
            public ScreenshotStage Stage { get; set; }

            /// <summary>
            ///     Gets or sets the texture.
            /// </summary>
            public Texture2D Texture { get; set; }

            /// <summary>
            ///     Gets or sets the texture (resized).
            /// </summary>
            public Texture2D TextureResized { get; set; }

            /// <summary>
            ///     Gets or sets the Unity frame.
            /// </summary>
            public int UnityFrame { get; set; }

            /// <summary>
            ///     Gets or sets the wait frames.
            /// </summary>
            public int WaitFrames { get; set; }

            #endregion
        }

        /// <summary>
        ///     Represents a screenshot stage.
        /// </summary>
        private enum ScreenshotStage
        {
            /// <summary>
            ///     Render.
            /// </summary>
            Render = 0,

            /// <summary>
            ///     Read pixels.
            /// </summary>
            ReadPixels = 1,

            /// <summary>
            ///     Gets pixels.
            /// </summary>
            GetPixels = 2,

            /// <summary>
            ///     Encode to PNG.
            /// </summary>
            EncodeToPNG = 3,

            /// <summary>
            ///     Done.
            /// </summary>
            Done = 4
        }

        #endregion

        #region Fields

        private readonly List<LogMessage> logMessages;

        private readonly List<PostOperation> postOperations;

        private readonly List<ProfilerSampler> profilerSamplers;

        private readonly List<ScreenshotOperation> screenshotOperations;

        private readonly Stopwatch screenshotStopwatch;

        private List<PostOperation> taskOperations;

        #endregion

        #region Methods

        /// <inheritdoc cref="IUserReportingPlatform" />
        public T DeserializeJson<T>(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<T>(json);
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public void OnEndOfFrame(UserReportingClient client)
        {
            // Screenshot Operations
            var screenshotOperationIndex = 0;
            while (screenshotOperationIndex < screenshotOperations.Count)
            {
                var screenshotOperation = screenshotOperations[screenshotOperationIndex];
                if (screenshotOperation.Stage == ScreenshotStage.Render && screenshotOperation.WaitFrames < 1)
                {
                    var cameraSource = screenshotOperation.Source as Camera;
                    if (cameraSource != null)
                    {
                        screenshotStopwatch.Reset();
                        screenshotStopwatch.Start();
                        var renderTexture = new RenderTexture(screenshotOperation.MaximumWidth,
                            screenshotOperation.MaximumHeight, 24);
                        var originalTargetTexture = cameraSource.targetTexture;
                        cameraSource.targetTexture = renderTexture;
                        cameraSource.Render();
                        cameraSource.targetTexture = originalTargetTexture;
                        screenshotStopwatch.Stop();
                        client.SampleClientMetric("Screenshot.Render", screenshotStopwatch.ElapsedMilliseconds);
                        screenshotOperation.Source = renderTexture;
                        screenshotOperation.Stage = ScreenshotStage.ReadPixels;
                        screenshotOperation.WaitFrames = 15;
                        screenshotOperationIndex++;
                        continue;
                    }

                    screenshotOperation.Stage = ScreenshotStage.ReadPixels;
                }

                if (screenshotOperation.Stage == ScreenshotStage.ReadPixels && screenshotOperation.WaitFrames < 1)
                {
                    screenshotStopwatch.Reset();
                    screenshotStopwatch.Start();
                    var renderTextureSource = screenshotOperation.Source as RenderTexture;
                    if (renderTextureSource != null)
                    {
                        var originalActiveTexture = RenderTexture.active;
                        RenderTexture.active = renderTextureSource;
                        screenshotOperation.Texture = new Texture2D(renderTextureSource.width,
                            renderTextureSource.height, TextureFormat.ARGB32, true);
                        screenshotOperation.Texture.ReadPixels(
                            new Rect(0, 0, renderTextureSource.width, renderTextureSource.height), 0, 0);
                        screenshotOperation.Texture.Apply();
                        RenderTexture.active = originalActiveTexture;
                    }
                    else
                    {
                        screenshotOperation.Texture =
                            new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, true);
                        screenshotOperation.Texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                        screenshotOperation.Texture.Apply();
                    }

                    screenshotStopwatch.Stop();
                    client.SampleClientMetric("Screenshot.ReadPixels", screenshotStopwatch.ElapsedMilliseconds);
                    screenshotOperation.Stage = ScreenshotStage.GetPixels;
                    screenshotOperation.WaitFrames = 15;
                    screenshotOperationIndex++;
                    continue;
                }

                if (screenshotOperation.Stage == ScreenshotStage.GetPixels && screenshotOperation.WaitFrames < 1)
                {
                    screenshotStopwatch.Reset();
                    screenshotStopwatch.Start();
                    var maximumWidth = screenshotOperation.MaximumWidth > 32 ? screenshotOperation.MaximumWidth : 32;
                    var maximumHeight = screenshotOperation.MaximumHeight > 32 ? screenshotOperation.MaximumHeight : 32;
                    var width = screenshotOperation.Texture.width;
                    var height = screenshotOperation.Texture.height;
                    var mipLevel = 0;
                    while (width > maximumWidth || height > maximumHeight)
                    {
                        width /= 2;
                        height /= 2;
                        mipLevel++;
                    }

                    screenshotOperation.TextureResized = new Texture2D(width, height);
                    screenshotOperation.TextureResized.SetPixels(screenshotOperation.Texture.GetPixels(mipLevel));
                    screenshotOperation.TextureResized.Apply();
                    screenshotStopwatch.Stop();
                    client.SampleClientMetric("Screenshot.GetPixels", screenshotStopwatch.ElapsedMilliseconds);
                    screenshotOperation.Stage = ScreenshotStage.EncodeToPNG;
                    screenshotOperation.WaitFrames = 15;
                    screenshotOperationIndex++;
                    continue;
                }

                if (screenshotOperation.Stage == ScreenshotStage.EncodeToPNG && screenshotOperation.WaitFrames < 1)
                {
                    screenshotStopwatch.Reset();
                    screenshotStopwatch.Start();
                    screenshotOperation.PngData = screenshotOperation.TextureResized.EncodeToPNG();
                    screenshotStopwatch.Stop();
                    client.SampleClientMetric("Screenshot.EncodeToPNG", screenshotStopwatch.ElapsedMilliseconds);
                    screenshotOperation.Stage = ScreenshotStage.Done;
                    screenshotOperationIndex++;
                    continue;
                }

                if (screenshotOperation.Stage == ScreenshotStage.Done && screenshotOperation.WaitFrames < 1)
                {
                    screenshotOperation.Callback(screenshotOperation.FrameNumber, screenshotOperation.PngData);
                    Object.Destroy(screenshotOperation.Texture);
                    Object.Destroy(screenshotOperation.TextureResized);
                    screenshotOperations.Remove(screenshotOperation);
                }

                screenshotOperation.WaitFrames--;
            }
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public void Post(string endpoint, string contentType, byte[] content, Action<float, float> progressCallback,
            Action<bool, byte[]> callback)
        {
            var webRequest = new UnityWebRequest(endpoint, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(content);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", contentType);
            webRequest.SendWebRequest();
            var postOperation = new PostOperation();
            postOperation.WebRequest = webRequest;
            postOperation.Callback = callback;
            postOperation.ProgressCallback = progressCallback;
            postOperations.Add(postOperation);
        }

        public void ReceiveLogMessage(string logString, string stackTrace, LogType logType)
        {
            lock (logMessages)
            {
                var logMessage = new LogMessage();
                logMessage.LogString = logString;
                logMessage.StackTrace = stackTrace;
                logMessage.LogType = logType;
                logMessages.Add(logMessage);
            }
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public void RunTask(Func<object> task, Action<object> callback)
        {
            callback(task());
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public void SendAnalyticsEvent(string eventName, Dictionary<string, object> eventData)
        {
            Analytics.CustomEvent(eventName, eventData);
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public string SerializeJson(object instance)
        {
            return SimpleJson.SimpleJson.SerializeObject(instance);
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public void TakeScreenshot(int frameNumber, int maximumWidth, int maximumHeight, object source,
            Action<int, byte[]> callback)
        {
            var screenshotOperation = new ScreenshotOperation();
            screenshotOperation.FrameNumber = frameNumber;
            screenshotOperation.MaximumWidth = maximumWidth;
            screenshotOperation.MaximumHeight = maximumHeight;
            screenshotOperation.Source = source;
            screenshotOperation.Callback = callback;
            screenshotOperation.UnityFrame = Time.frameCount;
            screenshotOperations.Add(screenshotOperation);
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public void Update(UserReportingClient client)
        {
            // Log Messages
            lock (logMessages)
            {
                foreach (var logMessage in logMessages)
                {
                    var eventLevel = UserReportEventLevel.Info;
                    if (logMessage.LogType == LogType.Warning)
                        eventLevel = UserReportEventLevel.Warning;
                    else if (logMessage.LogType == LogType.Error)
                        eventLevel = UserReportEventLevel.Error;
                    else if (logMessage.LogType == LogType.Exception)
                        eventLevel = UserReportEventLevel.Error;
                    else if (logMessage.LogType == LogType.Assert) eventLevel = UserReportEventLevel.Error;
                    if (client.IsConnectedToLogger)
                        client.LogEvent(eventLevel, logMessage.LogString, logMessage.StackTrace);
                }

                logMessages.Clear();
            }

            // Metrics
            if (client.Configuration.MetricsGatheringMode == MetricsGatheringMode.Automatic)
            {
                // Sample Automatic Metrics
                SampleAutomaticMetrics(client);

                // Profiler Samplers
                foreach (var profilerSampler in profilerSamplers)
                    client.SampleMetric(profilerSampler.Name, profilerSampler.GetValue());
            }

            // Post Operations
            var postOperationIndex = 0;
            while (postOperationIndex < postOperations.Count)
            {
                var postOperation = postOperations[postOperationIndex];
                if (postOperation.WebRequest.isDone)
                {
                    var isError = postOperation.WebRequest.error != null &&
                                  postOperation.WebRequest.responseCode != 200;
                    if (isError)
                    {
                        var errorMessage = string.Format("UnityUserReportingPlatform.Post: {0} {1}",
                            postOperation.WebRequest.responseCode, postOperation.WebRequest.error);
                        Debug.Log(errorMessage);
                        client.LogEvent(UserReportEventLevel.Error, errorMessage);
                    }

                    postOperation.ProgressCallback(1, 1);
                    postOperation.Callback(!isError, postOperation.WebRequest.downloadHandler.data);
                    postOperations.Remove(postOperation);
                }
                else
                {
                    postOperation.ProgressCallback(postOperation.WebRequest.uploadProgress,
                        postOperation.WebRequest.downloadProgress);
                    postOperationIndex++;
                }
            }
        }

        #endregion

        #region Virtual Methods

        /// <inheritdoc cref="IUserReportingPlatform" />
        public virtual IDictionary<string, string> GetDeviceMetadata()
        {
            var deviceMetadata = new Dictionary<string, string>();

            // Unity
            deviceMetadata.Add("BuildGUID", Application.buildGUID);
            deviceMetadata.Add("DeviceModel", SystemInfo.deviceModel);
            deviceMetadata.Add("DeviceType", SystemInfo.deviceType.ToString());
            deviceMetadata.Add("DeviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
            deviceMetadata.Add("DPI", Screen.dpi.ToString(CultureInfo.InvariantCulture));
            deviceMetadata.Add("GraphicsDeviceName", SystemInfo.graphicsDeviceName);
            deviceMetadata.Add("GraphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
            deviceMetadata.Add("GraphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
            deviceMetadata.Add("GraphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
            deviceMetadata.Add("GraphicsMemorySize", SystemInfo.graphicsMemorySize.ToString());
            deviceMetadata.Add("InstallerName", Application.installerName);
            deviceMetadata.Add("InstallMode", Application.installMode.ToString());
            deviceMetadata.Add("IsEditor", Application.isEditor.ToString());
            deviceMetadata.Add("IsFullScreen", Screen.fullScreen.ToString());
            deviceMetadata.Add("OperatingSystem", SystemInfo.operatingSystem);
            deviceMetadata.Add("OperatingSystemFamily", SystemInfo.operatingSystemFamily.ToString());
            deviceMetadata.Add("Orientation", Screen.orientation.ToString());
            deviceMetadata.Add("Platform", Application.platform.ToString());
            try
            {
                deviceMetadata.Add("QualityLevel", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            }
            catch
            {
                // Empty
            }

            deviceMetadata.Add("ResolutionWidth", Screen.currentResolution.width.ToString());
            deviceMetadata.Add("ResolutionHeight", Screen.currentResolution.height.ToString());
            deviceMetadata.Add("ResolutionRefreshRate", Screen.currentResolution.refreshRate.ToString());
            deviceMetadata.Add("SystemLanguage", Application.systemLanguage.ToString());
            deviceMetadata.Add("SystemMemorySize", SystemInfo.systemMemorySize.ToString());
            deviceMetadata.Add("TargetFrameRate", Application.targetFrameRate.ToString());
            deviceMetadata.Add("UnityVersion", Application.unityVersion);
            deviceMetadata.Add("Version", Application.version);

            // Other
            deviceMetadata.Add("Source", "Unity");

            // Type
            var type = GetType();
            deviceMetadata.Add("IUserReportingPlatform", type.Name);

            // Return
            return deviceMetadata;
        }

        public virtual Dictionary<string, string> GetSamplerNames()
        {
            var samplerNames = new Dictionary<string, string>();
            samplerNames.Add("AudioManager.FixedUpdate", "AudioManager.FixedUpdateInMilliseconds");
            samplerNames.Add("AudioManager.Update", "AudioManager.UpdateInMilliseconds");
            samplerNames.Add("LateBehaviourUpdate", "Behaviors.LateUpdateInMilliseconds");
            samplerNames.Add("BehaviourUpdate", "Behaviors.UpdateInMilliseconds");
            samplerNames.Add("Camera.Render", "Camera.RenderInMilliseconds");
            samplerNames.Add("Overhead", "Engine.OverheadInMilliseconds");
            samplerNames.Add("WaitForRenderJobs", "Engine.WaitForRenderJobsInMilliseconds");
            samplerNames.Add("WaitForTargetFPS", "Engine.WaitForTargetFPSInMilliseconds");
            samplerNames.Add("GUI.Repaint", "GUI.RepaintInMilliseconds");
            samplerNames.Add("Network.Update", "Network.UpdateInMilliseconds");
            samplerNames.Add("ParticleSystem.EndUpdateAll", "ParticleSystem.EndUpdateAllInMilliseconds");
            samplerNames.Add("ParticleSystem.Update", "ParticleSystem.UpdateInMilliseconds");
            samplerNames.Add("Physics.FetchResults", "Physics.FetchResultsInMilliseconds");
            samplerNames.Add("Physics.Processing", "Physics.ProcessingInMilliseconds");
            samplerNames.Add("Physics.ProcessReports", "Physics.ProcessReportsInMilliseconds");
            samplerNames.Add("Physics.Simulate", "Physics.SimulateInMilliseconds");
            samplerNames.Add("Physics.UpdateBodies", "Physics.UpdateBodiesInMilliseconds");
            samplerNames.Add("Physics.Interpolation", "Physics.InterpolationInMilliseconds");
            samplerNames.Add("Physics2D.DynamicUpdate", "Physics2D.DynamicUpdateInMilliseconds");
            samplerNames.Add("Physics2D.FixedUpdate", "Physics2D.FixedUpdateInMilliseconds");
            return samplerNames;
        }

        /// <inheritdoc cref="IUserReportingPlatform" />
        public virtual void ModifyUserReport(UserReport userReport)
        {
            // Active Scene
            var activeScene = SceneManager.GetActiveScene();
            userReport.DeviceMetadata.Add(new UserReportNamedValue("ActiveSceneName", activeScene.name));

            // Main Camera
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                userReport.DeviceMetadata.Add(new UserReportNamedValue("MainCameraName", mainCamera.name));
                userReport.DeviceMetadata.Add(new UserReportNamedValue("MainCameraPosition",
                    mainCamera.transform.position.ToString()));
                userReport.DeviceMetadata.Add(new UserReportNamedValue("MainCameraForward",
                    mainCamera.transform.forward.ToString()));

                // Looking At
                RaycastHit hit;
                if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit))
                {
                    var lookingAt = hit.transform.gameObject;
                    userReport.DeviceMetadata.Add(new UserReportNamedValue("LookingAt", hit.point.ToString()));
                    userReport.DeviceMetadata.Add(new UserReportNamedValue("LookingAtGameObject", lookingAt.name));
                    userReport.DeviceMetadata.Add(new UserReportNamedValue("LookingAtGameObjectPosition",
                        lookingAt.transform.position.ToString()));
                }
            }
        }

        /// <summary>
        ///     Samples automatic metrics.
        /// </summary>
        /// <param name="client">The client.</param>
        public virtual void SampleAutomaticMetrics(UserReportingClient client)
        {
            // Graphics
            client.SampleMetric("Graphics.FramesPerSecond", 1.0f / Time.deltaTime);

            // Memory
            client.SampleMetric("Memory.MonoUsedSizeInBytes", Profiler.GetMonoUsedSizeLong());
            client.SampleMetric("Memory.TotalAllocatedMemoryInBytes", Profiler.GetTotalAllocatedMemoryLong());
            client.SampleMetric("Memory.TotalReservedMemoryInBytes", Profiler.GetTotalReservedMemoryLong());
            client.SampleMetric("Memory.TotalUnusedReservedMemoryInBytes", Profiler.GetTotalUnusedReservedMemoryLong());

            // Battery
            client.SampleMetric("Battery.BatteryLevelInPercent", SystemInfo.batteryLevel);
        }

        #endregion
    }
}