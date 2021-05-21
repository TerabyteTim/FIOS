using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TT.FIOS.Editor
{
    /// <summary>
    /// StarEditor.cs - V1
    ///
    /// Editor for our star component
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    [CustomEditor(typeof(Star))]
    public class StarEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Path to the UXML file for the star inspector
        /// </summary>
        private const string UxmlPath = "Assets/UI Elements/StarInspector.uxml";

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
                    Debug.LogErrorFormat("Missing UXML file for star inspector at path \"{0}\". Please ensure the file is there!",
                                         UxmlPath);
                    EditorApplication.Beep();
                    errorShown = true;
                }

                return base.CreateInspectorGUI();
            }

            errorShown = false;

            //Clone our UI and add to our editor window
            VisualElement ui = tree.CloneTree();

            //Re-setup when any value changes
            ui.Q<PropertyField>(StarColorProp).RegisterCallback<ChangeEvent<Color>>(change => { ReSetupStar(); });

            ui.Q<PropertyField>(StarRadiusProp).RegisterCallback<ChangeEvent<float>>(change =>
            {
                StarWizardEditor.ValidateNonNegative(change);
                ReSetupStar();
            });

            ui.Q<PropertyField>(GravityRadiusProp).RegisterCallback<ChangeEvent<float>>(change =>
            {
                StarWizardEditor.ValidateNonNegative(change);
                ReSetupStar();
            });

            ui.Q<PropertyField>(StarNameProp).RegisterCallback<ChangeEvent<string>>(change => { ReSetupStar(); });

            return ui;
        }

        /// <summary>
        /// Calls the ReSetupStar() function on the star this script is editing
        /// </summary>
        private void ReSetupStar()
        {
            foreach (Object obj in serializedObject.targetObjects)
            {
                Star s;

                if ((s = obj as Star) != null)
                    s.ReSetupStar();
            }
        }
    }
}