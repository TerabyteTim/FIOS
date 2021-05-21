using System;
using UnityEngine;

namespace TT.FIOS.Editor
{
    /// <summary>
    /// StarPresetList.cs - V1
    ///
    /// Used for serializing a collection of star presets
    /// 
    /// Created by TerabyteTim (tim@terabytetim.com)
    /// </summary>
    [Serializable]
    public class StarPresetList
    {
        /// <param name="presetList">Star presets for list</param>
        public StarPresetList(StarPreset[] presetList) { presets = presetList; }

        [SerializeField]
        private StarPreset[] presets;

        /// <summary>
        /// Star presets in list
        /// </summary>
        public StarPreset[] Presets => presets;
    }
}