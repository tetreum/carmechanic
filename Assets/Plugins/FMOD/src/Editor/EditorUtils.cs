#if UNITY_ADDRESSABLES_EXIST
    // The Addressables package depends on the ScriptableBuildPipeline package
    #define UNITY_SCRIPTABLEBUILDPIPELINE_EXIST
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using FMOD;
using FMOD.Studio;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using ADVANCEDSETTINGS = FMOD.Studio.ADVANCEDSETTINGS;
using Debug = UnityEngine.Debug;
using INITFLAGS = FMOD.Studio.INITFLAGS;
using Object = UnityEngine.Object;
#if UNITY_SCRIPTABLEBUILDPIPELINE_EXIST
using UnityEditor.Build.Pipeline;
#endif

namespace FMODUnity
{
    public enum PreviewState
    {
        Stopped,
        Playing,
        Paused
    }

    [InitializeOnLoad]
    internal class EditorUtils : MonoBehaviour
    {
        public const string BuildFolder = "Build";


        private const int StudioScriptPort = 3663;

        private const string LibPrefix = "Assets/Plugins/FMOD/lib";
        private const string StagingPrefix = "Assets/Plugins/FMOD/staging";

        private static FMOD.Studio.System system;
        private static SPEAKERMODE speakerMode;
        private static string encryptionKey;

        private static readonly List<Bank> masterBanks = new List<Bank>();
        private static readonly List<Bank> previewBanks = new List<Bank>();
        private static EventDescription previewEventDesc;
        private static EventInstance previewEventInstance;

        private static NetworkStream networkStream;
        private static Socket socket;
        private static IAsyncResult socketConnection;

        static EditorUtils()
        {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
            EditorApplication.pauseStateChanged += HandleOnPausedModeChanged;
        }

        public static FMOD.Studio.System System
        {
            get
            {
                if (!system.isValid()) CreateSystem();
                return system;
            }
        }

        public static PreviewState PreviewState { get; private set; }

        private static NetworkStream ScriptStream
        {
            get
            {
                if (networkStream == null)
                    try
                    {
                        if (socket == null)
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        if (!socket.Connected)
                        {
                            socketConnection = socket.BeginConnect("127.0.0.1", StudioScriptPort, null, null);
                            socketConnection.AsyncWaitHandle.WaitOne();
                            socket.EndConnect(socketConnection);
                            socketConnection = null;
                        }

                        networkStream = new NetworkStream(socket);

                        var headerBytes = new byte[128];
                        var read = ScriptStream.Read(headerBytes, 0, 128);
                        var header = Encoding.UTF8.GetString(headerBytes, 0, read - 1);
                        if (header.StartsWith("log():"))
                            Debug.Log("FMOD Studio: Script Client returned " + header.Substring(6));
                    }
                    catch (Exception e)
                    {
                        Debug.Log("FMOD Studio: Script Client failed to connect - Check FMOD Studio is running");

                        socketConnection = null;
                        socket = null;
                        networkStream = null;

                        throw e;
                    }

                return networkStream;
            }
        }

        private static void Update()
        {
            // Update the editor system
            if (system.isValid())
            {
                CheckResult(system.update());

                if (speakerMode != Settings.Instance.GetEditorSpeakerMode()) RecreateSystem();

                if (encryptionKey != Settings.Instance.EncryptionKey) RecreateSystem();
            }

            if (previewEventInstance.isValid())
            {
                PLAYBACK_STATE state;
                previewEventInstance.getPlaybackState(out state);
                if (PreviewState == PreviewState.Playing && state == PLAYBACK_STATE.STOPPED) PreviewStop();
            }
        }

        public static void CheckResult(RESULT result)
        {
            if (result != RESULT.OK)
                Debug.LogError(string.Format("FMOD Studio: Encounterd Error: {0} {1}", result, Error.String(result)));
        }

        public static void ValidateSource(out bool valid, out string reason)
        {
            valid = true;
            reason = "";
            var settings = Settings.Instance;
            if (settings.HasSourceProject)
            {
                if (string.IsNullOrEmpty(settings.SourceProjectPath))
                {
                    valid = false;
                    reason = "The FMOD Studio project path must be set to an .fspro file.";
                    return;
                }

                if (!File.Exists(settings.SourceProjectPath))
                {
                    valid = false;
                    reason = string.Format("The FMOD Studio project path '{0}' does not exist.",
                        settings.SourceProjectPath);
                    return;
                }

                var projectPath = settings.SourceProjectPath;
                var projectFolder = Path.GetDirectoryName(projectPath);
                var buildFolder = RuntimeUtils.GetCommonPlatformPath(Path.Combine(projectFolder, BuildFolder));
                if (!Directory.Exists(buildFolder) ||
                    Directory.GetDirectories(buildFolder).Length == 0 ||
                    Directory.GetFiles(Directory.GetDirectories(buildFolder)[0], "*.bank", SearchOption.AllDirectories)
                        .Length == 0
                )
                {
                    valid = false;
                    reason = string.Format(
                        "The FMOD Studio project '{0}' does not contain any built banks. Please build your project in FMOD Studio.",
                        settings.SourceProjectPath);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(settings.SourceBankPath))
                {
                    valid = false;
                    reason = "The build path has not been set.";
                    return;
                }

                if (!Directory.Exists(settings.SourceBankPath))
                {
                    valid = false;
                    reason = string.Format("The build path '{0}' does not exist.", settings.SourceBankPath);
                    return;
                }

                if (settings.HasPlatforms)
                {
                    if (Directory.GetDirectories(settings.SourceBankPath).Length == 0)
                    {
                        valid = false;
                        reason = string.Format(
                            "Build path '{0}' does not contain any platform sub-directories. Please check that the build path is correct.",
                            settings.SourceBankPath);
                    }
                }
                else
                {
                    if (Directory.GetFiles(settings.SourceBankPath, "*.strings.bank").Length == 0)
                    {
                        valid = false;
                        reason = string.Format("Build path '{0}' does not contain any built banks.",
                            settings.SourceBankPath);
                    }
                }
            }
        }

        public static string[] GetBankPlatforms()
        {
            var buildFolder = Settings.Instance.SourceBankPath;
            try
            {
                if (Directory.GetFiles(buildFolder, "*.bank").Length == 0)
                {
                    var buildDirectories = Directory.GetDirectories(buildFolder);
                    var buildNames = new string[buildDirectories.Length];
                    for (var i = 0; i < buildDirectories.Length; i++)
                        buildNames[i] = Path.GetFileNameWithoutExtension(buildDirectories[i]);
                    return buildNames;
                }
            }
            catch
            {
            }

            return new string[0];
        }

        private static string VerionNumberToString(uint version)
        {
            var major = (version & 0x00FF0000) >> 16;
            var minor = (version & 0x0000FF00) >> 8;
            var patch = version & 0x000000FF;

            return major.ToString("X1") + "." + minor.ToString("X2") + "." + patch.ToString("X2");
        }

        public static string DurationString(float seconds)
        {
            var minutes = seconds / 60;
            var hours = minutes / 60;

            if (hours >= 1)
                return Pluralize(Mathf.FloorToInt(hours), "hour", "hours");
            if (minutes >= 1)
                return Pluralize(Mathf.FloorToInt(minutes), "minute", "minutes");
            if (seconds >= 1)
                return Pluralize(Mathf.FloorToInt(seconds), "second", "seconds");
            return "a moment";
        }

        public static string Pluralize(int count, string singular, string plural)
        {
            return string.Format("{0} {1}", count, count == 1 ? singular : plural);
        }

        private static void HandleBeforeAssemblyReload()
        {
            DestroySystem();
        }

        private static void HandleOnPausedModeChanged(PauseState state)
        {
            if (RuntimeManager.IsInitialized && RuntimeManager.HasBanksLoaded)
            {
                RuntimeManager.GetBus("bus:/").setPaused(EditorApplication.isPaused);
                RuntimeManager.StudioSystem.update();
            }
        }

        private static void HandleOnPlayModeChanged(PlayModeStateChange state)
        {
            // Entering Play Mode will cause scripts to reload, losing all state
            // This is the last chance to clean up FMOD and avoid a leak.
            if (state == PlayModeStateChange.ExitingEditMode) DestroySystem();
        }

        private static void RecreateSystem()
        {
            PreviewStop();
            DestroySystem();
            CreateSystem();
        }

        private static void DestroySystem()
        {
            if (system.isValid())
            {
                Debug.Log("FMOD Studio: Destroying editor system instance");
                system.release();
                system.clearHandle();
            }
        }

        private static void CreateSystem()
        {
            Debug.Log("FMOD Studio: Creating editor system instance");
            RuntimeUtils.EnforceLibraryOrder();

            var result = FMOD.Debug.Initialize(DEBUG_FLAGS.LOG, DEBUG_MODE.FILE, null, "fmod_editor.log");
            if (result != RESULT.OK)
                Debug.LogWarning(
                    "FMOD Studio: Cannot open fmod_editor.log. Logging will be disabled for importing and previewing");

            CheckResult(FMOD.Studio.System.create(out system));

            FMOD.System lowlevel;
            CheckResult(system.getCoreSystem(out lowlevel));

            // Use play-in-editor speaker mode for event browser preview and metering
            speakerMode = Settings.Instance.GetEditorSpeakerMode();
            CheckResult(lowlevel.setSoftwareFormat(0, speakerMode, 0));

            encryptionKey = Settings.Instance.EncryptionKey;
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                var studioAdvancedSettings = new ADVANCEDSETTINGS();
                CheckResult(system.setAdvancedSettings(studioAdvancedSettings, encryptionKey));
            }

            CheckResult(system.initialize(256, INITFLAGS.ALLOW_MISSING_PLUGINS | INITFLAGS.SYNCHRONOUS_UPDATE,
                FMOD.INITFLAGS.NORMAL, IntPtr.Zero));

            ChannelGroup master;
            CheckResult(lowlevel.getMasterChannelGroup(out master));
            DSP masterHead;
            CheckResult(master.getDSP(CHANNELCONTROL_DSP_INDEX.HEAD, out masterHead));
            CheckResult(masterHead.setMeteringEnabled(false, true));
        }

        public static void UpdateParamsOnEmitter(SerializedObject serializedObject, string path)
        {
            if (string.IsNullOrEmpty(path) || EventManager.EventFromPath(path) == null) return;

            var eventRef = EventManager.EventFromPath(path);
            serializedObject.ApplyModifiedProperties();
            if (serializedObject.isEditingMultipleObjects)
                foreach (var obj in serializedObject.targetObjects)
                    UpdateParamsOnEmitter(obj, eventRef);
            else
                UpdateParamsOnEmitter(serializedObject.targetObject, eventRef);
            serializedObject.Update();
        }

        private static void UpdateParamsOnEmitter(Object obj, EditorEventRef eventRef)
        {
            var emitter = obj as StudioEventEmitter;
            if (emitter == null)
                // Custom game object
                return;

            for (var i = 0; i < emitter.Params.Length; i++)
                if (!eventRef.LocalParameters.Exists(x => x.Name == emitter.Params[i].Name))
                {
                    var end = emitter.Params.Length - 1;
                    emitter.Params[i] = emitter.Params[end];
                    Array.Resize(ref emitter.Params, end);
                    i--;
                }
        }

        [MenuItem("FMOD/Help/Getting Started", priority = 2)]
        private static void OnlineGettingStarted()
        {
            OpenOnlineDocumentation("unity", "user-guide");
        }

        [MenuItem("FMOD/Help/Integration Manual", priority = 3)]
        public static void OnlineManual()
        {
            OpenOnlineDocumentation("unity");
        }

        [MenuItem("FMOD/Help/API Manual", priority = 4)]
        private static void OnlineAPIDocs()
        {
            OpenOnlineDocumentation("api");
        }

        [MenuItem("FMOD/Help/Support Forum", priority = 16)]
        private static void OnlineQA()
        {
            Application.OpenURL("https://qa.fmod.com/");
        }

        [MenuItem("FMOD/Help/Revision History", priority = 5)]
        private static void OnlineRevisions()
        {
            OpenOnlineDocumentation("api", "welcome-revision-history");
        }

        private static void OpenOnlineDocumentation(string section, string page = null)
        {
            const string Prefix = "https://fmod.com/resources/documentation-";
            var version = string.Format("{0:X}.{1:X}", VERSION.number >> 16, (VERSION.number >> 8) & 0xFF);
            string url;

            if (!string.IsNullOrEmpty(page))
                url = string.Format("{0}{1}?version={2}&page={3}.html", Prefix, section, version, page);
            else
                url = string.Format("{0}{1}?version={2}", Prefix, section, version);

            Application.OpenURL(url);
        }

        [MenuItem("FMOD/About Integration", priority = 7)]
        public static void About()
        {
            FMOD.System lowlevel;
            CheckResult(System.getCoreSystem(out lowlevel));

            uint version;
            CheckResult(lowlevel.getVersion(out version));

            EditorUtility.DisplayDialog("FMOD Studio Unity Integration",
                "Version: " + VerionNumberToString(version) +
                "\n\nCopyright \u00A9 Firelight Technologies Pty, Ltd. 2014-2021 \n\nSee LICENSE.TXT for additional license information.",
                "OK");
        }

        [MenuItem("FMOD/Consolidate Plugin Files")]
        public static void FolderMerge()
        {
            var root = "Assets/Plugins/FMOD";
            var lib = root + "/lib";
            var src = root + "/src";
            var runtime = src + "/Runtime";
            var editor = src + "/Editor";
            var addons = root + "/addons";

            var merge = EditorUtility.DisplayDialog("FMOD Plugin Consolidator",
                "This will consolidate most of the FMOD files into a single directory (Assets/Plugins/FMOD), only if the files have not been moved from their original location.\n\nThis should only need to be done if upgrading from before 2.0.",
                "OK", "Cancel");
            if (merge)
            {
                if (!Directory.Exists(addons))
                    AssetDatabase.CreateFolder(root, "addons");
                if (!Directory.Exists(src))
                    AssetDatabase.CreateFolder(root, "src");
                if (!Directory.Exists(runtime))
                    AssetDatabase.CreateFolder(src, "Runtime");
                if (!Directory.Exists(lib))
                    AssetDatabase.CreateFolder(root, "lib");
                if (!Directory.Exists(lib + "/mac"))
                    AssetDatabase.CreateFolder(lib, "mac");
                if (!Directory.Exists(lib + "/win"))
                    AssetDatabase.CreateFolder(lib, "win");
                if (!Directory.Exists(lib + "/linux"))
                    AssetDatabase.CreateFolder(lib, "linux");
                if (!Directory.Exists(lib + "/linux/x86"))
                    AssetDatabase.CreateFolder(lib + "/linux", "x86");
                if (!Directory.Exists(lib + "/linux/x86_64"))
                    AssetDatabase.CreateFolder(lib + "/linux", "x86_64");
                if (!Directory.Exists(lib + "/android"))
                    AssetDatabase.CreateFolder(lib, "android");

                // Scripts
                var files = Directory.GetFiles(root, "*.cs", SearchOption.TopDirectoryOnly);
                foreach (var filePath in files) MoveAsset(filePath, runtime + "/" + Path.GetFileName(filePath));
                MoveAsset(root + "/fmodplugins.cpp", runtime + "/fmodplugins.cpp");
                MoveAsset(root + "/Timeline", runtime + "/Timeline");
                MoveAsset("Assets/Plugins/FMOD/Wrapper", runtime + "/wrapper");
                MoveAsset("Assets/Plugins/Editor/FMOD", editor);
                MoveAsset("Assets/Plugins/Editor/FMOD/Timeline", editor + "/Timeline");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/FMOD/Runtime") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/FMOD/Runtime"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/FMOD/Runtime");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/Editor/FMOD") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/Editor/FMOD"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/Editor/FMOD");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/Editor") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/Editor"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/Editor");
                // GoogleVR
                if (AssetDatabase.IsValidFolder("Assets/GoogleVR"))
                    MoveAsset("Assets/GoogleVR", addons + "/GoogleVR");
                // ResonanceAudio
                MoveAsset("Assets/ResonanceAudio", addons + "/ResonanceAudio");
                // GVR Audio
                if (AssetDatabase.IsValidFolder("Assets/Plugins/gvraudio.bundle"))
                    MoveAsset("Assets/Plugins/gvraudio.bundle", lib + "/mac/gvraudio.bundle");
                // Cache files
                MoveAsset("Assets/Resources/FMODStudioSettings.asset", root + "/Resources/FMODStudioSettings.asset");
                if (AssetDatabase.IsValidFolder("Assets/Resources") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Resources"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Resources");
                MoveAsset("Assets/FMODStudioCache.asset", root + "/Resources/FMODStudioCache.asset");
                if (AssetDatabase.FindAssets("Assets/FMODStudioCache.asset").Length != 0)
                    AssetDatabase.MoveAssetToTrash("Assets/FMODStudioCache.asset");
                // Android libs
                string[] archs = {"armeabi-v7a", "x86", "arm64-v8a"};
                foreach (var arch in archs)
                {
                    MoveAsset("Assets/Plugins/Android/libs/" + arch + "/libfmod.so",
                        lib + "/android/" + arch + "/libfmod.so");
                    MoveAsset("Assets/Plugins/Android/libs/" + arch + "/libfmodL.so",
                        lib + "/android/" + arch + "/libfmodL.so");
                    MoveAsset("Assets/Plugins/Android/libs/" + arch + "/libfmodstudio.so",
                        lib + "/android/" + arch + "/libfmodstudio.so");
                    MoveAsset("Assets/Plugins/Android/libs/" + arch + "/libfmodstudioL.so",
                        lib + "/android/" + arch + "/libfmodstudioL.so");
                    MoveAsset("Assets/Plugins/Android/libs/" + arch + "/libresonanceaudio.so",
                        lib + "/android/" + arch + "/libresonanceaudio.so");
                    MoveAsset("Assets/Plugins/Android/libs/" + arch + "/libgvraudio.so",
                        lib + "/android/" + arch + "/libgvraudio.so");
                    if (AssetDatabase.IsValidFolder("Assets/Plugins/Android/libs/" + arch) &&
                        AssetDatabase.FindAssets("", new[] {"Assets/Plugins/Android/libs/" + arch}).Length == 0)
                        AssetDatabase.MoveAssetToTrash("Assets/Plugins/Android/libs/" + arch);
                }

                MoveAsset("Assets/Plugins/Android/fmod.jar", lib + "/android/fmod.jar");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/Android/libs") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/Android/libs"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/Android/libs");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/Android") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/Android"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/Android");
                AssetDatabase.
                    // Mac libs
                    MoveAsset("Assets/Plugins/fmodstudio.bundle", lib + "/mac/fmodstudio.bundle");
                MoveAsset("Assets/Plugins/fmodstudioL.bundle", lib + "/mac/fmodstudioL.bundle");
                MoveAsset("Assets/Plugins/resonanceaudio.bundle", lib + "/mac/resonanceaudio.bundle");
                // iOS libs
                MoveAsset("Assets/Plugins/iOS/libfmodstudiounityplugin.a", lib + "/ios/libfmodstudiounityplugin.a");
                MoveAsset("Assets/Plugins/iOS/libfmodstudiounitypluginL.a", lib + "/ios/libfmodstudiounitypluginL.a");
                MoveAsset("Assets/Plugins/iOS/libgvraudio.a", lib + "/ios/libgvraudio.a");
                MoveAsset("Assets/Plugins/iOS/libresonanceaudio.a", lib + "/ios/libresonanceaudio.a");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/iOS") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/iOS"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/iOS");
                // tvOS libs
                MoveAsset("Assets/Plugins/tvOS/libfmodstudiounityplugin.a", lib + "/tvos/libfmodstudiounityplugin.a");
                MoveAsset("Assets/Plugins/tvOS/libfmodstudiounitypluginL.a", lib + "/tvos/libfmodstudiounitypluginL.a");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/tvOS") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/tvOS"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/tvOS");
                // UWP libs
                archs = new[] {"arm", "x64", "x86"};
                foreach (var arch in archs)
                {
                    MoveAsset("Assets/Plugins/UWP/" + arch + "/fmod.dll", lib + "/uwp/" + arch + "/fmod.dll");
                    MoveAsset("Assets/Plugins/UWP/" + arch + "/fmodL.dll", lib + "/uwp/" + arch + "/fmodL.dll");
                    MoveAsset("Assets/Plugins/UWP/" + arch + "/fmodstudio.dll",
                        lib + "/uwp/" + arch + "/fmodstudio.dll");
                    MoveAsset("Assets/Plugins/UWP/" + arch + "/fmodstudioL.dll",
                        lib + "/uwp/" + arch + "/fmodstudioL.dll");
                    if (AssetDatabase.IsValidFolder("Assets/Plugins/UWP/" + arch) &&
                        AssetDatabase.FindAssets("", new[] {"Assets/Plugins/UWP/" + arch}).Length == 0)
                        AssetDatabase.MoveAssetToTrash("Assets/Plugins/UWP/" + arch);
                }

                if (AssetDatabase.IsValidFolder("Assets/Plugins/UWP") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/UWP"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/UWP");
                // HTML5 libs
                MoveAsset("Assets/Plugins/WebGL/libfmodstudiounityplugin.bc",
                    lib + "/html5/libfmodstudiounityplugin.bc");
                MoveAsset("Assets/Plugins/WebGL/libfmodstudiounitypluginL.bc",
                    lib + "/html5/libfmodstudiounitypluginL.bc");
                if (AssetDatabase.IsValidFolder("Assets/Plugins/WebGL") &&
                    AssetDatabase.FindAssets("", new[] {"Assets/Plugins/WebGL"}).Length == 0)
                    AssetDatabase.MoveAssetToTrash("Assets/Plugins/WebGL");
                // PS4 libs (optional)
                if (AssetDatabase.IsValidFolder("Assets/Plugins/PS4"))
                {
                    MoveAsset("Assets/Plugins/PS4/libfmod.prx", lib + "/ps4/libfmod.prx");
                    MoveAsset("Assets/Plugins/PS4/libfmodL.prx", lib + "/ps4/libfmodL.prx");
                    MoveAsset("Assets/Plugins/PS4/libfmodstudio.prx", lib + "/ps4/libfmodstudio.prx");
                    MoveAsset("Assets/Plugins/PS4/libfmodstudioL.prx", lib + "/ps4/libfmodstudioL.prx");
                    MoveAsset("Assets/Plugins/PS4/resonanceaudio.prx", lib + "/ps4/resonanceaudio.prx");
                    if (AssetDatabase.IsValidFolder("Assets/Plugins/PS4") &&
                        AssetDatabase.FindAssets("", new[] {"Assets/Plugins/PS4"}).Length == 0)
                        AssetDatabase.MoveAssetToTrash("Assets/Plugins/PS4");
                }

                // Switch libs (optional)
                if (AssetDatabase.IsValidFolder("Assets/Plugins/Switch"))
                {
                    MoveAsset("Assets/Plugins/Switch/libfmodstudiounityplugin.a",
                        lib + "/switch/libfmodstudiounityplugin.a");
                    MoveAsset("Assets/Plugins/Switch/libfmodstudiounitypluginL.a",
                        lib + "/switch/libfmodstudiounitypluginL.a");
                    if (AssetDatabase.IsValidFolder("Assets/Plugins/Switch") &&
                        AssetDatabase.FindAssets("", new[] {"Assets/Plugins/Switch"}).Length == 0)
                        AssetDatabase.MoveAssetToTrash("Assets/Plugins/Switch");
                }

                // Xbox One libs (optional)
                if (AssetDatabase.IsValidFolder("Assets/Plugins/XboxOne"))
                {
                    MoveAsset("Assets/Plugins/XboxOne/fmod.dll", lib + "/xboxone/fmod.dll");
                    MoveAsset("Assets/Plugins/XboxOne/fmodL.dll", lib + "/xboxone/fmodL.dll");
                    MoveAsset("Assets/Plugins/XboxOne/fmodstudio.dll", lib + "/xboxone/fmodstudio.dll");
                    MoveAsset("Assets/Plugins/XboxOne/fmodstudioL.dll", lib + "/xboxone/fmodstudioL.dll");
                    if (AssetDatabase.IsValidFolder("Assets/Plugins/XboxOne") &&
                        AssetDatabase.FindAssets("", new[] {"Assets/Plugins/XboxOne"}).Length == 0)
                        AssetDatabase.MoveAssetToTrash("Assets/Plugins/XboxOne");
                }

                // Linux libs
                archs = new[] {"x86", "x86_64"};
                foreach (var arch in archs)
                {
                    MoveAsset("Assets/Plugins/" + arch + "/libfmod.so", lib + "/linux/" + arch + "/libfmod.so");
                    MoveAsset("Assets/Plugins/" + arch + "/libfmodL.so", lib + "/linux/" + arch + "/libfmodL.so");
                    MoveAsset("Assets/Plugins/" + arch + "/libfmodstudio.so",
                        lib + "/linux/" + arch + "/libfmodstudio.so");
                    MoveAsset("Assets/Plugins/" + arch + "/libfmodstudioL.so",
                        lib + "/linux/" + arch + "/libfmodstudio.so");
                    MoveAsset("Assets/Plugins/" + arch + "/libgvraudio.so", lib + "/linux/" + arch + "/libgvraudio.so");
                    MoveAsset("Assets/Plugins/" + arch + "/libresonanceaudio.so",
                        lib + "/linux/" + arch + "/libresonanceaudio.so");
                    // The folders will be deleted after the windows libs are moved.
                }

                // Windows libs
                foreach (var arch in archs)
                {
                    MoveAsset("Assets/Plugins/" + arch + "/fmodstudio.dll", lib + "/win/" + arch + "/fmodstudio.dll");
                    MoveAsset("Assets/Plugins/" + arch + "/fmodstudioL.dll", lib + "/win/" + arch + "/fmodstudioL.dll");
                    MoveAsset("Assets/Plugins/" + arch + "/gvraudio.dll", lib + "/win/" + arch + "/gvraudio.dll");
                    MoveAsset("Assets/Plugins/" + arch + "/resonanceaudio.dll",
                        lib + "/win/" + arch + "/resonanceaudio.dll");
                    if (AssetDatabase.IsValidFolder("Assets/Plugins/" + arch) &&
                        AssetDatabase.FindAssets("", new[] {"Assets/Plugins/" + arch}).Length == 0)
                        AssetDatabase.MoveAssetToTrash("Assets/Plugins/" + arch);
                }

                Debug.Log("Folder merge finished!");
            }
        }

        private static void MoveAsset(string from, string to)
        {
            if (AssetDatabase.IsValidFolder(to))
            {
                // Need to move all sub files/folders manually
                var files = Directory.GetFiles(from, "*", SearchOption.TopDirectoryOnly);
                foreach (var fileName in files)
                    AssetDatabase.MoveAsset(fileName, to + '/' + Path.GetFileName(fileName));
                var directories = Directory.GetDirectories(from, "*", SearchOption.AllDirectories);
                foreach (var dir in directories)
                {
                    var subDir = dir.Replace(from, "");
                    files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                    foreach (var fileName in files)
                        AssetDatabase.MoveAsset(fileName, to + '/' + subDir + '/' + Path.GetFileName(fileName));
                }
            }
            else
            {
                var result = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(result)) Debug.LogWarning("[FMOD] Failed to move " + from + " : " + result);
            }
        }

        public static void PreviewEvent(EditorEventRef eventRef, Dictionary<string, float> previewParamValues)
        {
            var load = true;
            if (previewEventDesc.isValid())
            {
                Guid guid;
                previewEventDesc.getID(out guid);
                if (guid == eventRef.Guid)
                    load = false;
                else
                    PreviewStop();
            }

            if (load)
            {
                masterBanks.Clear();
                previewBanks.Clear();

                foreach (var masterBankRef in EventManager.MasterBanks)
                {
                    Bank masterBank;
                    CheckResult(System.loadBankFile(masterBankRef.Path, LOAD_BANK_FLAGS.NORMAL, out masterBank));
                    masterBanks.Add(masterBank);
                }

                if (!EventManager.MasterBanks.Exists(x => eventRef.Banks.Contains(x)))
                {
                    var bankName = eventRef.Banks[0].Name;
                    var banks = EventManager.Banks.FindAll(x => x.Name.Contains(bankName));
                    foreach (var bank in banks)
                    {
                        Bank previewBank;
                        CheckResult(System.loadBankFile(bank.Path, LOAD_BANK_FLAGS.NORMAL, out previewBank));
                        previewBanks.Add(previewBank);
                    }
                }
                else
                {
                    foreach (var previewBank in previewBanks) previewBank.clearHandle();
                }

                CheckResult(System.getEventByID(eventRef.Guid, out previewEventDesc));
                CheckResult(previewEventDesc.createInstance(out previewEventInstance));
            }

            foreach (var param in eventRef.Parameters)
            {
                PARAMETER_DESCRIPTION paramDesc;
                CheckResult(previewEventDesc.getParameterDescriptionByName(param.Name, out paramDesc));
                param.ID = paramDesc.id;
                if (param.IsGlobal)
                    CheckResult(System.setParameterByID(param.ID, previewParamValues[param.Name]));
                else
                    PreviewUpdateParameter(param.ID, previewParamValues[param.Name]);
            }

            CheckResult(previewEventInstance.start());
            PreviewState = PreviewState.Playing;
        }

        public static void PreviewUpdateParameter(PARAMETER_ID id, float paramValue)
        {
            if (previewEventInstance.isValid()) CheckResult(previewEventInstance.setParameterByID(id, paramValue));
        }

        public static void PreviewUpdatePosition(float distance, float orientation)
        {
            if (previewEventInstance.isValid())
            {
                // Listener at origin
                var pos = new ATTRIBUTES_3D();
                pos.position.x = (float) Math.Sin(orientation) * distance;
                pos.position.y = (float) Math.Cos(orientation) * distance;
                pos.forward.x = 1.0f;
                pos.up.z = 1.0f;
                CheckResult(previewEventInstance.set3DAttributes(pos));
            }
        }

        public static void PreviewPause()
        {
            if (previewEventInstance.isValid())
            {
                bool paused;
                CheckResult(previewEventInstance.getPaused(out paused));
                CheckResult(previewEventInstance.setPaused(!paused));
                PreviewState = paused ? PreviewState.Playing : PreviewState.Paused;
            }
        }

        public static void PreviewStop()
        {
            if (previewEventInstance.isValid())
            {
                previewEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                previewEventInstance.release();
                previewEventInstance.clearHandle();
                previewEventDesc.clearHandle();
                previewBanks.ForEach(x =>
                {
                    x.unload();
                    x.clearHandle();
                });
                masterBanks.ForEach(x =>
                {
                    x.unload();
                    x.clearHandle();
                });
                PreviewState = PreviewState.Stopped;
            }
        }

        public static float[] GetMetering()
        {
            FMOD.System lowlevel;
            CheckResult(System.getCoreSystem(out lowlevel));
            ChannelGroup master;
            CheckResult(lowlevel.getMasterChannelGroup(out master));
            DSP masterHead;
            CheckResult(master.getDSP(CHANNELCONTROL_DSP_INDEX.HEAD, out masterHead));

            DSP_METERING_INFO outputMetering;
            CheckResult(masterHead.getMeteringInfo(IntPtr.Zero, out outputMetering));

            SPEAKERMODE mode;
            int rate, raw;
            lowlevel.getSoftwareFormat(out rate, out mode, out raw);
            int channels;
            lowlevel.getSpeakerModeChannels(mode, out channels);

            var data = new float[channels];
            if (outputMetering.numchannels > 0) Array.Copy(outputMetering.rmslevel, data, channels);
            return data;
        }

        private static void AsyncConnectCallback(IAsyncResult result)
        {
            try
            {
                socket.EndConnect(result);
            }
            catch (Exception)
            {
            }
            finally
            {
                socketConnection = null;
            }
        }

        public static bool IsConnectedToStudio()
        {
            try
            {
                if (socket != null && socket.Connected)
                    if (SendScriptCommand("true"))
                        return true;

                if (socketConnection == null)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketConnection = socket.BeginConnect("127.0.0.1", StudioScriptPort, AsyncConnectCallback, null);
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public static bool SendScriptCommand(string command)
        {
            var commandBytes = Encoding.UTF8.GetBytes(command);
            try
            {
                ScriptStream.Write(commandBytes, 0, commandBytes.Length);
                var commandReturnBytes = new byte[128];
                var read = ScriptStream.Read(commandReturnBytes, 0, 128);
                var result = Encoding.UTF8.GetString(commandReturnBytes, 0, read - 1);
                return result.Contains("true");
            }
            catch (Exception)
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream = null;
                }

                return false;
            }
        }


        public static string GetScriptOutput(string command)
        {
            var commandBytes = Encoding.UTF8.GetBytes(command);
            try
            {
                ScriptStream.Write(commandBytes, 0, commandBytes.Length);
                var commandReturnBytes = new byte[2048];
                var read = ScriptStream.Read(commandReturnBytes, 0, commandReturnBytes.Length);
                var result = Encoding.UTF8.GetString(commandReturnBytes, 0, read - 1);
                if (result.StartsWith("out():")) return result.Substring(6).Trim();
                return null;
            }
            catch (Exception)
            {
                networkStream.Close();
                networkStream = null;
                return null;
            }
        }

        private static string GetMasterBank()
        {
            GetScriptOutput("masterBankFolder = studio.project.workspace.masterBankFolder;");
            var bankCountString = GetScriptOutput("masterBankFolder.items.length;");
            var bankCount = int.Parse(bankCountString);
            for (var i = 0; i < bankCount; i++)
            {
                var isMaster =
                    GetScriptOutput(string.Format("masterBankFolder.items[{1}].isOfExactType(\"MasterBank\");", i));
                if (isMaster == "true")
                {
                    var guid = GetScriptOutput(string.Format("masterBankFolder.items[{1}].id;", i));
                    return guid;
                }
            }

            return "";
        }

        private static bool CheckForNameConflict(string folderGuid, string eventName)
        {
            const string checkForNameConflictFunc =
                @"function(folderGuid, eventName) {
                    var nameConflict = false;
                    studio.project.lookup(folderGuid).items.forEach(function(val) {
                        nameConflict |= val.name == eventName;
                    });
                    return nameConflict;
                }";

            var conflictBool = GetScriptOutput(string.Format("({0})(\"{1}\", \"{2}\")", checkForNameConflictFunc,
                folderGuid, eventName));
            return conflictBool == "1";
        }

        public static string CreateStudioEvent(string eventPath, string eventName)
        {
            var folders = eventPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var folderGuid = GetScriptOutput("studio.project.workspace.masterEventFolder.id;");

            const string getFolderGuidFunc =
                @"function(parentGuid, folderName) {
                    folderGuid = """";
                    studio.project.lookup(parentGuid).items.forEach(function(val) {
                        folderGuid = val.isOfType(""EventFolder"") && val.name == folderName ? val.id : folderGuid;
                    });
                    if (folderGuid == """")
                    {
                        var newFolder = studio.project.create(""EventFolder"");
                        newFolder.name = folderName;
                        newFolder.folder = studio.project.lookup(parentGuid);
                        folderGuid = newFolder.id;
                    }
                    return folderGuid;
                }";

            for (var i = 0; i < folders.Length; i++)
            {
                var parentGuid = folderGuid;
                folderGuid = GetScriptOutput(string.Format("({0})(\"{1}\", \"{2}\")", getFolderGuidFunc, parentGuid,
                    folders[i]));
            }

            if (CheckForNameConflict(folderGuid, eventName))
            {
                EditorUtility.DisplayDialog("Name Conflict",
                    string.Format("The event {0} already exists under {1}", eventName, eventPath), "OK");
                return null;
            }

            const string createEventFunc =
                @"function(eventName, folderGuid) {
                    event = studio.project.create(""Event"");
                    event.note = ""Placeholder created via Unity"";
                    event.name = eventName;
                    event.folder = studio.project.lookup(folderGuid);

                    track = studio.project.create(""GroupTrack"");
                    track.mixerGroup.output = event.mixer.masterBus;
                    track.mixerGroup.name = ""Audio 1"";
                    event.relationships.groupTracks.add(track);

                    tag = studio.project.create(""Tag"");
                    tag.name = ""placeholder"";
                    tag.folder = studio.project.workspace.masterTagFolder;
                    event.relationships.tags.add(tag);

                    return event.id;
                }";

            var eventGuid =
                GetScriptOutput(string.Format("({0})(\"{1}\", \"{2}\")", createEventFunc, eventName, folderGuid));
            return eventGuid;
        }

        [InitializeOnLoadMethod]
        private static void CleanObsoleteFiles()
        {
            if (Environment.GetCommandLineArgs().Any(a => a == "-exportPackage"))
                // Don't delete anything or it won't be included in the package
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                // Messing with the asset database while entering play mode causes a NullReferenceException
                return;
            if (AssetDatabase.IsValidFolder("Assets/Plugins/FMOD/obsolete"))
            {
                EditorApplication.LockReloadAssemblies();

                var guids = AssetDatabase.FindAssets(string.Empty, new[] {"Assets/Plugins/FMOD/obsolete"});
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.DeleteAsset(path)) Debug.LogFormat("FMOD: Removed obsolete file {0}", path);
                }

                if (AssetDatabase.MoveAssetToTrash("Assets/Plugins/FMOD/obsolete"))
                    Debug.LogFormat("FMOD: Removed obsolete folder Assets/Plugins/FMOD/obsolete");
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        [InitializeOnLoadMethod]
        private static void BeginSharedLibraryUpdate()
        {
            EditorApplication.delayCall += UpdateSharedLibraries;
        }

        private static void UpdateSharedLibraries()
        {
            if (AssetDatabase.IsValidFolder(StagingPrefix))
            {
                var libInfoList = new List<LibInfo>
                {
                    new LibInfo
                    {
                        cpu = "x86", os = "Windows", lib = "/win/x86/fmodstudioL.dll",
                        buildTarget = BuildTarget.StandaloneWindows
                    },
                    new LibInfo
                    {
                        cpu = "x86_64", os = "Windows", lib = "/win/x86_64/fmodstudioL.dll",
                        buildTarget = BuildTarget.StandaloneWindows64
                    },
#if !UNITY_2019_2_OR_NEWER
                    new LibInfo() {cpu = "x86", os = "Linux", lib = "/linux/x86/libfmodstudioL.so",  buildTarget =
 BuildTarget.StandaloneLinux},
#endif
                    new LibInfo
                    {
                        cpu = "x86_64", os = "Linux", lib = "/linux/x86_64/libfmodstudioL.so",
                        buildTarget = BuildTarget.StandaloneLinux64
                    },
                    new LibInfo
                    {
                        cpu = "AnyCPU", os = "OSX", lib = "/mac/fmodstudioL.bundle",
                        buildTarget = BuildTarget.StandaloneOSX
                    }
                };

                if (Settings.Instance.SharedLibraryUpdateStage ==
                    Settings.SharedLibraryUpdateStages.DisableExistingLibraries)
                {
                    var areLibsThere = false;
                    foreach (var libInfo in libInfoList)
                    {
                        var targetPath = LibPrefix + libInfo.lib;
                        var pluginImporter = AssetImporter.GetAtPath(targetPath) as PluginImporter;
                        if (pluginImporter != null && pluginImporter.GetCompatibleWithEditor())
                        {
                            pluginImporter.SetCompatibleWithEditor(false);
                            pluginImporter.SetCompatibleWithAnyPlatform(false);
                            EditorUtility.SetDirty(pluginImporter);
                            pluginImporter.SaveAndReimport();
                            areLibsThere = true;
                        }
                    }

                    if (areLibsThere)
                    {
                        Settings.Instance.SharedLibraryUpdateStage = Settings.SharedLibraryUpdateStages.RestartUnity;
                        Settings.Instance.SharedLibraryTimeSinceStart = EditorApplication.timeSinceStartup;
                        EditorUtility.SetDirty(Settings.Instance);

                        if (EditorUtility.DisplayDialog("Restart Unity",
                            "Please restart Unity to update the FMOD native libraries.", "Restart Unity", "Cancel"))
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                Settings.Instance.SharedLibraryUpdateStage =
                                    Settings.SharedLibraryUpdateStages.CopyNewLibraries;
                                EditorUtility.SetDirty(Settings.Instance);

                                // Restart Unity
                                EditorApplication.OpenProject(Environment.CurrentDirectory);

                                return;
                            }
                    }
                    else
                    {
                        Settings.Instance.SharedLibraryUpdateStage =
                            Settings.SharedLibraryUpdateStages.CopyNewLibraries;
                    }
                }

                if (Settings.Instance.SharedLibraryUpdateStage == Settings.SharedLibraryUpdateStages.RestartUnity)
                    if (EditorApplication.timeSinceStartup < Settings.Instance.SharedLibraryTimeSinceStart)
                        // Unity has been closed down since Settings.SharedLibraryUpdateStages.RestartUnity was set
                        Settings.Instance.SharedLibraryUpdateStage =
                            Settings.SharedLibraryUpdateStages.CopyNewLibraries;
                if (Settings.Instance.SharedLibraryUpdateStage == Settings.SharedLibraryUpdateStages.CopyNewLibraries)
                {
                    Settings.Instance.SharedLibraryUpdateStage =
                        Settings.SharedLibraryUpdateStages.DisableExistingLibraries;
                    EditorUtility.SetDirty(Settings.Instance);

                    foreach (var libInfo in libInfoList)
                    {
                        var targetPath = LibPrefix + libInfo.lib;
                        var sourcePath = StagingPrefix + libInfo.lib;

                        AssetDatabase.DeleteAsset(targetPath);
                        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
                            Debug.LogError(string.Format("FMOD: Could not copy {0} to {1}", sourcePath, targetPath));
                        var pluginImporter = AssetImporter.GetAtPath(targetPath) as PluginImporter;
                        if (pluginImporter != null)
                        {
                            pluginImporter.ClearSettings();
                            pluginImporter.SetCompatibleWithEditor(true);
                            pluginImporter.SetCompatibleWithAnyPlatform(false);
                            pluginImporter.SetCompatibleWithPlatform(libInfo.buildTarget, true);
                            pluginImporter.SetEditorData("CPU", libInfo.cpu);
                            pluginImporter.SetEditorData("OS", libInfo.os);
                            EditorUtility.SetDirty(pluginImporter);
                            pluginImporter.SaveAndReimport();
                        }
                    }

                    if (AssetDatabase.MoveAssetToTrash(StagingPrefix))
                        Debug.LogFormat("FMOD: Removed staging folder " + StagingPrefix);
                }
            }
        }

        private struct LibInfo
        {
            public string cpu;
            public string os;
            public string lib;
            public BuildTarget buildTarget;
        }
    }

    public class BuildStatusWatcher
    {
        public static Action OnBuildStarted;
        public static Action OnBuildEnded;

        private static bool buildInProgress;

        private static void SetBuildInProgress(bool inProgress)
        {
            if (inProgress != buildInProgress)
            {
                buildInProgress = inProgress;

                if (buildInProgress)
                {
                    EditorApplication.update += PollBuildStatus;

                    if (OnBuildStarted != null) OnBuildStarted();
                }
                else
                {
                    EditorApplication.update -= PollBuildStatus;

                    if (OnBuildEnded != null) OnBuildEnded();
                }
            }
        }

        private static void PollBuildStatus()
        {
            SetBuildInProgress(BuildPipeline.isBuildingPlayer);
        }

#if UNITY_2018_1_OR_NEWER
        private class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            public void OnPostprocessBuild(BuildReport report)
            {
                SetBuildInProgress(false);
            }

            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                SetBuildInProgress(true);
            }
        }
#else
        private class BuildProcessor : IPreprocessBuild, IPostprocessBuild
        {
            public int callbackOrder { get { return 0; } }

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                SetBuildInProgress(true);
            }

            public void OnPostprocessBuild(BuildTarget target, string path)
            {
                SetBuildInProgress(false);
            }
        }
#endif

#if UNITY_SCRIPTABLEBUILDPIPELINE_EXIST
        [InitializeOnLoadMethod]
        static void RegisterScriptableBuildCallbacks()
        {
            BuildCallbacks callbacks = ContentPipeline.BuildCallbacks;

            callbacks.PostDependencyCallback += (parameters, dependencyData) => {
                SetBuildInProgress(true);
                return ReturnCode.Success;
            };

            callbacks.PostWritingCallback += (parameters, dependencyData, writeData, results) => {
                SetBuildInProgress(false);
                return ReturnCode.Success;
            };
        }
#endif
    }

    public static class SerializedPropertyExtensions
    {
        public static bool ArrayContains(this SerializedProperty array, Func<SerializedProperty, bool> predicate)
        {
            return FindArrayIndex(array, predicate) >= 0;
        }

        public static bool ArrayContains(this SerializedProperty array, string subPropertyName,
            Func<SerializedProperty, bool> predicate)
        {
            return FindArrayIndex(array, subPropertyName, predicate) >= 0;
        }

        public static int FindArrayIndex(this SerializedProperty array, Func<SerializedProperty, bool> predicate)
        {
            for (var i = 0; i < array.arraySize; ++i)
            {
                var current = array.GetArrayElementAtIndex(i);

                if (predicate(current)) return i;
            }

            return -1;
        }

        public static int FindArrayIndex(this SerializedProperty array, string subPropertyName,
            Func<SerializedProperty, bool> predicate)
        {
            for (var i = 0; i < array.arraySize; ++i)
            {
                var current = array.GetArrayElementAtIndex(i);
                var subProperty = current.FindPropertyRelative(subPropertyName);

                if (predicate(subProperty)) return i;
            }

            return -1;
        }

        public static void ArrayAdd(this SerializedProperty array, Action<SerializedProperty> initialize)
        {
            array.InsertArrayElementAtIndex(array.arraySize);
            initialize(array.GetArrayElementAtIndex(array.arraySize - 1));
        }

        public static void ArrayClear(this SerializedProperty array)
        {
            while (array.arraySize > 0) array.DeleteArrayElementAtIndex(array.arraySize - 1);
        }
    }

    public class NoIndentScope : IDisposable
    {
        private readonly int oldIndentLevel;

        public NoIndentScope()
        {
            oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
        }

        public void Dispose()
        {
            EditorGUI.indentLevel = oldIndentLevel;
        }
    }

    public class NaturalComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return EditorUtility.NaturalCompare(a, b);
        }
    }
}