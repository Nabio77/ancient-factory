using UnityEngine;
using System.Collections.Generic;

using Sirenix.OdinInspector;

namespace AncientFactory.Core.Data
{
    [System.Serializable]
    public struct WonderStage
    {
        public string StageName;
        public List<ItemStack> Requirements;
        [PreviewField(50)]
        public Sprite Visual;
    }

    [CreateAssetMenu(fileName = "WonderDefinition", menuName = "Ancient Factory/Wonder Definition")]
    public class WonderDefinition : ScriptableObject
    {
        [Title("Wonder Stages")]
        [ListDrawerSettings(ShowFoldout = false, ShowIndexLabels = true)]
        public List<WonderStage> Stages = new();
    }
}
