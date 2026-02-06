using System.Collections.Generic;
using System.IO;
using System.Linq;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;
using UnityEditor;
using UnityEngine;

namespace AncientFactory.Editor
{
    public class GameDataGenerator : EditorWindow
    {
        [MenuItem("Ancient Factory/Generate Game Data")]
        public static void ShowWindow()
        {
            GetWindow<GameDataGenerator>("Game Data Generator");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Generate All Data"))
            {
                if (EditorUtility.DisplayDialog("Confirm Delete",
                    "This will delete ALL existing Items and Blueprints. Are you sure?", "Yes", "No"))
                {
                    GenerateAll();
                }
            }
        }

        private static void GenerateAll()
        {
            ClearAllData();
            var items = GenerateItems();
            var blueprints = GenerateBlueprints(items);
            GenerateTechTree(blueprints);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Game Data Generation Complete!");
        }

        private static void ClearAllData()
        {
            DeleteFolderContents("Assets/Items");
            DeleteFolderContents("Assets/Blueprints");
        }

        private static void DeleteFolderContents(string path)
        {
            if (Directory.Exists(path))
            {
                var guids = AssetDatabase.FindAssets("", new[] { path });
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }

        private static Dictionary<string, ItemDefinition> GenerateItems()
        {
            var items = new Dictionary<string, ItemDefinition>();

            // === T0 HARVEST (Raw Materials) ===
            CreateItem(items, "Copper Ore", ItemTier.Harvest, 1);
            CreateItem(items, "Tin Ore", ItemTier.Harvest, 1);
            CreateItem(items, "Iron Ore", ItemTier.Harvest, 1);
            CreateItem(items, "Gold Ore", ItemTier.Harvest, 2);
            CreateItem(items, "Clay", ItemTier.Harvest, 1);
            CreateItem(items, "Stone", ItemTier.Harvest, 1);
            CreateItem(items, "Sand", ItemTier.Harvest, 1);
            CreateItem(items, "Wood", ItemTier.Harvest, 1);
            CreateItem(items, "Flax", ItemTier.Harvest, 1);
            CreateItem(items, "Papyrus", ItemTier.Harvest, 1);
            CreateItem(items, "Coal", ItemTier.Harvest, 1);

            // === T1 REFINED (Basic Processing) ===
            CreateItem(items, "Copper Ingot", ItemTier.Refined, 2);
            CreateItem(items, "Tin Ingot", ItemTier.Refined, 2);
            CreateItem(items, "Iron Ingot", ItemTier.Refined, 3);
            CreateItem(items, "Gold Ingot", ItemTier.Refined, 5);
            CreateItem(items, "Bricks", ItemTier.Refined, 2);
            CreateItem(items, "Cut Stone", ItemTier.Refined, 2);
            CreateItem(items, "Glass", ItemTier.Refined, 3);
            CreateItem(items, "Planks", ItemTier.Refined, 2);
            CreateItem(items, "Linen Thread", ItemTier.Refined, 2);
            CreateItem(items, "Papyrus Sheet", ItemTier.Refined, 2);

            // === T2 CRAFTED (Secondary Processing) ===
            CreateItem(items, "Bronze", ItemTier.Crafted, 4);
            CreateItem(items, "Steel", ItemTier.Crafted, 5);
            CreateItem(items, "Cement", ItemTier.Crafted, 4);
            CreateItem(items, "Linen Cloth", ItemTier.Crafted, 4);
            CreateItem(items, "Rope", ItemTier.Crafted, 3);
            CreateItem(items, "Glassware", ItemTier.Crafted, 5);

            // === T3 ARTISAN (Skilled Craftwork) ===
            CreateItem(items, "Bronze Tools", ItemTier.Artisan, 8);
            CreateItem(items, "Iron Tools", ItemTier.Artisan, 10);
            CreateItem(items, "Pottery", ItemTier.Artisan, 8);
            CreateItem(items, "Stone Blocks", ItemTier.Artisan, 8);
            CreateItem(items, "Gold Jewelry", ItemTier.Artisan, 15);
            CreateItem(items, "Leather", ItemTier.Artisan, 6);
            CreateItem(items, "Scroll", ItemTier.Artisan, 7);

            // === T4 FINE (High-Quality Goods) ===
            CreateItem(items, "Bronze Weapons", ItemTier.Fine, 15);
            CreateItem(items, "Iron Weapons", ItemTier.Fine, 20);
            CreateItem(items, "Bronze Armor", ItemTier.Fine, 18);
            CreateItem(items, "Furniture", ItemTier.Fine, 12);
            CreateItem(items, "Mosaic", ItemTier.Fine, 16);

            // === T5 GRAND (Large Constructions) ===
            CreateItem(items, "Chariot Frame", ItemTier.Grand, 30);
            CreateItem(items, "Statue Base", ItemTier.Grand, 28);
            CreateItem(items, "Temple Column", ItemTier.Grand, 25);
            CreateItem(items, "War Engine Parts", ItemTier.Grand, 35);

            // === T6 MASTERWORK (Ultimate Achievements) ===
            CreateItem(items, "Chariot", ItemTier.Masterwork, 80);
            CreateItem(items, "Bronze Statue", ItemTier.Masterwork, 70);
            CreateItem(items, "Temple Component", ItemTier.Masterwork, 60);
            CreateItem(items, "War Catapult", ItemTier.Masterwork, 90);
            CreateItem(items, "Royal Throne", ItemTier.Masterwork, 100);

            // === FOOD CHAIN ===
            CreateItem(items, "Grain", ItemTier.Harvest, 1, isFood: true, nutrition: 1);
            CreateItem(items, "Grapes", ItemTier.Harvest, 1, isFood: true, nutrition: 1);
            CreateItem(items, "Olives", ItemTier.Harvest, 1, isFood: true, nutrition: 1);
            CreateItem(items, "Cattle", ItemTier.Harvest, 2, isFood: true, nutrition: 2);

            CreateItem(items, "Flour", ItemTier.Refined, 2, isFood: true, nutrition: 2);
            CreateItem(items, "Bread", ItemTier.Crafted, 4, isFood: true, nutrition: 5);
            CreateItem(items, "Wine", ItemTier.Crafted, 5, isFood: true, nutrition: 4);
            CreateItem(items, "Olive Oil", ItemTier.Crafted, 4, isFood: true, nutrition: 3);
            CreateItem(items, "Meat", ItemTier.Refined, 4, isFood: true, nutrition: 6);
            CreateItem(items, "Feast", ItemTier.Fine, 15, isFood: true, nutrition: 20);

            return items;
        }

        private static void CreateItem(Dictionary<string, ItemDefinition> dict, string name, ItemTier tier, int techPoints, bool isFood = false, int nutrition = 0)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = name.Replace(" ", "");

            var so = new SerializedObject(item);
            so.FindProperty("itemName").stringValue = name;
            so.FindProperty("tier").intValue = (int)tier;
            so.FindProperty("category").enumValueIndex = (int)tier;
            so.FindProperty("techPoints").intValue = techPoints;
            so.FindProperty("isFood").boolValue = isFood;
            if (isFood) so.FindProperty("nutritionalValue").intValue = nutrition;
            so.ApplyModifiedProperties();

            string folder = $"Assets/Items/{tier}";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = $"{folder}/{item.name}.asset";
            AssetDatabase.CreateAsset(item, path);
            dict[name] = item;
        }

        private static List<BlueprintDefinition> GenerateBlueprints(Dictionary<string, ItemDefinition> items)
        {
            var blueprints = new List<BlueprintDefinition>();

            // === FORGE BLUEPRINTS (Ore → Ingots) ===
            blueprints.Add(CreateBlueprint("Copper Ingot", BlueprintType.Forge, items["Copper Ingot"], 2f,
                new[] { (items["Copper Ore"], 2) }, isStarter: true, unlockCost: 0, carbon: 8));

            blueprints.Add(CreateBlueprint("Tin Ingot", BlueprintType.Forge, items["Tin Ingot"], 2f,
                new[] { (items["Tin Ore"], 2) }, isStarter: true, unlockCost: 0, carbon: 8));

            blueprints.Add(CreateBlueprint("Iron Ingot", BlueprintType.Forge, items["Iron Ingot"], 3f,
                new[] { (items["Iron Ore"], 2) }, isStarter: false, unlockCost: 20, carbon: 12));

            blueprints.Add(CreateBlueprint("Gold Ingot", BlueprintType.Forge, items["Gold Ingot"], 4f,
                new[] { (items["Gold Ore"], 2) }, isStarter: false, unlockCost: 50, carbon: 10));

            blueprints.Add(CreateBlueprint("Bronze", BlueprintType.Forge, items["Bronze"], 3f,
                new[] { (items["Copper Ingot"], 1), (items["Tin Ingot"], 1) }, isStarter: false, unlockCost: 30, carbon: 10));

            blueprints.Add(CreateBlueprint("Steel", BlueprintType.Forge, items["Steel"], 5f,
                new[] { (items["Iron Ingot"], 2), (items["Coal"], 1) }, isStarter: false, unlockCost: 80, carbon: 18));

            // === KILN BLUEPRINTS (Heat Processing) ===
            blueprints.Add(CreateBlueprint("Bricks", BlueprintType.Kiln, items["Bricks"], 2f,
                new[] { (items["Clay"], 2) }, isStarter: true, unlockCost: 0, carbon: 6));

            blueprints.Add(CreateBlueprint("Glass", BlueprintType.Kiln, items["Glass"], 3f,
                new[] { (items["Sand"], 2) }, isStarter: false, unlockCost: 25, carbon: 8));

            blueprints.Add(CreateBlueprint("Glassware", BlueprintType.Kiln, items["Glassware"], 4f,
                new[] { (items["Glass"], 2) }, isStarter: false, unlockCost: 60, carbon: 6));

            blueprints.Add(CreateBlueprint("Pottery", BlueprintType.Kiln, items["Pottery"], 3f,
                new[] { (items["Bricks"], 2), (items["Glassware"], 1) }, isStarter: false, unlockCost: 100, carbon: 8));

            blueprints.Add(CreateBlueprint("Bread", BlueprintType.Kiln, items["Bread"], 3f,
                new[] { (items["Flour"], 2) }, isStarter: false, unlockCost: 30, carbon: 4));

            // === WORKSHOP BLUEPRINTS (Basic Crafting) ===
            blueprints.Add(CreateBlueprint("Cut Stone", BlueprintType.Workshop, items["Cut Stone"], 2f,
                new[] { (items["Stone"], 2) }, isStarter: true, unlockCost: 0, carbon: 2));

            blueprints.Add(CreateBlueprint("Planks", BlueprintType.Workshop, items["Planks"], 2f,
                new[] { (items["Wood"], 2) }, isStarter: true, unlockCost: 0, carbon: 2));

            blueprints.Add(CreateBlueprint("Linen Thread", BlueprintType.Workshop, items["Linen Thread"], 2f,
                new[] { (items["Flax"], 2) }, isStarter: true, unlockCost: 0, carbon: 1));

            blueprints.Add(CreateBlueprint("Papyrus Sheet", BlueprintType.Workshop, items["Papyrus Sheet"], 2f,
                new[] { (items["Papyrus"], 2) }, isStarter: true, unlockCost: 0, carbon: 1));

            blueprints.Add(CreateBlueprint("Rope", BlueprintType.Workshop, items["Rope"], 2f,
                new[] { (items["Flax"], 3) }, isStarter: false, unlockCost: 15, carbon: 1));

            blueprints.Add(CreateBlueprint("Linen Cloth", BlueprintType.Workshop, items["Linen Cloth"], 3f,
                new[] { (items["Linen Thread"], 2) }, isStarter: false, unlockCost: 25, carbon: 2));

            blueprints.Add(CreateBlueprint("Cement", BlueprintType.Workshop, items["Cement"], 3f,
                new[] { (items["Clay"], 1), (items["Stone"], 1), (items["Sand"], 1) }, isStarter: false, unlockCost: 40, carbon: 5));

            blueprints.Add(CreateBlueprint("Bronze Tools", BlueprintType.Workshop, items["Bronze Tools"], 4f,
                new[] { (items["Bronze"], 2), (items["Planks"], 1) }, isStarter: false, unlockCost: 60, carbon: 4));

            blueprints.Add(CreateBlueprint("Iron Tools", BlueprintType.Workshop, items["Iron Tools"], 4f,
                new[] { (items["Iron Ingot"], 2), (items["Planks"], 1) }, isStarter: false, unlockCost: 80, carbon: 5));

            blueprints.Add(CreateBlueprint("Stone Blocks", BlueprintType.Workshop, items["Stone Blocks"], 4f,
                new[] { (items["Cut Stone"], 2), (items["Cement"], 1) }, isStarter: false, unlockCost: 100, carbon: 4));

            blueprints.Add(CreateBlueprint("Scroll", BlueprintType.Workshop, items["Scroll"], 3f,
                new[] { (items["Papyrus Sheet"], 2), (items["Planks"], 1) }, isStarter: false, unlockCost: 50, carbon: 2));

            // === ARTISAN BLUEPRINTS (Complex Assembly) ===
            blueprints.Add(CreateBlueprint("Gold Jewelry", BlueprintType.Artisan, items["Gold Jewelry"], 5f,
                new[] { (items["Gold Ingot"], 1), (items["Glassware"], 1) }, isStarter: false, unlockCost: 150, carbon: 3));

            blueprints.Add(CreateBlueprint("Bronze Weapons", BlueprintType.Artisan, items["Bronze Weapons"], 5f,
                new[] { (items["Bronze Tools"], 2), (items["Leather"], 1) }, isStarter: false, unlockCost: 120, carbon: 6));

            blueprints.Add(CreateBlueprint("Iron Weapons", BlueprintType.Artisan, items["Iron Weapons"], 6f,
                new[] { (items["Iron Tools"], 2), (items["Leather"], 1) }, isStarter: false, unlockCost: 180, carbon: 8));

            blueprints.Add(CreateBlueprint("Bronze Armor", BlueprintType.Artisan, items["Bronze Armor"], 6f,
                new[] { (items["Bronze"], 3), (items["Leather"], 2) }, isStarter: false, unlockCost: 160, carbon: 8));

            blueprints.Add(CreateBlueprint("Furniture", BlueprintType.Artisan, items["Furniture"], 4f,
                new[] { (items["Planks"], 2), (items["Linen Cloth"], 1) }, isStarter: false, unlockCost: 100, carbon: 3));

            blueprints.Add(CreateBlueprint("Mosaic", BlueprintType.Artisan, items["Mosaic"], 5f,
                new[] { (items["Glass"], 2), (items["Stone"], 2) }, isStarter: false, unlockCost: 140, carbon: 4));

            blueprints.Add(CreateBlueprint("Chariot Frame", BlueprintType.Artisan, items["Chariot Frame"], 8f,
                new[] { (items["Planks"], 4), (items["Bronze"], 2), (items["Rope"], 2) }, isStarter: false, unlockCost: 250, carbon: 10));

            blueprints.Add(CreateBlueprint("Statue Base", BlueprintType.Artisan, items["Statue Base"], 8f,
                new[] { (items["Stone Blocks"], 4), (items["Bronze"], 2) }, isStarter: false, unlockCost: 220, carbon: 8));

            blueprints.Add(CreateBlueprint("Temple Column", BlueprintType.Artisan, items["Temple Column"], 7f,
                new[] { (items["Stone Blocks"], 4), (items["Cement"], 2) }, isStarter: false, unlockCost: 200, carbon: 8));

            blueprints.Add(CreateBlueprint("War Engine Parts", BlueprintType.Artisan, items["War Engine Parts"], 10f,
                new[] { (items["Iron Tools"], 4), (items["Rope"], 4) }, isStarter: false, unlockCost: 300, carbon: 12));

            // === MASTERWORK BLUEPRINTS (Final Products) ===
            blueprints.Add(CreateBlueprint("Chariot", BlueprintType.Artisan, items["Chariot"], 12f,
                new[] { (items["Chariot Frame"], 1), (items["Bronze"], 2), (items["Leather"], 2) }, isStarter: false, unlockCost: 500, carbon: 15));

            blueprints.Add(CreateBlueprint("Bronze Statue", BlueprintType.Artisan, items["Bronze Statue"], 12f,
                new[] { (items["Statue Base"], 1), (items["Bronze"], 4) }, isStarter: false, unlockCost: 450, carbon: 12));

            blueprints.Add(CreateBlueprint("Temple Component", BlueprintType.Artisan, items["Temple Component"], 10f,
                new[] { (items["Temple Column"], 2), (items["Mosaic"], 1) }, isStarter: false, unlockCost: 400, carbon: 10));

            blueprints.Add(CreateBlueprint("War Catapult", BlueprintType.Artisan, items["War Catapult"], 15f,
                new[] { (items["War Engine Parts"], 1), (items["Rope"], 4), (items["Planks"], 4) }, isStarter: false, unlockCost: 600, carbon: 18));

            blueprints.Add(CreateBlueprint("Royal Throne", BlueprintType.Artisan, items["Royal Throne"], 15f,
                new[] { (items["Furniture"], 2), (items["Gold Jewelry"], 2), (items["Linen Cloth"], 2) }, isStarter: false, unlockCost: 800, carbon: 8));

            // === KITCHEN BLUEPRINTS (Food Processing) ===
            blueprints.Add(CreateBlueprint("Flour", BlueprintType.Kitchen, items["Flour"], 2f,
                new[] { (items["Grain"], 2) }, isStarter: true, unlockCost: 0, carbon: 1));

            blueprints.Add(CreateBlueprint("Wine", BlueprintType.Kitchen, items["Wine"], 4f,
                new[] { (items["Grapes"], 3) }, isStarter: false, unlockCost: 40, carbon: 2));

            blueprints.Add(CreateBlueprint("Olive Oil", BlueprintType.Kitchen, items["Olive Oil"], 3f,
                new[] { (items["Olives"], 3) }, isStarter: false, unlockCost: 35, carbon: 2));

            blueprints.Add(CreateBlueprint("Meat", BlueprintType.Kitchen, items["Meat"], 4f,
                new[] { (items["Cattle"], 1) }, isStarter: false, unlockCost: 50, carbon: 3));

            blueprints.Add(CreateBlueprint("Leather", BlueprintType.Kitchen, items["Leather"], 3f,
                new[] { (items["Cattle"], 1) }, isStarter: false, unlockCost: 45, carbon: 3));

            blueprints.Add(CreateBlueprint("Feast", BlueprintType.Kitchen, items["Feast"], 8f,
                new[] { (items["Bread"], 1), (items["Wine"], 1), (items["Meat"], 1) }, isStarter: false, unlockCost: 150, carbon: 5));

            return blueprints;
        }

        private static BlueprintDefinition CreateBlueprint(string name, BlueprintType type, ItemDefinition outputItem, float time, (ItemDefinition item, int amount)[] inputs, bool isStarter, int unlockCost, int carbon)
        {
            var bp = ScriptableObject.CreateInstance<BlueprintDefinition>();
            bp.name = $"BP_{name.Replace(" ", "")}";

            var so = new SerializedObject(bp);
            so.FindProperty("blueprintName").stringValue = name;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("productionTime").floatValue = time;
            so.FindProperty("isStarterCard").boolValue = isStarter;
            so.FindProperty("unlockCost").intValue = unlockCost;
            so.FindProperty("carbonEmission").intValue = carbon;

            // Output
            var outputProp = so.FindProperty("output");
            outputProp.FindPropertyRelative("item").objectReferenceValue = outputItem;
            outputProp.FindPropertyRelative("amount").intValue = 1;

            // Inputs
            var inputsProp = so.FindProperty("inputs");
            inputsProp.ClearArray();
            for (int i = 0; i < inputs.Length; i++)
            {
                inputsProp.InsertArrayElementAtIndex(i);
                var inputStack = inputsProp.GetArrayElementAtIndex(i);
                inputStack.FindPropertyRelative("item").objectReferenceValue = inputs[i].item;
                inputStack.FindPropertyRelative("amount").intValue = inputs[i].amount;
            }

            // Input/Output Counts for connectors
            so.FindProperty("inputCount").intValue = inputs.Length;
            so.FindProperty("outputCount").intValue = 1;

            so.ApplyModifiedProperties();

            string folder = "Assets/Blueprints";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // Organize into subfolders based on type
            string subFolder = $"{folder}/{type}";
            if (!Directory.Exists(subFolder)) Directory.CreateDirectory(subFolder);

            string path = $"{subFolder}/{bp.name}.asset";
            AssetDatabase.CreateAsset(bp, path);

            return bp;
        }

        private static void GenerateTechTree(List<BlueprintDefinition> blueprints)
        {
            var techTree = AssetDatabase.LoadAssetAtPath<TechTreeGraph>("Assets/TechTreeGraph.asset");
            if (techTree == null)
            {
                Debug.LogError("Could not find TechTreeGraph asset at Assets/TechTreeGraph.asset");
                return;
            }

            techTree.Clear();
            var so = new SerializedObject(techTree);
            var nodesProp = so.FindProperty("nodes");
            nodesProp.ClearArray();

            // Filter for unlockable blueprints
            var unlockableBlueprints = blueprints.Where(b => !b.IsStarterCard).ToList();

            // Group by Output Item Tier
            var tierGroups = unlockableBlueprints
                .GroupBy(b => b.GetOutputTier())
                .OrderBy(g => g.Key);

            float startX = 0;
            float startY = 0;
            float spacingX = 400; // Increased spacing for cards
            float spacingY = 250;

            foreach (var group in tierGroups)
            {
                int tier = group.Key;
                int count = 0;
                float currentY = startY;

                foreach (var bp in group)
                {
                    nodesProp.InsertArrayElementAtIndex(nodesProp.arraySize);
                    var nodeProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);

                    // Initialize new node
                    var node = new TechTreeNodeData();
                    node.guid = System.Guid.NewGuid().ToString();
                    node.position = new Vector2(startX + (tier * spacingX), currentY);

                    // Set properties via SerializedProperty to ensure saving
                    nodeProp.FindPropertyRelative("guid").stringValue = node.guid;
                    nodeProp.FindPropertyRelative("position").vector2Value = node.position;
                    nodeProp.FindPropertyRelative("blueprint").objectReferenceValue = bp;

                    currentY += spacingY;
                    count++;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(techTree);
            Debug.Log($"Generated Tech Tree with {nodesProp.arraySize} nodes.");
        }
    }
}
