using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PlatformMobileLow : Platform
    {
        static PlatformMobileLow()
        {
            Settings.AddPlatformTemplate<PlatformMobileLow>("c88d16e5272a4e241b0ef0ac2e53b73d");
        }

        public override string DisplayName => "Low-End Mobile";
        public override void DeclareUnityMappings(Settings settings)
        {
            settings.DeclareRuntimePlatform(RuntimePlatform.IPhonePlayer, this);
            settings.DeclareRuntimePlatform(RuntimePlatform.Android, this);
        }
#if UNITY_EDITOR
        public override Legacy.Platform LegacyIdentifier => Legacy.Platform.MobileLow;

        protected override IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants,
            string suffix)
        {
            yield break;
        }

        public override bool SupportsAdditionalCPP(BuildTarget target)
        {
            if (target == BuildTarget.iOS)
                return PlatformIOS.StaticSupportsAdditionalCpp();
            return base.SupportsAdditionalCPP(target);
        }
#endif

        public override float Priority => DefaultPriority + 1;

        public override bool MatchesCurrentEnvironment => Active;

#if UNITY_IOS
        public override void LoadPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            PlatformIOS.StaticLoadPlugins(this, coreSystem, reportResult);
        }
#elif UNITY_ANDROID
        public override string GetBankFolder()
        {
            return PlatformAndroid.StaticGetBankFolder();
        }

        public override string GetPluginPath(string pluginName)
        {
            return PlatformAndroid.StaticGetPluginPath(pluginName);
        }
#endif

#if UNITY_EDITOR
        public override OutputType[] ValidOutputTypes => null;
#endif
    }
}