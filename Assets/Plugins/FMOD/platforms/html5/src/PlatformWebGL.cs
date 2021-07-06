﻿using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
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
    public class PlatformWebGL : Platform
    {
        static PlatformWebGL()
        {
            Settings.AddPlatformTemplate<PlatformWebGL>("46fbfdf3fc43db0458918377fd40293e");
        }

        public override string DisplayName { get { return "WebGL"; } }
        public override void DeclareUnityMappings(Settings settings)
        {
            settings.DeclareRuntimePlatform(RuntimePlatform.WebGLPlayer, this);

#if UNITY_EDITOR
            settings.DeclareBuildTarget(BuildTarget.WebGL, this);
#endif
        }

#if UNITY_EDITOR
        public override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.WebGL; } }

        protected override IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            yield return string.Format("html5/libfmodstudiounityplugin{0}.bc", suffix);
        }

        public override bool IsFMODStaticallyLinked { get { return true; } }
#endif

        public override string GetPluginPath(string pluginName)
        {
            return string.Format("{0}/{1}.bc", GetPluginBasePath(), pluginName);
        }
#if UNITY_EDITOR
        public override OutputType[] ValidOutputTypes
        {
            get
            {
                return sValidOutputTypes;
            }
        }

        private static OutputType[] sValidOutputTypes = {
           new OutputType() { displayName = "JavaScript webaudio output", outputType = FMOD.OUTPUTTYPE.WEBAUDIO },
        };
#endif
    }
}
