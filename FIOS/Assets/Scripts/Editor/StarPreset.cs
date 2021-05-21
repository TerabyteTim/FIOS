using System;
using UnityEngine;

namespace TT.FIOS.Editor
{
    /// <summary>
    /// StarPreset.cs - V1
    ///
    /// Contains info on preset settings for a star
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    [Serializable]
    public class StarPreset
    {
        /// <param name="starCol">Color of the star renderer</param>
        /// <param name="radius">Radius in meters of the star renderer</param>
        /// <param name="gravityRadius">Radius in meters of the star's gravity well</param>
        /// <param name="newName">Name of the star</param>
        internal StarPreset(Color starCol, float radius, float gravityRadius, string newName)
        {
            starColor  = starCol;
            starRadius = radius;
            gravRadius = gravityRadius;
            starName   = newName;
        }

        [SerializeField, Tooltip("Color of the star renderer")]
        private Color starColor;

        [SerializeField, Tooltip("Radius in meters of the star renderer")]
        private float starRadius;

        [SerializeField, Tooltip("Radius in meters of the star's gravity well")]
        private float gravRadius;

        [SerializeField, Tooltip("Name of the star")]
        private string starName;

        /// <summary>
        /// Color of the star renderer
        /// </summary>
        internal Color StarColor => starColor;

        /// <summary>
        /// Radius in meters of the star renderer
        /// </summary>
        internal float StarRadius => starRadius;

        /// <summary>
        /// Radius in meters of the star's gravity well
        /// </summary>
        internal float GravityRadius => gravRadius;

        /// <summary>
        /// Name of the star
        /// </summary>
        internal string StarName => starName;
    }
}