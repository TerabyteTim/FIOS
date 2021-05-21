using System.IO;
using UnityEditor;
using UnityEngine;

namespace TT.FIOS
{
    /// <summary>
    /// Star.cs - V1
    ///
    /// This allows an attached renderer to be setup as a star, and draws a gizmo for the star's gravity well
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class Star : MonoBehaviour
    {
        /// <summary>
        /// Directory relative to the assets directory that we store our star materials at
        /// </summary>
        private const string AssetRelDir = "/Materials/";

        [SerializeField, Tooltip("Color of the star")]
        private Color starCol;

        [SerializeField, Tooltip("Radius in meters of the star's renderer")]
        private float starRadius;

        [SerializeField, Tooltip("Radius in meters of the star's gravity well")]
        private float gravRadius;

        [SerializeField, Tooltip("Name of this star")]
        private string starName;

        [SerializeField, HideInInspector, Tooltip("Keep track of if we've instantiated a valid material asset")]
        private bool setupRun;

        /// <summary>
        /// Sets up this star using existing settings - only works if SetupStar has been called at least once
        /// </summary>
        public void ReSetupStar()
        {
            if (!setupRun)
            {
                Debug.LogError("SetupStar() must be called at least once before calling ReSetupStar()");
                return;
            }

            SetupStar(starCol, starRadius, gravRadius, starName);
        }

        /// <summary>
        /// Sets up this star with the given settings
        /// </summary>
        /// <param name="starColor">Color for the renderer attached to this star</param>
        /// <param name="starRenderRadius">Radius in meters for this star</param>
        /// <param name="gravityRadius">Radius in meters of the gravity well for this star</param>
        /// <param name="sName">Name for the star</param>
        public void SetupStar(Color starColor, float starRenderRadius, float gravityRadius, string sName)
        {
            //Set our name, gravity well radius, and size
            starCol              = starColor;
            starRadius           = starRenderRadius;
            gravRadius           = gravityRadius;
            name                 = starName = sName;
            transform.localScale = Vector3.one * starRenderRadius * 2;

            //Set our material color
#if UNITY_EDITOR
            Material m = GetComponent<Renderer>().sharedMaterial;

            //If no unique mat has been created, create a material in the project folder for us
            if (!setupRun)
            {
                m = new Material(m);

                string dirPath   = Application.dataPath + AssetRelDir;
                string assetPath = string.Format("Assets{0}{1}.mat", AssetRelDir, starName);

                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                AssetDatabase.CreateAsset(m, AssetDatabase.GenerateUniqueAssetPath(assetPath));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.LogFormat("Created material for star \"{0}\" at path \"{1}\"", starName, assetPath);
                EditorApplication.Beep();

                GetComponent<Renderer>().material = m;

                setupRun = true;
            }
#else
            Material m = GetComponent<Renderer>().material;
#endif

            //Set our star color (.color prop doesn't work, it refs _Color internally which doesn't update for some reason)
            m.SetColor("_BaseColor", starColor);
        }

        private void OnDrawGizmos()
        {
            //Draw our gravity well as a gizmo wireframe sphere
            Color c = Gizmos.color;
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(transform.position, gravRadius);

            Gizmos.color = c;
        }
    }
}