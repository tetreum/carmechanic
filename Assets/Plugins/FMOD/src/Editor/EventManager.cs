using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FMOD;
using FMOD.Studio;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace FMODUnity
{
    [InitializeOnLoad]
    public class EventManager : MonoBehaviour
    {
        private const string CacheAssetName = "FMODStudioCache";
        public const string CacheAssetFullName = "Assets/Plugins/FMOD/Cache/Editor/" + CacheAssetName + ".asset";
        private static EventCache eventCache;

        private const string StringBankExtension = "strings.bank";
        private const string BankExtension = "bank";

#if UNITY_EDITOR
        [MenuItem("FMOD/Refresh Banks", priority = 1)]
        public static void RefreshBanks()
        {
            var result = UpdateCache();
            OnCacheChange();
            if (Settings.Instance.ImportType == ImportType.AssetBundle)
                UpdateBankStubAssets(EditorUserBuildSettings.activeBuildTarget);

            BankRefresher.HandleBankRefresh(result);
        }
#endif

        private static void ClearCache()
        {
            eventCache.StringsBankWriteTime = DateTime.MinValue;
            eventCache.EditorBanks.Clear();
            eventCache.EditorEvents.Clear();
            eventCache.EditorParameters.Clear();
            eventCache.StringsBanks.Clear();
            eventCache.MasterBanks.Clear();
            if (Settings.Instance && Settings.Instance.BanksToLoad != null)
                Settings.Instance.BanksToLoad.Clear();
        }

        private static void AffirmEventCache()
        {
            if (eventCache == null) UpdateCache();
        }

        private static string UpdateCache()
        {
            if (eventCache == null)
            {
                eventCache = AssetDatabase.LoadAssetAtPath(CacheAssetFullName, typeof(EventCache)) as EventCache;
                if (eventCache == null || eventCache.cacheVersion != EventCache.CurrentCacheVersion)
                {
                    Debug.Log("FMOD: Event cache is missing or in an old format; creating a new instance.");

                    eventCache = ScriptableObject.CreateInstance<EventCache>();
                    eventCache.cacheVersion = EventCache.CurrentCacheVersion;

                    Directory.CreateDirectory(Path.GetDirectoryName(CacheAssetFullName));
                    AssetDatabase.CreateAsset(eventCache, CacheAssetFullName);
                }
            }

            var settings = Settings.Instance;

            if (string.IsNullOrEmpty(settings.SourceBankPath))
            {
                ClearCache();
                return null;
            }

            string defaultBankFolder = null;

            if (!settings.HasPlatforms)
            {
                defaultBankFolder = settings.SourceBankPath;
            }
            else
            {
                var platform = settings.CurrentEditorPlatform;

                if (platform == settings.DefaultPlatform) platform = settings.PlayInEditorPlatform;

                defaultBankFolder =
                    RuntimeUtils.GetCommonPlatformPath(Path.Combine(settings.SourceBankPath, platform.BuildDirectory));
            }

            var bankPlatforms = EditorUtils.GetBankPlatforms();
            var bankFolders = new string[bankPlatforms.Length];
            for (var i = 0; i < bankPlatforms.Length; i++)
                bankFolders[i] =
                    RuntimeUtils.GetCommonPlatformPath(Path.Combine(settings.SourceBankPath, bankPlatforms[i]));

            var stringBanks = new List<string>(0);
            try
            {
                var files = Directory.GetFiles(defaultBankFolder, "*." + StringBankExtension,
                    SearchOption.AllDirectories);
                stringBanks = new List<string>(files);
            }
            catch
            {
            }

            // Strip out OSX resource-fork files that appear on FAT32
            stringBanks.RemoveAll(x => Path.GetFileName(x).StartsWith("._"));

            if (stringBanks.Count == 0)
            {
                ClearCache();
                return string.Format(
                    "Directory {0} doesn't contain any banks.\nBuild the banks in Studio or check the path in the settings.",
                    defaultBankFolder);
            }

            // If we have multiple .strings.bank files find the most recent
            stringBanks.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

            // Use the most recent string bank timestamp as a marker for the most recent build of any bank because it gets exported every time
            var lastWriteTime = File.GetLastWriteTime(stringBanks[0]);

            // Get a list of all banks
            var bankFileNames = new List<string>();
            var reducedStringBanksList = new List<string>();
            var stringBankGuids = new HashSet<Guid>();

            foreach (var stringBankPath in stringBanks)
            {
                Bank stringBank;
                EditorUtils.CheckResult(EditorUtils.System.loadBankFile(stringBankPath, LOAD_BANK_FLAGS.NORMAL,
                    out stringBank));

                if (!stringBank.isValid())
                    return string.Format("{0} is not a valid bank.", stringBankPath);
                stringBank.unload();

                Guid stringBankGuid;
                EditorUtils.CheckResult(stringBank.getID(out stringBankGuid));

                if (!stringBankGuids.Add(stringBankGuid))
                    // If we encounter multiple string banks with the same GUID then only use the first. This handles the scenario where
                    // a Studio project is cloned and extended for DLC with a new master bank name.
                    continue;

                reducedStringBanksList.Add(stringBankPath);
            }

            bankFileNames =
                new List<string>(Directory.GetFiles(defaultBankFolder, "*.bank", SearchOption.AllDirectories));
            bankFileNames.RemoveAll(x => x.Contains(".strings"));

            stringBanks = reducedStringBanksList;

            eventCache.StringsBankWriteTime = lastWriteTime;

            // Stop editor preview so no stale data being held
            EditorUtils.PreviewStop();

            // Reload the strings banks
            var loadedStringsBanks = new List<Bank>();

            try
            {
                AssetDatabase.StartAssetEditing();

                eventCache.EditorBanks.ForEach(x => x.Exists = false);
                var masterBankFileNames = new HashSet<string>();

                foreach (var stringBankPath in stringBanks)
                {
                    Bank stringBank;
                    EditorUtils.CheckResult(EditorUtils.System.loadBankFile(stringBankPath, LOAD_BANK_FLAGS.NORMAL,
                        out stringBank));

                    if (!stringBank.isValid())
                    {
                        ClearCache();
                        return string.Format("{0} is not a valid bank.", stringBankPath);
                    }

                    loadedStringsBanks.Add(stringBank);

                    var stringBankFileInfo = new FileInfo(stringBankPath);

                    var masterBankFileName =
                        Path.GetFileName(stringBankPath).Replace(StringBankExtension, BankExtension);
                    masterBankFileNames.Add(masterBankFileName);

                    var stringsBankRef = eventCache.StringsBanks.Find(x =>
                        RuntimeUtils.GetCommonPlatformPath(stringBankPath) == x.Path);

                    if (stringsBankRef == null)
                    {
                        stringsBankRef = ScriptableObject.CreateInstance<EditorBankRef>();
                        stringsBankRef.FileSizes = new List<EditorBankRef.NameValuePair>();
                        AssetDatabase.AddObjectToAsset(stringsBankRef, eventCache);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(stringsBankRef));
                        eventCache.EditorBanks.Add(stringsBankRef);
                        eventCache.StringsBanks.Add(stringsBankRef);
                    }

                    stringsBankRef.SetPath(stringBankPath, defaultBankFolder);
                    string studioPath;
                    stringBank.getPath(out studioPath);
                    stringsBankRef.SetStudioPath(studioPath);
                    stringsBankRef.LastModified = stringBankFileInfo.LastWriteTime;
                    stringsBankRef.Exists = true;
                    stringsBankRef.FileSizes.Clear();

                    if (Settings.Instance.HasPlatforms)
                        for (var i = 0; i < bankPlatforms.Length; i++)
                            stringsBankRef.FileSizes.Add(
                                new EditorBankRef.NameValuePair(bankPlatforms[i], stringBankFileInfo.Length));
                    else
                        stringsBankRef.FileSizes.Add(new EditorBankRef.NameValuePair("", stringBankFileInfo.Length));
                }

                eventCache.EditorParameters.ForEach(x => x.Exists = false);
                foreach (var bankFileName in bankFileNames)
                {
                    var bankRef =
                        eventCache.EditorBanks.Find(x => RuntimeUtils.GetCommonPlatformPath(bankFileName) == x.Path);

                    // New bank we've never seen before
                    if (bankRef == null)
                    {
                        bankRef = ScriptableObject.CreateInstance<EditorBankRef>();
                        AssetDatabase.AddObjectToAsset(bankRef, eventCache);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(bankRef));

                        bankRef.SetPath(bankFileName, defaultBankFolder);
                        bankRef.LastModified = DateTime.MinValue;
                        bankRef.FileSizes = new List<EditorBankRef.NameValuePair>();

                        eventCache.EditorBanks.Add(bankRef);
                    }

                    bankRef.Exists = true;

                    var bankFileInfo = new FileInfo(bankFileName);

                    // Update events from this bank if it has been modified,
                    // or it is a master bank (so that we get any global parameters)
                    if (bankRef.LastModified != bankFileInfo.LastWriteTime
                        || masterBankFileNames.Contains(Path.GetFileName(bankFileName)))
                    {
                        bankRef.LastModified = bankFileInfo.LastWriteTime;
                        UpdateCacheBank(bankRef);
                    }

                    // Update file sizes
                    bankRef.FileSizes.Clear();
                    if (Settings.Instance.HasPlatforms)
                    {
                        for (var i = 0; i < bankPlatforms.Length; i++)
                        {
                            var platformBankPath =
                                RuntimeUtils.GetCommonPlatformPath(bankFolders[i] +
                                                                   bankFileName.Replace(defaultBankFolder, ""));
                            var fileInfo = new FileInfo(platformBankPath);
                            if (fileInfo.Exists)
                                bankRef.FileSizes.Add(
                                    new EditorBankRef.NameValuePair(bankPlatforms[i], fileInfo.Length));
                        }
                    }
                    else
                    {
                        var platformBankPath =
                            RuntimeUtils.GetCommonPlatformPath(Path.Combine(Settings.Instance.SourceBankPath,
                                bankFileName));
                        var fileInfo = new FileInfo(platformBankPath);
                        if (fileInfo.Exists)
                            bankRef.FileSizes.Add(new EditorBankRef.NameValuePair("", fileInfo.Length));
                    }

                    if (masterBankFileNames.Contains(bankFileInfo.Name))
                        if (!eventCache.MasterBanks.Exists(x =>
                            RuntimeUtils.GetCommonPlatformPath(bankFileName) == x.Path))
                            eventCache.MasterBanks.Add(bankRef);
                }

                // Remove any stale entries from bank, event and parameter lists
                eventCache.EditorBanks.FindAll(x => !x.Exists).ForEach(RemoveCacheBank);
                eventCache.EditorBanks.RemoveAll(x => !x.Exists);
                eventCache.EditorEvents.RemoveAll(x => x.Banks.Count == 0);
                eventCache.EditorParameters.RemoveAll(x => !x.Exists);
                eventCache.MasterBanks.RemoveAll(x => !x.Exists);
                eventCache.StringsBanks.RemoveAll(x => !x.Exists);
            }
            finally
            {
                // Unload the strings banks
                loadedStringsBanks.ForEach(x => x.unload());
                AssetDatabase.StopAssetEditing();
                Debug.Log("FMOD: Cache updated.");
            }

            return null;
        }

        private static void UpdateCacheBank(EditorBankRef bankRef)
        {
            // Clear out any cached events from this bank
            eventCache.EditorEvents.ForEach(x => x.Banks.Remove(bankRef));

            Bank bank;
            var loadResult =
                EditorUtils.System.loadBankFile(bankRef.Path, LOAD_BANK_FLAGS.NORMAL, out bank);

            if (loadResult == RESULT.ERR_EVENT_ALREADY_LOADED)
            {
                EditorUtils.System.getBank(bankRef.Name, out bank);
                bank.unload();
                loadResult = EditorUtils.System.loadBankFile(bankRef.Path, LOAD_BANK_FLAGS.NORMAL, out bank);
            }

            if (loadResult == RESULT.OK)
            {
                // Get studio path
                string studioPath;
                bank.getPath(out studioPath);
                bankRef.SetStudioPath(studioPath);

                // Iterate all events in the bank and cache them
                EventDescription[] eventList;
                var result = bank.getEventList(out eventList);
                if (result == RESULT.OK)
                    foreach (var eventDesc in eventList)
                    {
                        string path;
                        result = eventDesc.getPath(out path);
                        var eventRef = eventCache.EditorEvents.Find(x => x.Path == path);
                        if (eventRef == null)
                        {
                            eventRef = ScriptableObject.CreateInstance<EditorEventRef>();
                            AssetDatabase.AddObjectToAsset(eventRef, eventCache);
                            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(eventRef));
                            eventRef.Banks = new List<EditorBankRef>();
                            eventCache.EditorEvents.Add(eventRef);
                            eventRef.Parameters = new List<EditorParamRef>();
                        }

                        eventRef.Banks.Add(bankRef);
                        Guid guid;
                        eventDesc.getID(out guid);
                        eventRef.Guid = guid;
                        eventRef.Path = eventRef.name = path;
                        eventDesc.is3D(out eventRef.Is3D);
                        eventDesc.isOneshot(out eventRef.IsOneShot);
                        eventDesc.isStream(out eventRef.IsStream);
                        eventDesc.getMaximumDistance(out eventRef.MaxDistance);
                        eventDesc.getMinimumDistance(out eventRef.MinDistance);
                        eventDesc.getLength(out eventRef.Length);
                        var paramCount = 0;
                        eventDesc.getParameterDescriptionCount(out paramCount);
                        eventRef.Parameters.ForEach(x => x.Exists = false);
                        for (var paramIndex = 0; paramIndex < paramCount; paramIndex++)
                        {
                            PARAMETER_DESCRIPTION param;
                            eventDesc.getParameterDescriptionByIndex(paramIndex, out param);
                            // Skip if readonly and not global
                            if ((param.flags & PARAMETER_FLAGS.READONLY) != 0 &&
                                (param.flags & PARAMETER_FLAGS.GLOBAL) == 0) continue;
                            var paramRef = eventRef.Parameters.Find(x => x.name == param.name);
                            if (paramRef == null)
                            {
                                paramRef = ScriptableObject.CreateInstance<EditorParamRef>();
                                AssetDatabase.AddObjectToAsset(paramRef, eventCache);
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(paramRef));
                                eventRef.Parameters.Add(paramRef);
                            }

                            InitializeParamRef(paramRef, param);

                            paramRef.name = "parameter:/" + Path.GetFileName(path) + "/" + paramRef.Name;
                            paramRef.Exists = true;
                        }

                        eventRef.Parameters.RemoveAll(x => !x.Exists);
                    }

                // Update global parameter list for each bank
                PARAMETER_DESCRIPTION[] parameterDescriptions;
                result = EditorUtils.System.getParameterDescriptionList(out parameterDescriptions);
                if (result == RESULT.OK)
                    for (var i = 0; i < parameterDescriptions.Length; i++)
                    {
                        var param = parameterDescriptions[i];
                        if ((param.flags & PARAMETER_FLAGS.GLOBAL) == PARAMETER_FLAGS.GLOBAL)
                        {
                            var paramRef = eventCache.EditorParameters.Find(x =>
                                param.id.data1 == global::x.ID.data1 && param.id.data2 == global::x.ID.data2);
                            if (paramRef == null)
                            {
                                paramRef = ScriptableObject.CreateInstance<EditorParamRef>();
                                AssetDatabase.AddObjectToAsset(paramRef, eventCache);
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(paramRef));
                                eventCache.EditorParameters.Add(paramRef);
                                paramRef.ID = param.id;
                            }

                            InitializeParamRef(paramRef, param);

                            paramRef.name = "parameter:/" + param.name;
                            paramRef.Exists = true;
                        }
                    }

                bank.unload();
            }
            else
            {
                Debug.LogError(string.Format("FMOD Studio: Unable to load {0}: {1}", bankRef.Name,
                    Error.String(loadResult)));
                eventCache.StringsBankWriteTime = DateTime.MinValue;
            }
        }

        private static void InitializeParamRef(EditorParamRef paramRef, PARAMETER_DESCRIPTION description)
        {
            paramRef.Name = description.name;
            paramRef.Min = description.minimum;
            paramRef.Max = description.maximum;
            paramRef.Default = description.defaultvalue;
            paramRef.IsGlobal = (description.flags & PARAMETER_FLAGS.GLOBAL) != 0;

            if ((description.flags & PARAMETER_FLAGS.DISCRETE) != 0)
                paramRef.Type = ParameterType.Discrete;
            else
                paramRef.Type = ParameterType.Continuous;
        }

        private static void RemoveCacheBank(EditorBankRef bankRef)
        {
            eventCache.EditorEvents.ForEach(x => x.Banks.Remove(bankRef));
        }

        static EventManager()
        {
            EditorApplication.delayCall += Startup;

            BuildStatusWatcher.OnBuildStarted += () =>
            {
                BuildTargetChanged();
                CopyToStreamingAssets(EditorUserBuildSettings.activeBuildTarget);
            };
            BuildStatusWatcher.OnBuildEnded += () =>
            {
                UpdateBankStubAssets(EditorUserBuildSettings.activeBuildTarget);
            };
        }

        private static void Startup()
        {
            // Avoid throwing exceptions so we don't stop Unity calling other delayCall functions
            try
            {
                RefreshBanks();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void CheckValidEventRefs(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                var allBehaviours = gameObject.GetComponentsInChildren<MonoBehaviour>();

                foreach (var behaviour in allBehaviours)
                    if (behaviour != null)
                    {
                        var componentType = behaviour.GetType();

                        var fields =
                            componentType.GetFields(
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        foreach (var item in fields)
                            if (HasAttribute(item, typeof(EventRefAttribute)))
                            {
                                if (item.FieldType == typeof(string))
                                {
                                    var output = item.GetValue(behaviour) as string;

                                    if (!IsValidEventRef(output))
                                        Debug.LogWarningFormat(
                                            "FMOD Studio: Unable to find FMOD Event \"{0}\" in scene \"{1}\" at path \"{2}\" \n- check the FMOD Studio event paths are set correctly in the Unity editor",
                                            output, scene.name, GetGameObjectPath(behaviour.transform));
                                }
                                else if (typeof(IEnumerable).IsAssignableFrom(item.FieldType))
                                {
                                    foreach (var listItem in (IEnumerable) item.GetValue(behaviour))
                                        if (listItem.GetType() == typeof(string))
                                        {
                                            var listOutput = listItem as string;
                                            if (!IsValidEventRef(listOutput))
                                                Debug.LogWarningFormat(
                                                    "FMOD Studio: Unable to find FMOD Event \"{0}\" in scene \"{1}\" at path \"{2}\" \n- check the FMOD Studio event paths are set correctly in the Unity editor",
                                                    listOutput, scene.name, GetGameObjectPath(behaviour.transform));
                                        }
                                }
                            }
                    }
            }
        }

        private static string GetGameObjectPath(Transform transform)
        {
            var objectPath = "/" + transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                objectPath = "/" + transform.name + objectPath;
            }

            return objectPath;
        }

        private static bool HasAttribute(MemberInfo provider, params Type[] attributeTypes)
        {
            var allAttributes = Attribute.GetCustomAttributes(provider, typeof(Attribute), true);

            if (allAttributes.Length == 0) return false;
            return allAttributes
                .Where(a => attributeTypes.Any(x => a.GetType() == x || x.IsAssignableFrom(a.GetType()))).Any();
        }

        private static bool IsValidEventRef(string reference)
        {
            if (string.IsNullOrEmpty(reference)) return true;
            var eventRef = EventFromPath(reference);
            return eventRef != null;
        }

        private const string FMODLabel = "FMOD";

        public static void CopyToStreamingAssets(BuildTarget buildTarget)
        {
            if (string.IsNullOrEmpty(Settings.Instance.SourceBankPath))
                return;

            var platform = Settings.Instance.GetPlatform(buildTarget);

            if (platform == Settings.Instance.DefaultPlatform)
            {
                Debug.LogWarningFormat("FMOD Studio: copy banks for platform {0} : Unsupported platform", buildTarget);
                return;
            }

            var bankTargetFolder =
                Settings.Instance.ImportType == ImportType.StreamingAssets
                    ? Settings.Instance.TargetPath
                    : Application.dataPath + (string.IsNullOrEmpty(Settings.Instance.TargetAssetPath)
                        ? ""
                        : '/' + Settings.Instance.TargetAssetPath);
            bankTargetFolder = RuntimeUtils.GetCommonPlatformPath(bankTargetFolder);
            Directory.CreateDirectory(bankTargetFolder);

            var bankTargetExtension =
                Settings.Instance.ImportType == ImportType.StreamingAssets
                    ? ".bank"
                    : ".bytes";

            var bankSourceFolder =
                Settings.Instance.HasPlatforms
                    ? Settings.Instance.SourceBankPath + '/' + platform.BuildDirectory
                    : Settings.Instance.SourceBankPath;
            bankSourceFolder = RuntimeUtils.GetCommonPlatformPath(bankSourceFolder);

            if (Path.GetFullPath(bankTargetFolder).TrimEnd('/').ToUpperInvariant() ==
                Path.GetFullPath(bankSourceFolder).TrimEnd('/').ToUpperInvariant())
                return;

            var madeChanges = false;

            try
            {
                // Clean out any stale .bank files
                var existingBankFiles =
                    Directory.GetFiles(bankTargetFolder, "*" + bankTargetExtension, SearchOption.AllDirectories);

                foreach (var bankFilePath in existingBankFiles)
                {
                    var bankName = EditorBankRef.CalculateName(bankFilePath, bankTargetFolder);

                    if (!eventCache.EditorBanks.Exists(x => x.Name == bankName))
                    {
                        var assetPath = bankFilePath.Replace(Application.dataPath, AssetsFolderName);

                        if (AssetHasLabel(assetPath, FMODLabel))
                        {
                            AssetDatabase.MoveAssetToTrash(assetPath);
                            madeChanges = true;
                        }
                    }
                }

                // Copy over any files that don't match timestamp or size or don't exist
                foreach (var bankRef in eventCache.EditorBanks)
                {
                    var sourcePath = bankSourceFolder + "/" + bankRef.Name + ".bank";
                    var targetPathRelative = bankRef.Name + bankTargetExtension;
                    var targetPathFull = bankTargetFolder + "/" + targetPathRelative;

                    var sourceInfo = new FileInfo(sourcePath);
                    var targetInfo = new FileInfo(targetPathFull);

                    if (!targetInfo.Exists ||
                        sourceInfo.Length != targetInfo.Length ||
                        sourceInfo.LastWriteTime != targetInfo.LastWriteTime)
                    {
                        if (targetInfo.Exists)
                            targetInfo.IsReadOnly = false;
                        else
                            EnsureFoldersExist(targetPathRelative, bankTargetFolder);

                        File.Copy(sourcePath, targetPathFull, true);
                        targetInfo = new FileInfo(targetPathFull);
                        targetInfo.IsReadOnly = false;
                        targetInfo.LastWriteTime = sourceInfo.LastWriteTime;

                        madeChanges = true;

                        var assetString = targetPathFull.Replace(Application.dataPath, "Assets");
                        AssetDatabase.ImportAsset(assetString);
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(assetString);
                        AssetDatabase.SetLabels(obj, new[] {FMODLabel});
                    }
                }

                RemoveEmptyFMODFolders(bankTargetFolder);
            }
            catch (Exception exception)
            {
                Debug.LogErrorFormat("FMOD Studio: copy banks for platform {0} : copying banks from {1} to {2}",
                    platform.DisplayName, bankSourceFolder, bankTargetFolder);
                Debug.LogException(exception);
                return;
            }

            if (madeChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.LogFormat("FMOD Studio: copy banks for platform {0} : copying banks from {1} to {2} succeeded",
                    platform.DisplayName, bankSourceFolder, bankTargetFolder);
            }
        }

        public static void UpdateBankStubAssets(BuildTarget buildTarget)
        {
            if (Settings.Instance.ImportType != ImportType.AssetBundle
                || string.IsNullOrEmpty(Settings.Instance.SourceBankPath))
                return;

            var platform = Settings.Instance.GetPlatform(buildTarget);

            if (platform == Settings.Instance.DefaultPlatform)
            {
                Debug.LogWarningFormat("FMOD: Updating bank stubs: Unsupported platform {0}", buildTarget);
                return;
            }

            var bankTargetFolder = Application.dataPath;

            if (!string.IsNullOrEmpty(Settings.Instance.TargetAssetPath))
                bankTargetFolder += "/" + Settings.Instance.TargetAssetPath;

            bankTargetFolder = RuntimeUtils.GetCommonPlatformPath(bankTargetFolder);

            var bankSourceFolder = Settings.Instance.SourceBankPath;

            if (Settings.Instance.HasPlatforms) bankSourceFolder += "/" + platform.BuildDirectory;

            bankSourceFolder = RuntimeUtils.GetCommonPlatformPath(bankSourceFolder);

            if (Path.GetFullPath(bankTargetFolder).TrimEnd('/').ToUpperInvariant() ==
                Path.GetFullPath(bankSourceFolder).TrimEnd('/').ToUpperInvariant())
                return;

            var madeChanges = false;

            Directory.CreateDirectory(bankTargetFolder);

            try
            {
                const string BankAssetExtension = ".bytes";

                // Clean out any stale stubs
                var existingBankFiles =
                    Directory.GetFiles(bankTargetFolder, "*" + BankAssetExtension, SearchOption.AllDirectories);

                foreach (var bankFilePath in existingBankFiles)
                {
                    var bankName = EditorBankRef.CalculateName(bankFilePath, bankTargetFolder);

                    if (!eventCache.EditorBanks.Exists(x => x.Name == bankName))
                    {
                        var assetPath = bankFilePath.Replace(Application.dataPath, AssetsFolderName);

                        if (AssetHasLabel(assetPath, FMODLabel))
                        {
                            AssetDatabase.MoveAssetToTrash(assetPath);
                            madeChanges = true;
                        }
                    }
                }

                // Create any stubs that don't exist, and ensure any that do exist have the correct data
                foreach (var bankRef in eventCache.EditorBanks)
                {
                    var sourcePath = bankSourceFolder + "/" + bankRef.Name + ".bank";
                    var targetPathRelative = bankRef.Name + BankAssetExtension;
                    var targetPathFull = bankTargetFolder + "/" + targetPathRelative;

                    EnsureFoldersExist(targetPathRelative, bankTargetFolder);

                    var targetInfo = new FileInfo(targetPathFull);

                    var stubData = RuntimeManager.BankStubPrefix + bankRef.Name;

                    // Minimise asset database refreshing by only writing the stub if necessary
                    bool writeStub;

                    if (targetInfo.Exists && targetInfo.Length == stubData.Length)
                        using (var reader = targetInfo.OpenText())
                        {
                            var contents = reader.ReadToEnd();
                            writeStub = contents != stubData;
                        }
                    else
                        writeStub = true;

                    if (writeStub)
                    {
                        // Create or update the stub
                        using (var writer = targetInfo.CreateText())
                        {
                            writer.Write(stubData);
                        }

                        madeChanges = true;

                        if (!targetInfo.Exists)
                        {
                            var assetPath = targetPathFull.Replace(Application.dataPath, "Assets");
                            AssetDatabase.ImportAsset(assetPath);

                            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                            AssetDatabase.SetLabels(obj, new[] {FMODLabel});
                        }
                    }
                }

                RemoveEmptyFMODFolders(bankTargetFolder);
            }
            catch (Exception exception)
            {
                Debug.LogErrorFormat("FMOD: Updating bank stubs in {0} to match {1}",
                    bankTargetFolder, bankSourceFolder);
                Debug.LogException(exception);
                return;
            }

            if (madeChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.LogFormat("FMOD: Updated bank stubs in {0} to match {1}", bankTargetFolder, bankSourceFolder);
            }
        }

        private static void EnsureFoldersExist(string filePath, string basePath)
        {
            var dataPath = Application.dataPath + "/";

            if (!basePath.StartsWith(dataPath))
                throw new ArgumentException(
                    string.Format("Base path {0} is not within the Assets folder", basePath), "basePath");

            var lastSlash = filePath.LastIndexOf('/');

            if (lastSlash == -1)
                // No folders
                return;

            var assetString = filePath.Substring(0, lastSlash);

            var folders = assetString.Split('/');
            var parentFolder = "Assets/" + basePath.Substring(dataPath.Length);

            for (var i = 0; i < folders.Length; ++i)
            {
                var folderPath = parentFolder + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);

                    var folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
                    AssetDatabase.SetLabels(folder, new[] {FMODLabel});
                }

                parentFolder = folderPath;
            }
        }

        private static void BuildTargetChanged()
        {
            RefreshBanks();
#if UNITY_ANDROID
            Settings.Instance.AndroidUseOBB = PlayerSettings.Android.useAPKExpansionFiles;
#endif
        }

        private static void OnCacheChange()
        {
            var masterBanks = new List<string>();
            var banks = new List<string>();

            var settings = Settings.Instance;
            var hasChanged = false;

            foreach (var bankRef in eventCache.MasterBanks) masterBanks.Add(bankRef.Name);

            if (!CompareLists(masterBanks, settings.MasterBanks))
            {
                settings.MasterBanks.Clear();
                settings.MasterBanks.AddRange(masterBanks);
                hasChanged = true;
            }

            foreach (var bankRef in eventCache.EditorBanks)
                if (!eventCache.MasterBanks.Contains(bankRef) &&
                    !eventCache.StringsBanks.Contains(bankRef))
                    banks.Add(bankRef.Name);
            banks.Sort((a, b) => string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase));

            if (!CompareLists(banks, settings.Banks))
            {
                settings.Banks.Clear();
                settings.Banks.AddRange(banks);
                hasChanged = true;
            }

            if (hasChanged) EditorUtility.SetDirty(settings);
        }

        public static DateTime CacheTime
        {
            get
            {
                if (eventCache != null)
                    return eventCache.StringsBankWriteTime;
                return DateTime.MinValue;
            }
        }

        public static List<EditorEventRef> Events
        {
            get
            {
                AffirmEventCache();
                return eventCache.EditorEvents;
            }
        }

        public static List<EditorBankRef> Banks
        {
            get
            {
                AffirmEventCache();
                return eventCache.EditorBanks;
            }
        }

        public static List<EditorParamRef> Parameters
        {
            get
            {
                AffirmEventCache();
                return eventCache.EditorParameters;
            }
        }

        public static List<EditorBankRef> MasterBanks
        {
            get
            {
                AffirmEventCache();
                return eventCache.MasterBanks;
            }
        }

        public static bool IsLoaded => Settings.Instance.SourceBankPath != null;

        public static bool IsValid
        {
            get
            {
                AffirmEventCache();
                return eventCache.StringsBankWriteTime != DateTime.MinValue;
            }
        }

        public static EditorEventRef EventFromPath(string pathOrGuid)
        {
            EditorEventRef eventRef;
            if (pathOrGuid.StartsWith("{"))
            {
                var guid = new Guid(pathOrGuid);
                eventRef = EventFromGUID(guid);
            }
            else
            {
                eventRef = EventFromString(pathOrGuid);
            }

            return eventRef;
        }

        public static EditorEventRef EventFromString(string path)
        {
            AffirmEventCache();
            return eventCache.EditorEvents.Find(x => x.Path.Equals(path, StringComparison.CurrentCultureIgnoreCase));
        }

        public static EditorEventRef EventFromGUID(Guid guid)
        {
            AffirmEventCache();
            return eventCache.EditorEvents.Find(x => x.Guid == guid);
        }

        public static EditorParamRef ParamFromPath(string name)
        {
            AffirmEventCache();
            return eventCache.EditorParameters.Find(x =>
                x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        public class ActiveBuildTargetListener : IActiveBuildTargetChanged
        {
            public int callbackOrder => 0;

            public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
            {
                BuildTargetChanged();
            }
        }

#if UNITY_2018_1_OR_NEWER
        public class PreprocessScene : IProcessSceneWithReport
        {
            public int callbackOrder => 0;

            public void OnProcessScene(Scene scene, BuildReport report)
            {
                if (report == null) return;

                CheckValidEventRefs(scene);
            }
        }
#else
        public class PreprocessScene : IProcessScene
        {
            public int callbackOrder { get { return 0; } }

            public void OnProcessScene(UnityEngine.SceneManagement.Scene scene)
            {
                CheckValidEventRefs(scene);
            }
        }
#endif

        private static bool CompareLists(List<string> tempBanks, List<string> banks)
        {
            if (tempBanks.Count != banks.Count)
                return false;

            for (var i = 0; i < tempBanks.Count; i++)
                if (tempBanks[i] != banks[i])
                    return false;
            return true;
        }

        private static bool AssetHasLabel(string assetPath, string label)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            var labels = AssetDatabase.GetLabels(asset);

            return labels.Contains(label);
        }

        private const string AssetsFolderName = "Assets";

        public static void RemoveBanks(string basePath)
        {
            if (!Directory.Exists(basePath)) return;

            var filePaths = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);

            foreach (var filePath in filePaths)
                if (!filePath.EndsWith(".meta"))
                {
                    var assetPath = filePath.Replace(Application.dataPath, AssetsFolderName);

                    if (AssetHasLabel(assetPath, FMODLabel)) AssetDatabase.MoveAssetToTrash(assetPath);
                }

            RemoveEmptyFMODFolders(basePath);

            if (Directory.GetFileSystemEntries(basePath).Length == 0)
            {
                var baseFolder = basePath.Replace(Application.dataPath, AssetsFolderName);
                AssetDatabase.MoveAssetToTrash(baseFolder);
            }
        }

        public static void MoveBanks(string from, string to)
        {
            if (!Directory.Exists(from)) return;

            if (!Directory.Exists(to)) Directory.CreateDirectory(to);

            var oldBankFiles = Directory.GetFiles(from);

            foreach (var oldBankFileName in oldBankFiles)
            {
                if (oldBankFileName.EndsWith(".meta"))
                    continue;
                var assetString = oldBankFileName.Replace(Application.dataPath, "Assets");
                AssetDatabase.ImportAsset(assetString);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(assetString);
                var labels = AssetDatabase.GetLabels(obj);
                foreach (var label in labels)
                    if (label.Equals("FMOD"))
                    {
                        AssetDatabase.MoveAsset(assetString, to);
                        break;
                    }
            }

            if (Directory.GetFiles(Path.GetDirectoryName(oldBankFiles[0])).Length == 0)
                Directory.Delete(Path.GetDirectoryName(oldBankFiles[0]));
        }

        public static void RemoveEmptyFMODFolders(string basePath)
        {
            var folderPaths = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories);

            // Process longest paths first so parent folders are cleared out when we get to them
            Array.Sort(folderPaths, (a, b) => b.Length.CompareTo(a.Length));

            foreach (var folderPath in folderPaths)
            {
                var assetPath = folderPath.Replace(Application.dataPath, AssetsFolderName);

                if (AssetHasLabel(assetPath, FMODLabel) && Directory.GetFileSystemEntries(folderPath).Length == 0)
                    AssetDatabase.MoveAssetToTrash(assetPath);
            }
        }
    }
}