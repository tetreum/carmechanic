﻿using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_TVOS && !UNITY_EDITOR
namespace FMOD
{
    public partial class VERSION
    {
        public const string dll = "__Internal";
    }
}

namespace FMOD.Studio
{
    public partial class STUDIO_VERSION
    {
        public const string dll = "__Internal";
    }
}
#endif

namespace FMODUnity
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PlatformAppleTV : Platform
    {
        static PlatformAppleTV()
        {
            Settings.AddPlatformTemplate<PlatformAppleTV>("e7a046c753c3c3d4aacc91f6597f310d");
        }

        public override string DisplayName { get { return "Apple TV"; } }
        public override void DeclareUnityMappings(Settings settings)
        {
            settings.DeclareRuntimePlatform(RuntimePlatform.tvOS, this);

#if UNITY_EDITOR
            settings.DeclareBuildTarget(BuildTarget.tvOS, this);
#endif
        }

#if UNITY_EDITOR
        public override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.AppleTV; } }

        protected override IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            if (allVariants || PlayerSettings.tvOS.sdkVersion == tvOSSdkVersion.Device)
            {
                yield return string.Format("tvos/libfmodstudiounityplugin{0}.a", suffix);
            }

            if (allVariants || PlayerSettings.tvOS.sdkVersion == tvOSSdkVersion.Simulator)
            {
                yield return string.Format("tvos/libfmodstudiounitypluginsimulator{0}.a", suffix);
            }
        }

        public override bool SupportsAdditionalCPP(BuildTarget target)
        {
            return PlatformIOS.StaticSupportsAdditionalCpp();
        }
#endif

#if !UNITY_EDITOR
        public override void LoadPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            PlatformIOS.StaticLoadPlugins(this, coreSystem, reportResult);
        }
#endif

#if UNITY_EDITOR
        public override OutputType[] ValidOutputTypes
        {
            get
            {
                return sValidOutputTypes;
            }
        }

        private static OutputType[] sValidOutputTypes = {
           new OutputType() { displayName = "Core Audio", outputType = FMOD.OUTPUTTYPE.COREAUDIO },
        };
#endif
    }
}
