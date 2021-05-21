using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TT.FIOS.Editor
{
    /// <summary>
    /// PresetListAsset.cs - V1
    ///
    /// Editor asset used to store a list of preset stars created for the project
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    public class PresetListAsset : ScriptableObject
    {
        #region Fields & Properties

        /// <summary>
        /// Gets our star preset list instance
        /// </summary>
        internal static PresetListAsset Instance
        {
            get
            {
                //If our asset hasn't been loaded, try to load it. If none exists in the project, create our new asset
                if (asset == null &&
                    (asset = (PresetListAsset) AssetDatabase.LoadAssetAtPath(AssetPath, typeof(PresetListAsset))) ==
                    null)
                {
                    asset = CreateInstance<PresetListAsset>();

                    string dirPath = Application.dataPath + AssetRelDir;

                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);

                    AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(AssetPath));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.LogFormat("Created Fault in our Stars presets asset at path \"{0}\"", AssetPath);
                    EditorApplication.Beep();
                }

                return asset;
            }
        }

        /// <summary>
        /// Directory relative to the assets directory that we store our preset list asset at
        /// </summary>
        private const string AssetRelDir = "/FaultInOurStars/";

        /// <summary>
        /// Asset path we store our preset list asset at
        /// </summary>
        private const string AssetPath = "Assets" + AssetRelDir + "FIOS_StarPresets.asset";

        private static PresetListAsset asset;

        [SerializeField, ReadOnly(true), Tooltip("Star presets saved for this project")]
        private StarPreset[] starPresets = new StarPreset[0];

        /// <summary>
        /// Star presets saved for this project
        /// </summary>
        internal StarPreset[] StarPresets => starPresets;

        #endregion

        /// <summary>
        /// Checks if the given star name is not used by any star in our current list of presets
        /// </summary>
        /// <param name="starName">Name to check for uniqueness</param>
        /// <param name="existingIndex">Index of preset with the given name, or -1 if none</param>
        /// <returns>True if starName is unique</returns>
        internal bool UniqueName(string starName, out int existingIndex)
        {
            existingIndex = Array.FindIndex(starPresets,
                                            p => p.StarName.Equals(starName, StringComparison.OrdinalIgnoreCase));
            return existingIndex < 0;
        }

        /// <summary>
        /// Adds a new preset with the given settings to our project asset
        /// </summary>
        /// <param name="starCol">Color for our new star preset</param>
        /// <param name="starRadius">Radius in meters for our new star preset</param>
        /// <param name="gravRadius">Radius in meters for the gravity well of our new star preset</param>
        /// <param name="starName">Name for our star preset (if this name is already taken user will be prompted to confirm overwriting existing preset)</param>
        internal void AddPresetToList(Color starCol, float starRadius, float gravRadius, string starName)
        {
            //If name is empty exit with error
            if (string.IsNullOrWhiteSpace(starName))
            {
                Debug.LogError("Empty or null star name provided, unable to create preset!");
                EditorApplication.Beep();
                return;
            }

            bool overwriting = !UniqueName(starName, out int existingIndex);

            //Or if non-unique and user doesn't accept prompt exit silently
            if (overwriting &&
                !EditorUtility.DisplayDialog("Star Preset Creation",
                                             string
                                                 .Format("An existing preset named \"{0}\" will have it's settings overwritten with new values",
                                                         starName), "Overwrite Existing Preset", "Cancel"))
                return;

            //Resize our collection and add a new preset, marking our asset as dirty
            Undo.RecordObject(this,
                              string.Format("{0} Star Preset \"{1}\"", overwriting ? "Overwrite" : "Create", starName));

            if (!overwriting)
                Array.Resize(ref starPresets, starPresets.Length + 1);

            starPresets[overwriting ? existingIndex : starPresets.Length - 1] =
                new StarPreset(starCol, starRadius, gravRadius, starName);

            //Reload our preset list on any open editors
            StarWizardEditor.ReloadPresetList();
        }

        /// <summary>
        /// Removes a preset from our project asset at the given index
        /// </summary>
        /// <param name="index">Index of the preset to remove</param>
        internal void RemovePreset(int index)
        {
            //If a bad index exit
            if (index < 0 || index >= starPresets.Length)
            {
                Debug.LogError("Invalid preset index " + index);
                return;
            }

            string starName = starPresets[index].StarName;

            //If user cancels exit
            if (!EditorUtility.DisplayDialog("Star Preset Deletion",
                                             string.Format("Delete star preset \"{0}\"?", starName), "Delete Preset",
                                             "Cancel"))
                return;

            //Remove preset from our collection
            Undo.RecordObject(this, string.Format("Delete Star Preset \"{0}\"", starName));

            List<StarPreset> list = new List<StarPreset>(starPresets);
            list.RemoveAt(index);
            starPresets = list.ToArray();

            //Reload our preset list on any open editors
            StarWizardEditor.ReloadPresetList();
        }

        /// <summary>
        /// Loads a star preset collection from the given json data string
        /// </summary>
        /// <param name="json">String of json data to load our star presets from</param>
        internal void LoadPresetsFromJson(string json)
        {
            starPresets = JsonUtility.FromJson<StarPresetList>(json).Presets;
            StarWizardEditor.ReloadPresetList();
        }
    }
}