using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TT.FIOS.Editor
{
    /// <summary>
    /// PresetListAssetEditor.cs - V1
    ///
    /// Custom editor for our preset list asset
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    [CustomEditor(typeof(PresetListAsset))]
    public class PresetListAssetEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Path to the UXML file for the preset list asset inspector
        /// </summary>
        private const string UxmlPath = "Assets/UI Elements/PresetListInspector.uxml";

        /// <summary>
        /// Property name used to access our open wizard button
        /// </summary>
        private const string OpenWizardProp = "OpenWizard";

        /// <summary>
        /// Property name used to access our save presets button
        /// </summary>
        private const string SavePresetsProp = "SavePresets";

        /// <summary>
        /// Property name used to access our load presets button
        /// </summary>
        private const string LoadPresetsProp = "LoadPresets";

        private static bool errorShown;

        /// <inheritdoc />
        public override VisualElement CreateInspectorGUI()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);

            //If we were unable to load the asset, log error close window and exit
            if (tree == null)
            {
                if (!errorShown)
                {
                    Debug.LogErrorFormat("Missing UXML file for preset list asset inspector at path \"{0}\". Please ensure the file is there!",
                                         UxmlPath);
                    EditorApplication.Beep();
                    errorShown = true;
                }

                return base.CreateInspectorGUI();
            }

            errorShown = false;

            //Clone our UI and add to our editor window
            VisualElement ui = tree.CloneTree();

            //Register click event for each button
            ui.Q<Button>(OpenWizardProp).clicked  += StarWizardEditor.ShowDefaultWindow;
            ui.Q<Button>(SavePresetsProp).clicked += SavePresets;
            ui.Q<Button>(LoadPresetsProp).clicked += LoadPresets;

            return ui;
        }

        /// <summary>
        /// Saves all of our current project star presets to a JSON file
        /// </summary>
        private void SavePresets()
        {
            string savePath = EditorUtility.SaveFilePanelInProject("Save Presets", "FIOS_Star_Presets", "json",
                                                                   "Select a location to save project star presets to");

            //If user canceled, exit
            if (string.IsNullOrWhiteSpace(savePath))
                return;

            //Combine for asset path
            string filePath =
                string.Format("{0}/{1}",
                              Application.dataPath.Substring(0, Application.dataPath.Length - "Assets/".Length),
                              savePath);

            //Write our settings to the file
            File.WriteAllText(filePath, JsonUtility.ToJson(new StarPresetList(PresetListAsset.Instance.StarPresets)));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("Saved current project star presets to \"{0}\"", filePath);
            EditorApplication.Beep();
        }

        /// <summary>
        /// Loads a set of project star presets from a JSON file
        /// </summary>
        private void LoadPresets()
        {
            string filePath = EditorUtility.OpenFilePanel("Load Presets", Application.dataPath, "json");

            //If user canceled, exit
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            //Load our settings from the file
            Undo.RecordObject(PresetListAsset.Instance, "Load star presets from file");
            PresetListAsset.Instance.LoadPresetsFromJson(File.ReadAllText(filePath));

            Debug.LogFormat("Loaded project star presets from \"{0}\"", filePath);
            EditorApplication.Beep();
        }
    }
}