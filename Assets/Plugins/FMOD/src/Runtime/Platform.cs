﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMOD
{
    public partial class VERSION
    {
#if DEVELOPMENT_BUILD
        public const string dllSuffix = "L";
#else
        public const string dllSuffix = "";
#endif
    }
}

namespace FMOD.Studio
{
    public partial class STUDIO_VERSION
    {
#if DEVELOPMENT_BUILD
        public const string dllSuffix = "L";
#else
        public const string dllSuffix = "";
#endif
    }
}

namespace FMODUnity
{
    public class PlatformCallbackHandler : ScriptableObject
    {
        // A hook for custom initialization logic. RuntimeManager.Initialize calls this
        // just before calling system.Initialize.
        // Call reportResult() with the result of each FMOD call to use FMOD's error handling logic.
        public virtual void PreInitialize(FMOD.Studio.System system, Action<FMOD.RESULT, string> reportResult)
        {
        }
    }

    // This class holds per-platform settings and provides hooks for platform-specific behaviour.
    // Each platform has a parent platform, forming a hierarchy that is rooted at PlatformDefault.
    // By default a platform inherits all of its properties from its parent platform; this behaviour
    // can be overridden for each property.
    //
    // There is at least one concrete derived class for each supported platform; these classes use
    // [InitializeOnLoad] and a static constructor to register themselves as supported platforms by
    // calling Settings.AddPlatformTemplate. The user can also create instances of the PlatformGroup
    // class and use them to group platforms that have settings in common.
    public abstract class Platform : ScriptableObject
    {
        // This is a persistent identifier. It is used:
        // * To link platforms together at load time
        // * To avoid creating duplicate platforms from templates (in Settings.OnEnable)
        // * As a key for SettingsEditor UI state
        // It should be kept stable for concrete platforms (like PlatformWindows) to support
        // settings migration in the future.
        [SerializeField]
        private string identifier;

        public string Identifier
        {
            get
            {
                return identifier;
            }

            set
            {
                identifier = value;
            }
        }

        // The display name to show for this platform in the UI.
        public abstract string DisplayName { get; }

        // Declares the Unity RuntimePlatforms and BuildTargets this platform implements.
        public abstract void DeclareUnityMappings(Settings settings);

#if UNITY_EDITOR
        // The old FMOD platform identifier that this platform corresponds to, for settings migration.
        public abstract Legacy.Platform LegacyIdentifier { get; }
#endif

        public const float DefaultPriority = 0;

        // The priority to use when finding a platform to support the current Unity runtime
        // platform (higher priorities are tried first).
        public virtual float Priority { get { return DefaultPriority; } }

        // Determines whether this platform matches the current environment. When more than one
        // platform implements the current Unity runtime platform, FMOD for Unity will use the
        // highest-priority platform that returns true from MatchesCurrentEnvironment.
        public virtual bool MatchesCurrentEnvironment { get { return true; } }

        // Whether this platform is a fixed part of the FMOD for Unity settings, or can be
        // added/removed by the user.
        public virtual bool IsIntrinsic { get { return false; } }

        // A hook for platform-specific initialization logic. RuntimeManager.Initialize calls this
        // before calling FMOD.Studio.System.create.
        public virtual void PreSystemCreate(Action<FMOD.RESULT, string> reportResult)
        {
        }

        // A hook for platform-specific initialization logic. RuntimeManager.Initialize calls this
        // just before calling studioSystem.Initialize.
        public virtual void PreInitialize(FMOD.Studio.System studioSystem)
        {
        }

        // The folder in which FMOD .bank files are stored. Used when loading banks.
        public virtual string GetBankFolder()
        {
            return Application.streamingAssetsPath;
        }

#if UNITY_EDITOR
        [Flags]
        public enum BinaryType
        {
            Release = 1,
            Logging = 2,
            Optional = 4,
            AllVariants = 8,
            All = Release | Logging | Optional | AllVariants
        }

        protected virtual IEnumerable<string> GetBinaryPaths(BuildTarget buildTarget, BinaryType binaryType, string prefix)
        {
            string assetBasePath = GetBinaryAssetBasePath();
            bool allVariants = (binaryType & BinaryType.AllVariants) == BinaryType.AllVariants;

            if ((binaryType & BinaryType.Release) == BinaryType.Release)
            {
                foreach (string path in GetRelativeBinaryPaths(buildTarget, allVariants, ""))
                {
                    yield return string.Format("{0}/{1}/{2}", prefix, assetBasePath, path);
                }
            }

            if ((binaryType & BinaryType.Logging) == BinaryType.Logging)
            {
                foreach (string path in GetRelativeBinaryPaths(buildTarget, allVariants, "L"))
                {
                    yield return string.Format("{0}/{1}/{2}", prefix, assetBasePath, path);
                }
            }

            if ((binaryType & BinaryType.Optional) == BinaryType.Optional)
            {
                foreach (string path in GetRelativeOptionalBinaryPaths(buildTarget, allVariants))
                {
                    yield return string.Format("{0}/{1}/{2}", prefix, assetBasePath, path);
                }
            }
        }

        // Called by Settings.CanBuildTarget to get the required binaries for the current
        // build target and logging state.
        public virtual IEnumerable<string> GetBinaryFilePaths(BuildTarget buildTarget, BinaryType binaryType)
        {
            return GetBinaryPaths(buildTarget, binaryType, Application.dataPath);
        }

        // Called by Settings.SelectBinaries to get:
        // * The required and optional binaries for the current build target and logging state;
        //   these get enabled.
        // * All binaries; any that weren't enabled in the previous step get disabled.
        public virtual IEnumerable<string> GetBinaryAssetPaths(BuildTarget buildTarget, BinaryType binaryType)
        {
            return GetBinaryPaths(buildTarget, binaryType, "Assets");
        }

        protected virtual string GetBinaryAssetBasePath()
        {
            return "Plugins/FMOD/lib";
        }

        protected abstract IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants, string suffix);

        protected virtual IEnumerable<string> GetRelativeOptionalBinaryPaths(BuildTarget buildTarget, bool allVariants)
        {
            yield break;
        }

        public virtual bool IsFMODStaticallyLinked { get { return false; } }

        public virtual bool SupportsAdditionalCPP(BuildTarget target)
        {
            return true;
        }
#endif

        // The base path for FMOD plugins when in a standalone player.
        protected virtual string GetPluginBasePath()
        {
            return string.Format("{0}/Plugins", Application.dataPath);
        }

        // The base path for FMOD plugins when in the Unity editor.
        protected virtual string GetEditorPluginBasePath()
        {
            return string.Format("{0}/FMOD/lib", GetPluginBasePath());
        }

        // Returns the full path for an FMOD plugin.
        public virtual string GetPluginPath(string pluginName)
        {
            throw new NotImplementedException(string.Format("Plugins are not implemented on platform {0}", Identifier));
        }

        // Loads static and dynamic FMOD plugins for this platform.
        public virtual void LoadPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            LoadDynamicPlugins(coreSystem, reportResult);
            LoadStaticPlugins(coreSystem, reportResult);
        }

        // Loads dynamic FMOD plugins for this platform.
        public virtual void LoadDynamicPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            List<string> pluginNames = Plugins;

            if (pluginNames == null)
            {
                return;
            }

            foreach (string pluginName in pluginNames)
            {
                if (string.IsNullOrEmpty(pluginName))
                {
                    continue;
                }

                string pluginPath = GetPluginPath(pluginName);
                uint handle;

                FMOD.RESULT result = coreSystem.loadPlugin(pluginPath, out handle);

#if UNITY_64 || UNITY_EDITOR_64
                // Add a "64" suffix and try again
                if (result == FMOD.RESULT.ERR_FILE_BAD || result == FMOD.RESULT.ERR_FILE_NOTFOUND)
                {
                    string pluginPath64 = GetPluginPath(pluginName + "64");
                    result = coreSystem.loadPlugin(pluginPath64, out handle);
                }
#endif

                reportResult(result, string.Format("Loading plugin '{0}' from '{1}'", pluginName, pluginPath));
            }
        }

        // Loads static FMOD plugins for this platform.
        public virtual void LoadStaticPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            if (StaticPlugins.Count > 0)
            {
#if !UNITY_EDITOR && ENABLE_IL2CPP
                StaticPluginManager.Register(coreSystem, reportResult);
#else
                Debug.LogWarningFormat(
                    "FMOD: {0} static plugins specified, but static plugins are only supported on the IL2CPP scripting backend",
                    StaticPlugins.Count);
#endif
            }
        }

        // These need to match the function called by LoadStaticPlugins above
        public const string RegisterStaticPluginsClassName = "StaticPluginManager";
        public const string RegisterStaticPluginsFunctionName = "Register";

        // Ensures that this platform has properties.
        public void AffirmProperties()
        {
            if (!active)
            {
                Properties = new PropertyStorage();
                InitializeProperties();
                active = true;
            }
        }

        // Clears this platform's properties.
        public void ClearProperties()
        {
            if (active)
            {
                Properties = new PropertyStorage();
                active = false;
#if UNITY_EDITOR
                DisplaySortOrder = 0;
#endif
            }
        }

        // Initializes this platform's properties to their default values.
        public virtual void InitializeProperties()
        {
            if (!IsIntrinsic)
            {
                ParentIdentifier = PlatformDefault.ConstIdentifier;
            }
        }

        // Ensures that this platform's properties are valid after loading from file.
        public virtual void EnsurePropertiesAreValid()
        {
            if (!IsIntrinsic && string.IsNullOrEmpty(ParentIdentifier))
            {
                ParentIdentifier = PlatformDefault.ConstIdentifier;
            }
        }

        [SerializeField]
        private string parentIdentifier;

        public string ParentIdentifier
        {
            get
            {
                return parentIdentifier;
            }

            set
            {
                parentIdentifier = value;
            }
        }

#if UNITY_EDITOR
        [SerializeField]
        private float displaySortOrder;

        public float DisplaySortOrder
        {
            get
            {
                return displaySortOrder;
            }

            set
            {
                displaySortOrder = value;
            }
        }
#endif

        public bool IsLiveUpdateEnabled
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return LiveUpdate != TriStateBool.Disabled;
#else
                return LiveUpdate == TriStateBool.Enabled;
#endif
            }
        }

        public bool IsOverlayEnabled
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return Overlay != TriStateBool.Disabled;
#else
                return Overlay == TriStateBool.Enabled;
#endif
            }
        }

        // A property value that can be inherited from the parent or overridden.
        public class Property<T>
        {
            public T Value;
            public bool HasValue;
        }

        // These stub classes are needed because Unity can't serialize generic classes
        [Serializable]
        public class PropertyBool : Property<TriStateBool>
        {
        }

        [Serializable]
        public class PropertyInt : Property<int>
        {
        }

        [Serializable]
        public class PropertySpeakerMode : Property<FMOD.SPEAKERMODE>
        {
        }

        [Serializable]
        public class PropertyString : Property<string>
        {
        }

        [Serializable]
        public class PropertyStringList : Property<List<string>>
        {
        }

        [Serializable]
        public class PropertyCallbackHandler : Property<PlatformCallbackHandler>
        {
        }

        public interface PropertyOverrideControl
        {
            bool HasValue(Platform platform);
            void Clear(Platform platform);
        }

        // This class provides access to a specific property on any Platform object; the property to
        // operate on is determined by the Getter function. This allows client code to operate on
        // platform properties in a generic manner.
        public struct PropertyAccessor<T> : PropertyOverrideControl
        {
            private readonly Func<PropertyStorage, Property<T>> Getter;
            private readonly T DefaultValue;

            public PropertyAccessor(Func<PropertyStorage, Property<T>> getter, T defaultValue)
            {
                Getter = getter;
                DefaultValue = defaultValue;
            }

            // Determine whether the property has a value in the given platform, or is inherited
            // from the parent.
            public bool HasValue(Platform platform)
            {
                return platform.Active && Getter(platform.Properties).HasValue;
            }

            // Get the (possibly inherited) value of the property for the given platform.
            public T Get(Platform platform)
            {
                for (Platform current = platform; current != null; current = current.Parent)
                {
                    if (current.Active)
                    {
                        Property<T> property = Getter(current.Properties);

                        if (property.HasValue)
                        {
                            return property.Value;
                        }
                    }
                }

#if UNITY_EDITOR
                if (platform is PlatformPlayInEditor)
                {
                    return Get(Settings.Instance.CurrentEditorPlatform);
                }
#endif

                return DefaultValue;
            }

            // Set the value of the property in the given platform, so it is not inherited from the
            // platform's parent.
            public void Set(Platform platform, T value)
            {
                Property<T> property = Getter(platform.Properties);

                property.Value = value;
                property.HasValue = true;
            }

            // Clear the value of the property in the given platform, so it is inherited from the
            // platform's parent.
            public void Clear(Platform platform)
            {
                Getter(platform.Properties).HasValue = false;
            }
        }

        // This class stores all of the inheritable properties for a platform.
        [Serializable]
        public class PropertyStorage
        {
            public PropertyBool LiveUpdate = new PropertyBool();
            public PropertyInt LiveUpdatePort = new PropertyInt();
            public PropertyBool Overlay = new PropertyBool();
            public PropertyBool Logging = new PropertyBool();
            public PropertyInt SampleRate = new PropertyInt();
            public PropertyString BuildDirectory = new PropertyString();
            public PropertySpeakerMode SpeakerMode = new PropertySpeakerMode();
            public PropertyInt VirtualChannelCount = new PropertyInt();
            public PropertyInt RealChannelCount = new PropertyInt();
            public PropertyInt DSPBufferLength = new PropertyInt();
            public PropertyInt DSPBufferCount = new PropertyInt();
            public PropertyStringList Plugins = new PropertyStringList();
            public PropertyStringList StaticPlugins = new PropertyStringList();
            public PropertyCallbackHandler CallbackHandler = new PropertyCallbackHandler();
        }

        [SerializeField]
        private bool active = false;

        // Whether this platform is active in the settings UI.
        public bool Active { get { return active; } }

        // Whether this platform has any properties that are not inherited from the parent.
        public bool HasAnyOverriddenProperties
        {
            get
            {
                return active &&
                    (
                        Properties.LiveUpdate.HasValue
                        || Properties.LiveUpdatePort.HasValue
                        || Properties.Overlay.HasValue
                        || Properties.Logging.HasValue
                        || Properties.SampleRate.HasValue
                        || Properties.BuildDirectory.HasValue
                        || Properties.SpeakerMode.HasValue
                        || Properties.VirtualChannelCount.HasValue
                        || Properties.RealChannelCount.HasValue
                        || Properties.DSPBufferLength.HasValue
                        || Properties.DSPBufferCount.HasValue
                        || Properties.Plugins.HasValue
                        || Properties.StaticPlugins.HasValue
                    );
            }
        }

        [SerializeField]
        protected PropertyStorage Properties = new PropertyStorage();

        // These accessors provide (possibly inherited) property values.
        public TriStateBool LiveUpdate { get { return PropertyAccessors.LiveUpdate.Get(this); } }
        public int LiveUpdatePort { get { return PropertyAccessors.LiveUpdatePort.Get(this); } }
        public TriStateBool Overlay { get { return PropertyAccessors.Overlay.Get(this); } }
        public TriStateBool Logging { get { return PropertyAccessors.Logging.Get(this); } }
        public int SampleRate { get { return PropertyAccessors.SampleRate.Get(this); } }
        public string BuildDirectory { get { return PropertyAccessors.BuildDirectory.Get(this); } }
        public FMOD.SPEAKERMODE SpeakerMode { get { return PropertyAccessors.SpeakerMode.Get(this); } }
        public int VirtualChannelCount { get { return PropertyAccessors.VirtualChannelCount.Get(this); } }
        public int RealChannelCount { get { return PropertyAccessors.RealChannelCount.Get(this); } }
        public int DSPBufferLength { get { return PropertyAccessors.DSPBufferLength.Get(this); } }
        public int DSPBufferCount { get { return PropertyAccessors.DSPBufferCount.Get(this); } }
        public List<string> Plugins { get { return PropertyAccessors.Plugins.Get(this); } }
        public List<string> StaticPlugins { get { return PropertyAccessors.StaticPlugins.Get(this); } }
        public PlatformCallbackHandler CallbackHandler { get { return PropertyAccessors.CallbackHandler.Get(this); } }

        // These accessors provide full access to properties.
        public static class PropertyAccessors
        {
            public static readonly PropertyAccessor<TriStateBool> LiveUpdate
                    = new PropertyAccessor<TriStateBool>(properties => properties.LiveUpdate, TriStateBool.Disabled);

            public static readonly PropertyAccessor<int> LiveUpdatePort
                    = new PropertyAccessor<int>(properties => properties.LiveUpdatePort, 9264);

            public static readonly PropertyAccessor<TriStateBool> Overlay
                    = new PropertyAccessor<TriStateBool>(properties => properties.Overlay, TriStateBool.Disabled);

            public static readonly PropertyAccessor<TriStateBool> Logging
                    = new PropertyAccessor<TriStateBool>(properties => properties.Logging, TriStateBool.Disabled);

            public static readonly PropertyAccessor<int> SampleRate
                    = new PropertyAccessor<int>(properties => properties.SampleRate, 0);

            public static readonly PropertyAccessor<string> BuildDirectory
                    = new PropertyAccessor<string>(properties => properties.BuildDirectory, "Desktop");

            public static readonly PropertyAccessor<FMOD.SPEAKERMODE> SpeakerMode
                    = new PropertyAccessor<FMOD.SPEAKERMODE>(properties => properties.SpeakerMode, FMOD.SPEAKERMODE.STEREO);

            public static readonly PropertyAccessor<int> VirtualChannelCount
                    = new PropertyAccessor<int>(properties => properties.VirtualChannelCount, 128);

            public static readonly PropertyAccessor<int> RealChannelCount
                    = new PropertyAccessor<int>(properties => properties.RealChannelCount, 32);

            public static readonly PropertyAccessor<int> DSPBufferLength
                    = new PropertyAccessor<int>(properties => properties.DSPBufferLength, 0);

            public static readonly PropertyAccessor<int> DSPBufferCount
                    = new PropertyAccessor<int>(properties => properties.DSPBufferCount, 0);

            public static readonly PropertyAccessor<List<string>> Plugins
                    = new PropertyAccessor<List<string>>(properties => properties.Plugins, null);

            public static readonly PropertyAccessor<List<string>> StaticPlugins
                    = new PropertyAccessor<List<string>>(properties => properties.StaticPlugins, null);

            public static readonly PropertyAccessor<PlatformCallbackHandler> CallbackHandler
                    = new PropertyAccessor<PlatformCallbackHandler>(properties => properties.CallbackHandler, null);
        }

#if UNITY_EDITOR
        // The parent platform from which this platform inherits its property values.
        public Platform Parent
        {
            get
            {
                return (ParentIdentifier != null) ? Settings.Instance.FindPlatform(ParentIdentifier) : null;
            }
        }

        [SerializeField]
        private List<string> childIdentifiers = new List<string>();

        // The platforms which inherit their property values from this platform.
        public List<string> ChildIdentifiers { get { return childIdentifiers; } }
#else
        // The parent platform from which this platform inherits its property values.
        [NonSerialized]
        public Platform Parent;
#endif

        // Checks whether this platform inherits from the given platform, so we can avoid creating
        // inheritance loops.
        public bool InheritsFrom(Platform platform)
        {
            if (platform == this)
            {
                return true;
            }
            else if (Parent != null)
            {
                return Parent.InheritsFrom(platform);
            }
            else
            {
                return false;
            }
        }

        [SerializeField]
        public string outputType;

        public FMOD.OUTPUTTYPE GetOutputType()
        {
            if (Enum.IsDefined(typeof(FMOD.OUTPUTTYPE), outputType))
            {
                return (FMOD.OUTPUTTYPE)Enum.Parse(typeof(FMOD.OUTPUTTYPE), outputType);
            }
            return FMOD.OUTPUTTYPE.AUTODETECT;
        }
#if UNITY_EDITOR
        public struct OutputType
        {
            public string displayName;
            public FMOD.OUTPUTTYPE outputType;
        }

        public abstract OutputType[] ValidOutputTypes { get; }

        public virtual int CoreCount { get { return 0; } }

        public const int MaximumCoreCount = 16;
#endif

        public virtual List<ThreadAffinityGroup> DefaultThreadAffinities { get { return StaticThreadAffinities; } }

        private static List<ThreadAffinityGroup> StaticThreadAffinities = new List<ThreadAffinityGroup>();

        [Serializable]
        public class PropertyThreadAffinityList : Property<List<ThreadAffinityGroup>>
        {
        }

        [SerializeField]
        private PropertyThreadAffinityList threadAffinities = new PropertyThreadAffinityList();

        public IEnumerable<ThreadAffinityGroup> ThreadAffinities
        {
            get
            {
                if (threadAffinities.HasValue)
                {
                    return threadAffinities.Value;
                }
                else
                {
                    return DefaultThreadAffinities;
                }
            }
        }

        public PropertyThreadAffinityList ThreadAffinitiesProperty { get { return threadAffinities; } }
    }
}
