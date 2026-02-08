using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.WorldMap
{
    [CreateAssetMenu(fileName = "WorldGenProfile", menuName = "Ancient Factory/World Gen Profile")]
    public class WorldGenProfile : ScriptableObject
    {
        [Title("Map Dimensions")]
        [Min(1)]
        public int Rings = 5;

        [Min(0)]
        public int CoreRadius = 1;

        [Title("Settlements")]
        [BoxGroup("Settlement Settings")]
        [Min(0)]
        public int SettlementTileCount = 3;

        [BoxGroup("Settlement Settings")]
        [Min(1)]
        public int MinSettlementDistanceFromCore = 3;

        [BoxGroup("Settlement Settings")]
        [Min(1)]
        public int MinSettlementSpacing = 3;

        [Title("Global Quality Settings")]
        [BoxGroup("Quality Standards")]
        [LabelWidth(100), MinMaxSlider(1, 500, true)]
        public Vector2Int ImpureAmounts = new Vector2Int(20, 55);

        [BoxGroup("Quality Standards")]
        [LabelWidth(100), MinMaxSlider(1, 500, true)]
        public Vector2Int NormalAmounts = new Vector2Int(56, 135);

        [BoxGroup("Quality Standards")]
        [LabelWidth(100), MinMaxSlider(1, 500, true)]
        public Vector2Int PureAmounts = new Vector2Int(136, 250);

        [Title("Resources")]
        [ListDrawerSettings(ShowIndexLabels = false, ShowFoldout = true, DefaultExpandedState = true)]
        public List<ResourceSpawnRule> ResourceRules = new();
    }

    [Serializable]
    public class ResourceSpawnRule
    {
        private string RuleName => Item ? Item.ItemName : "New Rule";

        public Color UiColor
        {
            get
            {
                if (Item == null) return new Color(0.85f, 0.85f, 0.85f, 1f);

                // Pastel palette for specific resources - lighter shades for background visibility
                var c = Item.ItemName.ToLowerInvariant();
                if (c.Contains("iron")) return new Color(0.82f, 0.82f, 0.85f);      // Light Metallic Grey (Changed from Rust to distinguish from Food/Copper)
                if (c.Contains("coal")) return new Color(0.75f, 0.75f, 0.78f);      // Light Grey-Blue
                if (c.Contains("copper")) return new Color(0.95f, 0.85f, 0.75f);    // Soft Orange
                if (c.Contains("gold")) return new Color(0.98f, 0.95f, 0.70f);      // Light Gold
                if (c.Contains("stone")) return new Color(0.85f, 0.85f, 0.82f);     // Light Stone
                if (c.Contains("water")) return new Color(0.75f, 0.85f, 0.95f);     // Light Blue
                if (c.Contains("sand")) return new Color(0.95f, 0.92f, 0.80f);      // Light Sand
                if (c.Contains("oil")) return new Color(0.78f, 0.75f, 0.82f);       // Light Purple-Grey
                if (c.Contains("food") || c.Contains("apple") || c.Contains("wheat")) return new Color(0.78f, 0.90f, 0.75f); // Light Green

                // Fallback Palette - lighter pastel tones
                int hash = Mathf.Abs(Item.name.GetHashCode());
                return Color.HSVToRGB((hash % 100) / 100f, 0.25f, 0.95f);
            }
        }

        [BoxGroup("@RuleName", ShowLabel = true), GUIColor("UiColor")]
        [HorizontalGroup("@RuleName/Main", 80)]
        [PreviewField(80, ObjectFieldAlignment.Left), HideLabel]
        [Required]
        public ItemDefinition Item;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [LabelWidth(140), Min(1)]
        public int CountMin = 1;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [LabelWidth(140), Min(1)]
        public int CountMax = 5;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [LabelWidth(140), Min(1)]
        public int MinDistanceFromCore = 2;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [FoldoutGroup("@RuleName/Main/Properties/Clustering", Expanded = false)]
        [LabelWidth(140)]
        public bool UseClustering = false;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [FoldoutGroup("@RuleName/Main/Properties/Clustering")]
        [ShowIf("UseClustering"), Min(1), LabelWidth(140)]
        public int ClusterSizeMin = 1;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [FoldoutGroup("@RuleName/Main/Properties/Clustering")]
        [ShowIf("UseClustering"), Min(1), LabelWidth(140)]
        public int ClusterSizeMax = 3;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [VerticalGroup("@RuleName/Main/Properties")]
        [FoldoutGroup("@RuleName/Main/Properties/Clustering")]
        [ShowIf("UseClustering"), Min(1), LabelWidth(140)]
        public int ClusterRadius = 1;

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [Title("Outcomes (Quality & Reserves)", horizontalLine: false)]
        [ListDrawerSettings(ShowIndexLabels = false, ShowFoldout = true, DefaultExpandedState = true)]
        public List<ResourceQualitySetting> QualitySettings = new();

        [BoxGroup("@RuleName"), GUIColor("UiColor")]
        [Title("Then Spawn Nearby... (Linked)", horizontalLine: false)]
        [ListDrawerSettings(ShowIndexLabels = false, ShowFoldout = true, DefaultExpandedState = true)]
        public List<LinkedResourceRule> LinkedRules = new();
    }

    [Serializable]
    public class ResourceQualitySetting
    {
        [HorizontalGroup("Row"), HideLabel]
        [LabelWidth(50)]
        public ResourceQuality Quality;

        [HorizontalGroup("Row"), LabelText("Weight"), LabelWidth(45)]
        [Tooltip("Probability Weight. Higher value = More likely to be chosen.\nExample: 10 vs 1 means the first is 10x more likely.")]
        [Min(1)]
        public int Weight = 10;
    }

    [Serializable]
    public class LinkedResourceRule
    {
        [HorizontalGroup("Link", 0.3f), HideLabel]
        public ItemDefinition Item;

        [HorizontalGroup("Link"), LabelText("%"), LabelWidth(20)]
        [Tooltip("Chance to spawn this extra resource.")]
        [Range(0, 100)]
        public int Chance = 50;

        [HorizontalGroup("Link"), LabelText("Dist"), LabelWidth(30)]
        [Tooltip("Distance from the main resource.")]
        [MinMaxSlider(1, 10, true)]
        public Vector2Int DistanceRange = new Vector2Int(1, 2);
    }
}
