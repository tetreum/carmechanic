using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
    public class PlatformDefault : Platform
    {
        public const string ConstIdentifier = "default";

        public PlatformDefault()
        {
            Identifier = ConstIdentifier;
        }

        public override string DisplayName => "Default";

        public override bool IsIntrinsic => true;

        // null means no valid output types - don't display the field in the UI
#if UNITY_EDITOR
        public override OutputType[] ValidOutputTypes => null;
#endif
        public override void DeclareUnityMappings(Settings settings)
        {
        }

        public override void InitializeProperties()
        {
            base.InitializeProperties();

            PropertyAccessors.Plugins.Set(this, new List<string>());
            PropertyAccessors.StaticPlugins.Set(this, new List<string>());
        }

        public override void EnsurePropertiesAreValid()
        {
            base.EnsurePropertiesAreValid();

            if (StaticPlugins == null) PropertyAccessors.StaticPlugins.Set(this, new List<string>());
        }
#if UNITY_EDITOR
        public override Legacy.Platform LegacyIdentifier => Legacy.Platform.Default;

        protected override IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants,
            string suffix)
        {
            yield break;
        }
#endif
    }
}