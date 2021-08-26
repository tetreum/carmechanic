using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
    public class PlatformGroup : Platform
    {
        [SerializeField] public string displayName;

        [SerializeField] private Legacy.Platform legacyIdentifier;

        public override string DisplayName => displayName;
        public override void DeclareUnityMappings(Settings settings)
        {
        }
#if UNITY_EDITOR
        public override Legacy.Platform LegacyIdentifier => legacyIdentifier;

        public static PlatformGroup Create(string displayName, Legacy.Platform legacyIdentifier)
        {
            var group = CreateInstance<PlatformGroup>();
            group.Identifier = GUID.Generate().ToString();
            group.displayName = displayName;
            group.legacyIdentifier = legacyIdentifier;
            group.AffirmProperties();

            return group;
        }

        protected override IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants,
            string suffix)
        {
            yield break;
        }

        public override OutputType[] ValidOutputTypes => null;
#endif
    }
}