using System;
using System.IO;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.Tutorial;
using CookAndDeliver.Delivery;
using CookAndDeliver.SaveLoad.Core;
using CookAndDeliver.SaveLoad.Objects;
using UnityEditor;
using UnityEngine;

namespace CookAndDeliver.Cooking.Editor
{
    // Editor utility and debug window for inspecting and resetting save/tutorial progression data
    public class TutorialSaveDebugEditor : EditorWindow
    {
        private const string DevSkipPrefKey = "CookAndDeliver.Dev.ShowTutorialSkipControls";
        private const string DevSkipMenuPath = "Tools/Tutorial/Show Dev Skip Controls";

        private Vector2 _scrollPos;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;

        public static bool ShowDevSkipControls
        {
            get => EditorPrefs.GetBool(DevSkipPrefKey, false);
            set
            {
                if (EditorPrefs.GetBool(DevSkipPrefKey, false) == value) return;
                EditorPrefs.SetBool(DevSkipPrefKey, value);
                ApplyDevSkipVisibilityToActiveControllers();
            }
        }

        private static string SaveDataFilePath =>
            Path.Combine(Application.persistentDataPath, "SaveData", "savedata.json");

        private static string DeliveryProgressPath =>
            Path.Combine(Application.persistentDataPath, "Delivery", "progress.json");

        [MenuItem("Tools/Tutorial/Reset All Save Data (Test Tutorial)", false, 10)]
        public static void ResetAllSaveDataMenu()
        {
            ResetAllSaveData(out string report);
            Debug.Log($"[TutorialSaveDebug] {report}");
        }

        [MenuItem("Tools/Tutorial/Reset Tutorial Flags Only (Day 1)", false, 11)]
        public static void ResetTutorialFlagsOnlyMenu()
        {
            ResetTutorialFlagsOnly(out string report);
            Debug.Log($"[TutorialSaveDebug] {report}");
        }

        [MenuItem(DevSkipMenuPath, false, 15)]
        public static void ToggleDevSkipControlsMenu()
        {
            bool next = !ShowDevSkipControls;
            ShowDevSkipControls = next;
            Debug.Log($"[TutorialSaveDebug] Dev tutorial skip controls: {(next ? "Shown" : "Hidden")}");
        }

        [MenuItem(DevSkipMenuPath, true)]
        private static bool ValidateDevSkipControlsMenu()
        {
            Menu.SetChecked(DevSkipMenuPath, ShowDevSkipControls);
            return true;
        }

        private static void ApplyDevSkipVisibilityToActiveControllers()
        {
            if (!EditorApplication.isPlaying) return;

            var cookingDirectors = FindObjectsByType<CookingTutorialDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cookingDirectors.Length; i++)
            {
                cookingDirectors[i]?.RefreshDevSkipVisibility();
            }

            var deliveryTutorials = FindObjectsByType<DeliveryTutorialController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < deliveryTutorials.Length; i++)
            {
                deliveryTutorials[i]?.RefreshDevSkipVisibility();
            }
        }

        [MenuItem("Tools/Tutorial/Open Save & Tutorial Debugger", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<TutorialSaveDebugEditor>("Save & Tutorial Debugger");
            window.minSize = new Vector2(440f, 380f);
            window.Show();
        }

        [MenuItem("Tools/Tutorial/Open Save Folder", false, 30)]
        public static void OpenSaveFolder()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        public static void ResetAllSaveData(out string report)
        {
            int deletedCount = 0;

            deletedCount += DeleteFileIfExists(SaveDataFilePath);
            deletedCount += DeleteFileIfExists(SaveDataFilePath + ".bak");
            deletedCount += DeleteFileIfExists(SaveDataFilePath + ".tmp");

            deletedCount += DeleteFileIfExists(DeliveryProgressPath);
            deletedCount += DeleteFileIfExists(DeliveryProgressPath + ".bak");
            deletedCount += DeleteFileIfExists(DeliveryProgressPath + ".tmp");

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            SaveLoadManager.SetHandler(new JsonSaveLoadHandler());
            GameProgressStore.ResetCacheForEditorTest();

            report = $"Reset complete: removed {deletedCount} save file(s), cleared PlayerPrefs, and reset in-memory caches for Day 1 tutorial testing.";
        }

        public static void ResetTutorialFlagsOnly(out string report)
        {
            SaveTree tree = ReadSaveTree() ?? new SaveTree();
            GameProgressSLO progress = ReadProgressFromTree(tree) ?? new GameProgressSLO();

            progress.CurrentDay = 1;
            progress.IsCookingTutorialCompleted = false;
            progress.IsDeliveryTutorialCompleted = false;
            WriteProgressToTree(tree, progress);

            DeleteFileIfExists(DeliveryProgressPath);
            DeleteFileIfExists(DeliveryProgressPath + ".bak");
            DeleteFileIfExists(DeliveryProgressPath + ".tmp");

            SaveLoadManager.SetHandler(new JsonSaveLoadHandler());
            GameProgressStore.ResetCacheForEditorTest();

            report = $"Updated savedata.json to Day 1 with Cooking & Delivery tutorials reset (kept Coins={progress.TotalCoins}, Recipes={progress.UnlockedRecipeIds.Count}).";
        }

        public static void SetDay2CompletedForTest(out string report)
        {
            SaveTree tree = ReadSaveTree() ?? new SaveTree();
            GameProgressSLO progress = ReadProgressFromTree(tree) ?? new GameProgressSLO();

            progress.CurrentDay = Mathf.Max(2, progress.CurrentDay);
            progress.IsCookingTutorialCompleted = true;
            progress.IsDeliveryTutorialCompleted = true;
            WriteProgressToTree(tree, progress);

            var deliveryStore = new DeliveryProgressStore(Path.GetDirectoryName(DeliveryProgressPath));
            deliveryStore.MarkTutorialCompleted();

            SaveLoadManager.SetHandler(new JsonSaveLoadHandler());
            GameProgressStore.ResetCacheForEditorTest();

            report = $"Savedata updated to Day {progress.CurrentDay} with both tutorials marked completed.";
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Save & tutorial debugger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use these actions to wipe or adjust persisted save files when testing Day 1 Cooking and Delivery tutorials.",
                MessageType.None);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Persistent storage paths", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(Application.persistentDataPath, EditorStyles.textField, GUILayout.Height(20f));

            DrawMainSaveSection();
            EditorGUILayout.Space(8f);
            DrawDeliverySaveSection();
            EditorGUILayout.Space(10f);
            DrawActionButtons();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMainSaveSection()
        {
            EditorGUILayout.LabelField("Main game save (SaveData/savedata.json)", EditorStyles.boldLabel);
            bool exists = File.Exists(SaveDataFilePath);
            EditorGUILayout.LabelField("File exists", exists ? "Yes" : "No (Fresh New Game)");

            if (!exists) return;

            SaveTree tree = ReadSaveTree();
            GameProgressSLO progress = ReadProgressFromTree(tree);
            if (progress == null)
            {
                EditorGUILayout.LabelField("GameProgress payload", "Missing or encrypted");
                return;
            }

            EditorGUILayout.LabelField("Current day", progress.CurrentDay.ToString());
            EditorGUILayout.LabelField("Total coins", progress.TotalCoins.ToString());
            EditorGUILayout.LabelField("Total orders completed", progress.TotalOrdersCompleted.ToString());
            EditorGUILayout.LabelField("Cooking tutorial completed", progress.IsCookingTutorialCompleted ? "Yes" : "No");
            EditorGUILayout.LabelField("Delivery tutorial completed", progress.IsDeliveryTutorialCompleted ? "Yes" : "No");
            EditorGUILayout.LabelField("Unlocked recipe count", progress.UnlockedRecipeIds.Count.ToString());
        }

        private void DrawDeliverySaveSection()
        {
            EditorGUILayout.LabelField("Standalone delivery save (Delivery/progress.json)", EditorStyles.boldLabel);
            bool exists = File.Exists(DeliveryProgressPath);
            EditorGUILayout.LabelField("File exists", exists ? "Yes" : "No (Fresh)");
            if (!exists) return;

            try
            {
                var delivery = JsonUtility.FromJson<DeliveryProgress>(File.ReadAllText(DeliveryProgressPath));
                if (delivery != null)
                {
                    EditorGUILayout.LabelField("Standalone tutorialCompleted", delivery.tutorialCompleted ? "Yes" : "No");
                    EditorGUILayout.LabelField("Standalone totalDelivered", delivery.totalDelivered.ToString());
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField("Parse status", ex.Message);
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("Developer options", EditorStyles.boldLabel);
            bool currentDevSkip = ShowDevSkipControls;
            bool nextDevSkip = EditorGUILayout.ToggleLeft(
                "Show dev skip tutorial controls in Game View (Skip / Day 2 / Delivery Skip)",
                currentDevSkip);
            if (nextDevSkip != currentDevSkip)
            {
                ShowDevSkipControls = nextDevSkip;
                SetStatus($"Dev skip controls {(nextDevSkip ? "shown" : "hidden")}.", MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(1f, 0.55f, 0.5f);
            if (GUILayout.Button("Reset all save data (clean Day 1 tutorial test)", GUILayout.Height(30f)))
            {
                ResetAllSaveData(out string report);
                SetStatus(report, MessageType.Info);
            }

            GUI.backgroundColor = new Color(1f, 0.86f, 0.55f);
            if (GUILayout.Button("Reset tutorial flags only (keep coins and recipes, set Day 1)", GUILayout.Height(26f)))
            {
                ResetTutorialFlagsOnly(out string report);
                SetStatus(report, MessageType.Info);
            }

            GUI.backgroundColor = new Color(0.7f, 0.95f, 0.72f);
            if (GUILayout.Button("Set Day 2 and mark tutorials completed", GUILayout.Height(26f)))
            {
                SetDay2CompletedForTest(out string report);
                SetStatus(report, MessageType.Info);
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Open persistent save folder", GUILayout.Height(24f)))
            {
                OpenSaveFolder();
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        private static int DeleteFileIfExists(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            File.Delete(path);
            return 1;
        }

        private static SaveTree ReadSaveTree()
        {
            if (!File.Exists(SaveDataFilePath)) return null;
            try
            {
                string raw = File.ReadAllText(SaveDataFilePath);
                if (string.IsNullOrEmpty(raw)) return null;
                return JsonUtility.FromJson<SaveTree>(raw);
            }
            catch
            {
                return null;
            }
        }

        private static GameProgressSLO ReadProgressFromTree(SaveTree tree)
        {
            if (tree == null) return null;
            string payload = tree.Get(GameProgressStore.SaveKey);
            if (string.IsNullOrEmpty(payload)) return null;
            try
            {
                return JsonUtility.FromJson<GameProgressSLO>(payload);
            }
            catch
            {
                return null;
            }
        }

        private static void WriteProgressToTree(SaveTree tree, GameProgressSLO progress)
        {
            if (tree == null || progress == null) return;
            string dir = Path.GetDirectoryName(SaveDataFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            tree.Set(GameProgressStore.SaveKey, progress.Serialize());
            File.WriteAllText(SaveDataFilePath, tree.Serialize());
        }
    }
}
