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
        [MenuItem("Carbon World/Generate Game Data")]
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
            GenerateBlueprints(items);
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

            // --- Industrial T0 (Raw) ---
            CreateItem(items, "Iron Ore", ItemTier.Raw, 1);
            CreateItem(items, "Copper Ore", ItemTier.Raw, 1);
            CreateItem(items, "Coal", ItemTier.Raw, 1);
            CreateItem(items, "Oil", ItemTier.Raw, 1);
            CreateItem(items, "Lithium Ore", ItemTier.Raw, 1);
            CreateItem(items, "Stone", ItemTier.Raw, 1);
            CreateItem(items, "Sand", ItemTier.Raw, 1);
            CreateItem(items, "Water", ItemTier.Raw, 1); 

            // --- Industrial T1 (Basic) ---
            CreateItem(items, "Iron Ingot", ItemTier.Basic, 2);
            CreateItem(items, "Copper Ingot", ItemTier.Basic, 2);
            CreateItem(items, "Steel Ingot", ItemTier.Basic, 4); // Higher value
            CreateItem(items, "Lithium Ingot", ItemTier.Basic, 4);
            CreateItem(items, "Concrete", ItemTier.Basic, 2);

            // --- Industrial T2 (Processed) ---
            CreateItem(items, "Iron Plate", ItemTier.Processed, 5);
            CreateItem(items, "Steel Plate", ItemTier.Processed, 8);
            CreateItem(items, "Copper Wire", ItemTier.Processed, 5);
            CreateItem(items, "Plastic", ItemTier.Processed, 8);

            // --- Industrial T3 (Component) ---
            CreateItem(items, "Gear", ItemTier.Component, 12);
            CreateItem(items, "Carbon Fiber", ItemTier.Component, 15);

            // --- Industrial T4 (Advanced) ---
            CreateItem(items, "Circuit", ItemTier.Advanced, 25);
            CreateItem(items, "Motor", ItemTier.Advanced, 25);
            CreateItem(items, "Battery", ItemTier.Advanced, 30);
            CreateItem(items, "Reinforced Concrete", ItemTier.Advanced, 20);

            // --- Industrial T5 (Assembly) ---
            CreateItem(items, "Engine", ItemTier.Assembly, 60);
            CreateItem(items, "Computer Unit", ItemTier.Assembly, 60);

            // --- Industrial T6 (Final) ---
            CreateItem(items, "Automated Factory", ItemTier.FinalProduct, 200);
            CreateItem(items, "Quantum Processor", ItemTier.FinalProduct, 500);

            // --- Food Chain ---
            CreateItem(items, "Wheat", ItemTier.Raw, 1, isFood: true, nutrition: 1);
            CreateItem(items, "Flour", ItemTier.Basic, 2, isFood: true, nutrition: 2);
            CreateItem(items, "Dough", ItemTier.Processed, 5, isFood: true, nutrition: 4);
            CreateItem(items, "Bread", ItemTier.FinalProduct, 15, isFood: true, nutrition: 10);

            CreateItem(items, "Apple", ItemTier.Raw, 1, isFood: true, nutrition: 2);
            CreateItem(items, "Apple Juice", ItemTier.Basic, 2, isFood: true, nutrition: 5);
            CreateItem(items, "Apple Pie", ItemTier.FinalProduct, 15, isFood: true, nutrition: 15);

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

        private static void GenerateBlueprints(Dictionary<string, ItemDefinition> items)
        {
            // Blueprint Params: Name, Type, Output, Time, Inputs, IsStarter, UnlockCost, CarbonEmission

            // --- Starter Tier (T0/T1 Basics) ---
            // Carbon: Smelters=10, Constructors=2
            
            CreateBlueprint("Iron Ingot", BlueprintType.Smelter, items["Iron Ingot"], 2f, 
                new[] { (items["Iron Ore"], 1) }, isStarter: true, unlockCost: 0, carbon: 10);
            
            CreateBlueprint("Copper Ingot", BlueprintType.Smelter, items["Copper Ingot"], 2f, 
                new[] { (items["Copper Ore"], 1) }, isStarter: true, unlockCost: 0, carbon: 10);

            CreateBlueprint("Concrete", BlueprintType.Constructor, items["Concrete"], 2f, 
                new[] { (items["Stone"], 1), (items["Sand"], 1), (items["Water"], 1) }, isStarter: true, unlockCost: 0, carbon: 2);
            
            // Basic Construction (Starter)
            CreateBlueprint("Iron Plate", BlueprintType.Constructor, items["Iron Plate"], 2f,
                new[] { (items["Iron Ingot"], 1) }, isStarter: true, unlockCost: 0, carbon: 2);

            CreateBlueprint("Copper Wire", BlueprintType.Constructor, items["Copper Wire"], 1f,
                new[] { (items["Copper Ingot"], 1) }, isStarter: true, unlockCost: 0, carbon: 2);
            
            // --- Unlock Tier 1 (Steel, Plastic, Basic Food) ---
            // Cost: ~10-20 points
            
            CreateBlueprint("Steel Ingot", BlueprintType.Smelter, items["Steel Ingot"], 4f, 
                new[] { (items["Iron Ore"], 1), (items["Coal"], 1) }, isStarter: false, unlockCost: 20, carbon: 15);
            
            CreateBlueprint("Steel Plate", BlueprintType.Constructor, items["Steel Plate"], 3f,
                new[] { (items["Steel Ingot"], 1) }, isStarter: false, unlockCost: 20, carbon: 2);

            CreateBlueprint("Plastic", BlueprintType.Constructor, items["Plastic"], 3f,
                new[] { (items["Oil"], 1) }, isStarter: false, unlockCost: 25, carbon: 5); // Oil processing is dirty?

            CreateBlueprint("Lithium Ingot", BlueprintType.Smelter, items["Lithium Ingot"], 3f, 
                new[] { (items["Lithium Ore"], 1) }, isStarter: false, unlockCost: 25, carbon: 10);
            
            // --- Unlock Tier 2 (Components) ---
            // Cost: ~50 points

            CreateBlueprint("Gear", BlueprintType.Constructor, items["Gear"], 2f,
                new[] { (items["Iron Plate"], 1), (items["Iron Ingot"], 1) }, isStarter: false, unlockCost: 50, carbon: 2);

            CreateBlueprint("Carbon Fiber", BlueprintType.Assembler, items["Carbon Fiber"], 4f,
                new[] { (items["Coal"], 2), (items["Plastic"], 1) }, isStarter: false, unlockCost: 80, carbon: 8);

            // --- Unlock Tier 3 (Advanced) ---
            // Cost: ~150 points

            CreateBlueprint("Circuit", BlueprintType.Assembler, items["Circuit"], 5f,
                new[] { (items["Copper Wire"], 2), (items["Iron Plate"], 1), (items["Plastic"], 1) }, isStarter: false, unlockCost: 150, carbon: 5);

            CreateBlueprint("Motor", BlueprintType.Assembler, items["Motor"], 5f,
                new[] { (items["Gear"], 1), (items["Copper Wire"], 2), (items["Steel Plate"], 1) }, isStarter: false, unlockCost: 150, carbon: 5);

            CreateBlueprint("Battery", BlueprintType.Assembler, items["Battery"], 5f,
                new[] { (items["Lithium Ingot"], 2), (items["Plastic"], 1), (items["Copper Wire"], 1) }, isStarter: false, unlockCost: 200, carbon: 8);

            CreateBlueprint("Reinforced Concrete", BlueprintType.Assembler, items["Reinforced Concrete"], 4f,
                new[] { (items["Concrete"], 2), (items["Steel Plate"], 1) }, isStarter: false, unlockCost: 100, carbon: 5);

            // --- Unlock Tier 4 (Assembly) ---
            // Cost: ~500 points
            
            CreateBlueprint("Engine", BlueprintType.Assembler, items["Engine"], 8f,
                new[] { (items["Motor"], 1), (items["Steel Plate"], 2), (items["Circuit"], 1) }, isStarter: false, unlockCost: 500, carbon: 10);
            
            CreateBlueprint("Computer Unit", BlueprintType.Assembler, items["Computer Unit"], 8f,
                new[] { (items["Circuit"], 2), (items["Battery"], 1), (items["Plastic"], 2) }, isStarter: false, unlockCost: 600, carbon: 8);

            // --- Unlock Tier 5 (End Game) ---
            // Cost: ~2000 points

            CreateBlueprint("Automated Factory", BlueprintType.Assembler, items["Automated Factory"], 15f,
                new[] { (items["Engine"], 2), (items["Computer Unit"], 2), (items["Reinforced Concrete"], 4) }, isStarter: false, unlockCost: 2000, carbon: 20);

            CreateBlueprint("Quantum Processor", BlueprintType.Assembler, items["Quantum Processor"], 20f,
                new[] { (items["Computer Unit"], 2), (items["Battery"], 4), (items["Carbon Fiber"], 4) }, isStarter: false, unlockCost: 5000, carbon: 25);


            // --- Food Chains (Unlockable) ---
            // Maybe basic flour is starter or cheap unlock? Let's make flour starter to encourage food early, but processing unlockable.
            
            CreateBlueprint("Flour", BlueprintType.FoodProcessor, items["Flour"], 2f,
                new[] { (items["Wheat"], 1) }, isStarter: true, unlockCost: 0, carbon: 1);
                
            CreateBlueprint("Dough", BlueprintType.FoodProcessor, items["Dough"], 2f,
                new[] { (items["Flour"], 1), (items["Water"], 1) }, isStarter: false, unlockCost: 15, carbon: 1);
                
            CreateBlueprint("Bread", BlueprintType.Furnace, items["Bread"], 3f,
                new[] { (items["Dough"], 1), (items["Coal"], 1) }, isStarter: false, unlockCost: 30, carbon: 5);

            CreateBlueprint("Apple Juice", BlueprintType.FoodProcessor, items["Apple Juice"], 2f,
                new[] { (items["Apple"], 2) }, isStarter: false, unlockCost: 15, carbon: 1);
                
            CreateBlueprint("Apple Pie", BlueprintType.FoodProcessor, items["Apple Pie"], 5f,
                new[] { (items["Apple"], 2), (items["Flour"], 1), (items["Water"], 1) }, isStarter: false, unlockCost: 40, carbon: 2);
        }

        private static void CreateBlueprint(string name, BlueprintType type, ItemDefinition outputItem, float time, (ItemDefinition item, int amount)[] inputs, bool isStarter, int unlockCost, int carbon)
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
            outputProp.FindPropertyRelative("amount").intValue = 1; // Default 1 output
            
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
        }
    }
}
