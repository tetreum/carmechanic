using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    public class BankRefreshWindow : EditorWindow
    {
        private const float CloseDelay = 5;
        private static BankRefreshWindow instance;
        private float closeTime = float.MaxValue;
        private SerializedProperty cooldown;
        private string lastRefreshError;

        private bool readyToRefreshBanks;

        private SerializedObject serializedSettings;
        private SerializedProperty showWindow;

        public static bool IsVisible => instance != null;

        public static bool ReadyToRefreshBanks => instance == null || instance.readyToRefreshBanks;

        private void OnEnable()
        {
            serializedSettings = new SerializedObject(Settings.Instance);
            cooldown = serializedSettings.FindProperty("BankRefreshCooldown");
            showWindow = serializedSettings.FindProperty("ShowBankRefreshWindow");

            // instance is set to null when scripts are recompiled
            if (instance == null)
                instance = this;
            else if (instance != this) Close();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnGUI()
        {
            serializedSettings.Update();

            DrawStatus();

            GUILayout.FlexibleSpace();

            SettingsEditor.DisplayBankRefreshSettings(cooldown, showWindow, false);

            DrawButtons();

            serializedSettings.ApplyModifiedProperties();
        }

        private void OnInspectorUpdate()
        {
            Repaint();

            if (BankRefresher.TimeUntilBankRefresh() != float.MaxValue) closeTime = float.MaxValue;

            if (Time.realtimeSinceStartup > closeTime) Close();
        }

        public static void ShowWindow()
        {
            if (instance == null)
            {
                instance = CreateInstance<BankRefreshWindow>();
                instance.titleContent = new GUIContent("FMOD Bank Refresh Status");
                instance.minSize = new Vector2(400, 200);
                instance.maxSize = new Vector2(1000, 200);

                instance.ShowUtility();
            }
        }

        public static void HandleBankRefresh(string error)
        {
            if (error != null) Debug.LogErrorFormat("FMOD: Bank refresh failed: {0}", error);

            if (instance != null)
            {
                instance.readyToRefreshBanks = false;
                instance.lastRefreshError = error;

                if (error == null) instance.closeTime = Time.realtimeSinceStartup + CloseDelay;
            }
        }

        private bool ConsumeEscapeKey()
        {
            if (focusedWindow == this && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        private void DrawStatus()
        {
            var labelStyle = new GUIStyle(EditorStyles.whiteLargeLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;

            var largeErrorStyle = new GUIStyle(labelStyle);
            largeErrorStyle.normal.textColor = Color.red;

            var errorStyle = new GUIStyle(GUI.skin.box);
            errorStyle.alignment = TextAnchor.UpperLeft;
            errorStyle.wordWrap = true;
            errorStyle.normal.textColor = Color.red;

            var timeSinceFileChange = BankRefresher.TimeSinceSourceFileChange();

            if (timeSinceFileChange != float.MaxValue)
            {
                GUILayout.Label(string.Format("The FMOD source banks changed {0} ago.",
                    EditorUtils.DurationString(timeSinceFileChange)), labelStyle);

                var timeUntilBankRefresh = BankRefresher.TimeUntilBankRefresh();

                if (timeUntilBankRefresh == 0)
                {
                    GUILayout.Label("Refreshing banks now...", labelStyle);
                    readyToRefreshBanks = true;
                }
                else if (timeUntilBankRefresh != float.MaxValue)
                {
                    if (DrawCountdown("Refreshing banks", timeUntilBankRefresh, Settings.Instance.BankRefreshCooldown,
                            labelStyle)
                        || ConsumeEscapeKey())
                        BankRefresher.DisableAutoRefresh();
                }
                else
                {
                    GUILayout.Label("Would you like to refresh banks?", labelStyle);
                }
            }
            else
            {
                if (lastRefreshError == null)
                {
                    GUILayout.Label("The FMOD banks are up to date.", labelStyle);
                }
                else
                {
                    GUILayout.Label("Bank refresh failed:", largeErrorStyle);
                    GUILayout.Box(lastRefreshError, errorStyle, GUILayout.ExpandWidth(true));
                }
            }

            if (closeTime != float.MaxValue)
            {
                var timeUntilClose = Mathf.Max(0, closeTime - Time.realtimeSinceStartup);

                if (DrawCountdown("Closing", timeUntilClose, CloseDelay, labelStyle) || ConsumeEscapeKey())
                    closeTime = float.MaxValue;
            }
        }

        private static bool DrawCountdown(string text, float remainingTime, float totalTime, GUIStyle labelStyle)
        {
            GUILayout.Label(string.Format("{0} in {1}...", text, EditorUtils.DurationString(remainingTime)),
                labelStyle);

            const float boxHeight = 2;

            var controlRect = EditorGUILayout.GetControlRect(false, boxHeight * 2);

            var boxRect = controlRect;
            boxRect.width *= remainingTime / totalTime;
            boxRect.x += (controlRect.width - boxRect.width) / 2;
            boxRect.height = 2;

            GUI.DrawTexture(boxRect, EditorGUIUtility.whiteTexture);

            var cancelContent = new GUIContent("Cancel");

            controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2);

            var buttonRect = controlRect;
            buttonRect.width = 100;
            buttonRect.x += (controlRect.width - buttonRect.width) / 2;

            return GUI.Button(buttonRect, cancelContent);
        }

        private void DrawButtons()
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2);

            var buttonCount = 2;

            var closeRect = rect;
            closeRect.width = rect.width / buttonCount;

            var refreshRect = rect;
            refreshRect.xMin = closeRect.xMax;

            if (GUI.Button(closeRect, "Close")) Close();

            if (GUI.Button(refreshRect, "Refresh Banks Now")) EventManager.RefreshBanks();
        }
    }
}