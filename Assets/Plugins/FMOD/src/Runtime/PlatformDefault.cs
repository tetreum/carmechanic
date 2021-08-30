﻿using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
    public class PlatformDefault : Platform
    {
        public PlatformDefault()
        {
            Identifier = ConstIdentifier;
        }

        public const string ConstIdentifier = "default";

        public override string DisplayName { get { return "Default"; } }
        public override void DeclareUnityMappings(Settings settings) { }
#if UNITY_EDITOR
        public override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.Default; } }

        protected override IEnumerable<string> GetRelativeBinaryPaths(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            yield break;
        }
#endif

        public override bool IsIntrinsic { get { return true; } }

        public override void InitializeProperties()
        {
            base.InitializeProperties();

            PropertyAccessors.Plugins.Set(this, new List<string>());
            PropertyAccessors.StaticPlugins.Set(this, new List<string>());
        }

        public override void EnsurePropertiesAreValid()
        {
            base.EnsurePropertiesAreValid();

            if (StaticPlugins == null)
            {
                PropertyAccessors.StaticPlugins.Set(this, new List<string>());
            }
        }

        // null means no valid output types - don't display the field in the UI
#if UNITY_EDITOR
        public override OutputType[] ValidOutputTypes { get { return null; } }
#endif
    }
}
