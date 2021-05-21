using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TT.FIOS.Editor
{
    /// <summary>
    /// StarWizardEditor.cs - V1
    ///
    /// Provides custom code for the star preset wizard, providing logic for the UI elements
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    public class StarWizardEditor : EditorWindow
    {
        #region Fields & Properties

        /// <summary>
        /// Path to the UXML file for the star preset wizard
        /// </summary>
        private const string UxmlPath = "Assets/UI Elements/StarWizardWindow.uxml";

        /// <summary>
        /// Property name used to access our star color field
        /// </summary>
        private const string StarColorProp = "StarColor";

        /// <summary>
        /// Property name used to access our star radius float field
        /// </summary>
        private const string StarRadiusProp = "StarRadius";

        /// <summary>
        /// Property name used to access our gravity well radius float field
        /// </summary>
        private const string GravityRadiusProp = "GravityRadius";

        /// <summary>
        /// Property name used to access our star name text field
        /// </summary>
        private const string StarNameProp = "StarName";

        /// <summary>
        /// Property name used to access our empty star name error field
        /// </summary>
        private const string EmptyNameProp = "EmptyNameError";

        /// <summary>
        /// Property name used to access our non-unique star name warning field
        /// </summary>
        private const string NonUniqueNameProp = "BadNameWarning";

        /// <summary>
        /// Property name used to access our create preset button
        /// </summary>
        private const string CreatePresetButProp = "CreatePresetButton";

        /// <summary>
        /// Property name used to access our list of presets
        /// </summary>
        private const string PresetListProp = "PresetList";

        /// <summary>
        /// Property name used to access our delete preset button
        /// </summary>
        private const string DeletePresetButProp = "DeletePresetButton";

        /// <summary>
        /// Property name used to access our add preset to scene button
        /// </summary>
        private const string AddPresetButProp = "AddPresetToSceneButton";

        /// <summary>
        /// Text to display on our create preset button when a brand new preset will be created
        /// </summary>
        private const string CreateNewPresetText = "Create New Preset";

        /// <summary>
        /// Text to display on our create preset button when an existing preset will have it's settings overwritten
        /// </summary>
        private const string OverridePresetText = "Override Existing Preset";

        /// <summary>
        /// Minimum size of our editor window in pixels
        /// </summary>
        private static readonly Vector2 MinWindowSize = new Vector2(350, 450);

        /// <summary>
        /// Maximum size of our editor window in pixels
        /// </summary>
        private static readonly Vector2 MaxWindowSize = new Vector2(350, 450);

        private static EditorWindow window;
        private static int          selPresetIndex = -1;

        #endregion

        [InitializeOnLoadMethod]
        public static void InitEditorHooks()
        {
            //Make sure our star name is still valid when any undo/redo is performed
            Undo.undoRedoPerformed += () =>
            {
                if (window != null && window.rootVisualElement != null)
                    ValidateStarName(window.rootVisualElement.Q<TextField>(StarNameProp).value);
            };
        }

        [MenuItem("Fault in our Stars/Star Preset Wizard")]
        public static void ShowDefaultWindow() { window = GetWindow<StarWizardEditor>("Star Preset Wizard"); }

        private void OnEnable()
        {
            if (window == null)
                window = GetWindow<StarWizardEditor>("Star Preset Wizard");

            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);

            //If we were unable to load the asset, log error close window and exit
            if (tree == null)
            {
                window.Close();

                Debug.LogErrorFormat("Missing UXML file for star wizard editor window at path \"{0}\". Please ensure the file is there!",
                                     UxmlPath);
                EditorApplication.Beep();
                return;
            }

            //Clone our UI and add to our editor window
            VisualElement ui         = tree.CloneTree();
            VisualElement windowRoot = window.rootVisualElement;
            windowRoot.Add(ui);

            //Set our min window size
            window.minSize = MinWindowSize;
            window.maxSize = MaxWindowSize;

            //Hook into our create preset event
            windowRoot.Q<Button>(CreatePresetButProp).clicked += CreateNewPreset;

            //Register for field validation
            windowRoot.Q<FloatField>(StarRadiusProp).RegisterValueChangedCallback(ValidateNonNegative);
            windowRoot.Q<FloatField>(GravityRadiusProp).RegisterValueChangedCallback(ValidateNonNegative);

            TextField starName = windowRoot.Q<TextField>(StarNameProp);
            starName.RegisterValueChangedCallback(ValidateStarName);
            ValidateStarName(starName.value);

            //Reload our preset list and update that UI
            ReloadPresetList();

            //Hook into our delete preset button
            windowRoot.Q<Button>(DeletePresetButProp).clicked += DeleteSelectedPreset;

            //Hook into our add preset to scene event
            windowRoot.Q<Button>(AddPresetButProp).clicked += AddPresetToScene;
        }

        /// <summary>
        /// Verifies the given float is equal to or greater than zero, setting it to 0 if not
        /// </summary>
        /// <param name="change">Info on the changed property field to validate</param>
        internal static void ValidateNonNegative(ChangeEvent<float> change)
        {
            FloatField f;

            if (change.newValue < 0 && (f = change.target as FloatField) != null)
            {
                f.value = 0;
                Debug.LogError("Star radius must be greater than 0!");
                EditorApplication.Beep();
            }
        }

        /// <summary>
        /// Verifies our star name is unique and valid, if it isn't disables the create preset button and reveals the non-unique name error message
        /// </summary>
        /// <param name="change">Info on what our star name was changed to</param>
        private static void ValidateStarName(ChangeEvent<string> change) { ValidateStarName(change.newValue); }

        /// <summary>
        /// Verifies our star name is unique and valid, if it isn't unique selects the existing preset from our list and shows a warning about overwriting
        /// </summary>
        /// <param name="newStarName">New star name to validate</param>
        private static void ValidateStarName(string newStarName)
        {
            bool exists = !PresetListAsset.Instance.UniqueName(newStarName, out int existingIndex);

            SetCreatePresetState(!string.IsNullOrWhiteSpace(newStarName), exists);

            //Set list view selection based on name
            if (window != null && window.rootVisualElement != null)
                window.rootVisualElement.Q<ListView>(PresetListProp).selectedIndex = existingIndex;
        }

        /// <summary>
        /// Sets the clickable state for our create preset button and reveals/hides the empty and/or non-unique name error messages
        /// </summary>
        /// <param name="canCreate">True if we should enable our create preset button, false if disable</param>
        /// <param name="showNameWarning">If true we will reveal our overwrite preset message</param>
        private static void SetCreatePresetState(bool canCreate, bool showNameWarning)
        {
            //Exit if bad window
            VisualElement windowRoot;

            if (window == null || (windowRoot = window.rootVisualElement) == null)
                return;

            Button createBut = windowRoot.Q<Button>(CreatePresetButProp);
            createBut.SetEnabled(canCreate);
            createBut.text = showNameWarning ? OverridePresetText : CreateNewPresetText;

            windowRoot.Q<Label>(EmptyNameProp).style.display =
                canCreate ? DisplayStyle.None : DisplayStyle.Flex;

            windowRoot.Q<Label>(NonUniqueNameProp).style.display =
                showNameWarning ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Reloads our list of presets available in the project, updating the wizard editor window with our options
        /// Call this when the preset list asset is changed externally to reload the UI for any open wizards
        /// </summary>
        internal static void ReloadPresetList()
        {
            //If our window is invalid exit
            if (window == null || window.rootVisualElement == null)
                return;

            //Get the names of all our star presets, used for our list
            string[] presetNames = PresetListAsset.Instance.StarPresets.Select(p => p.StarName).ToArray();

            ListView list = window.rootVisualElement.Q<ListView>();

            //Remove all existing children content
            list.Clear();

            //Add our make & bind functions, set our source, and refresh the view
            list.makeItem      = () => new Label();
            list.bindItem      = (e, i) => ((Label) e).text = (i < presetNames.Length) ? presetNames[i] : string.Empty;
            list.selectionType = SelectionType.Single;
            list.itemsSource   = presetNames;
            list.Refresh();

            //When our list selection changes, update our settings window and "Add preset" button
            list.onSelectionChanged += changes => SelectPreset(list.selectedIndex);

            //Update our settings window and add preset button for nothing being selected
            SelectPreset(list.selectedIndex);

            //Make sure our current name is still valid
            ValidateStarName(window.rootVisualElement.Q<TextField>(StarNameProp).value);
        }

        /// <summary>
        /// Deletes our currently selected preset from the preset list
        /// </summary>
        private static void DeleteSelectedPreset()
        {
            if (selPresetIndex >= 0)
                PresetListAsset.Instance.RemovePreset(selPresetIndex);
        }

        /// <summary>
        /// Selects the preset a the given index, updating our preset settings and add preset to scene button
        /// </summary>
        /// <param name="newPresetIndex">Index of the preset in our project presets list to select</param>
        private static void SelectPreset(int newPresetIndex)
        {
            //If bad window exit
            VisualElement windowRoot;
            selPresetIndex = newPresetIndex;

            if (window == null || (windowRoot = window.rootVisualElement) == null)
                return;

            bool presetSelected = newPresetIndex >= 0;

            //If new preset is selected, change our current settings to the values from that preset
            if (presetSelected)
            {
                StarPreset preset = PresetListAsset.Instance.StarPresets[newPresetIndex];
                windowRoot.Q<ColorField>(StarColorProp).value     = preset.StarColor;
                windowRoot.Q<FloatField>(StarRadiusProp).value    = preset.StarRadius;
                windowRoot.Q<FloatField>(GravityRadiusProp).value = preset.GravityRadius;
                windowRoot.Q<TextField>(StarNameProp).value       = preset.StarName;
            }

            //Our add preset to scene button should only be clickable when we have a valid selection
            windowRoot.Q<Button>(DeletePresetButProp).SetEnabled(presetSelected);
            windowRoot.Q<Button>(AddPresetButProp).SetEnabled(presetSelected);
        }

        /// <summary>
        /// Creates a new preset from our current wizard settings and adds it to our project presets list
        /// </summary>
        private static void CreateNewPreset()
        {
            //If window is invalid, exit
            VisualElement windowRoot;

            if (window == null || (windowRoot = window.rootVisualElement) == null)
                return;

            //Load settings for our preset from our fields
            Color  starCol    = windowRoot.Q<ColorField>(StarColorProp).value;
            float  starRadius = windowRoot.Q<FloatField>(StarRadiusProp).value;
            float  gravRadius = windowRoot.Q<FloatField>(GravityRadiusProp).value;
            string starName   = windowRoot.Q<TextField>(StarNameProp).value;

            //Create a new preset with these settings on our project asset
            PresetListAsset.Instance.AddPresetToList(starCol, starRadius, gravRadius, starName);
        }

        /// <summary>
        /// Adds an instance of our currently selected preset to the scene
        /// </summary>
        private static void AddPresetToScene()
        {
            //If window is invalid, exit
            if (window == null || window.rootVisualElement == null)
                return;

            //Get our preset settings for our currently selected preset
            StarPreset preset;

            if (selPresetIndex < 0 || selPresetIndex >= PresetListAsset.Instance.StarPresets.Length ||
                (preset = PresetListAsset.Instance.StarPresets[selPresetIndex]) == null)
            {
                Debug.LogError("Unable to add preset to scene, no valid preset selected!");
                EditorApplication.Beep();
                return;
            }

            //Re-select our current preset so our UI values update
            SelectPreset(selPresetIndex);

            string undoText = string.Format("Add \"{0}\" star to scene", preset.StarName);
            Undo.SetCurrentGroupName(undoText);

            //Instantiate a star and set it up with the selected preset settings
            GameObject starObj;
            Undo.RegisterCreatedObjectUndo(starObj = GameObject.CreatePrimitive(PrimitiveType.Sphere), undoText);

            Star newStar = Undo.AddComponent<Star>(starObj);

            Undo.RecordObject(newStar, undoText);
            newStar.SetupStar(preset.StarColor, preset.StarRadius, preset.GravityRadius, preset.StarName);

            Undo.IncrementCurrentGroup();
        }
    }
}