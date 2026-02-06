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

            // === T0 HARVEST (Raw Materials) ===
            CreateItem(items, "Copper Ore", ItemTier.Harvest, 1, "Raw copper ore extracted from the earth. Essential for bronze production.");
            CreateItem(items, "Tin Ore", ItemTier.Harvest, 1, "Silvery metal ore found in riverbeds. alloyed with copper to make bronze.");
            CreateItem(items, "Iron Ore", ItemTier.Harvest, 1, "Heavy, dark ore. Requires intense heat to smelt into usable iron.");
            CreateItem(items, "Gold Ore", ItemTier.Harvest, 2, "Precious yellow metal. Used for jewelry and ornamentation.");
            CreateItem(items, "Clay", ItemTier.Harvest, 1, "Malleable earth found near riverbanks. Fired into bricks or pottery.");
            CreateItem(items, "Stone", ItemTier.Harvest, 1, "Rough quarried stone. The foundation of all great structures.");
            CreateItem(items, "Sand", ItemTier.Harvest, 1, "Fine silica grains. Can be melted into glass.");
            CreateItem(items, "Wood", ItemTier.Harvest, 1, "Timber felled from local forests. Used for fuel and basic construction.");
            CreateItem(items, "Flax", ItemTier.Harvest, 1, "Fibrous plant harvested from the fields. Spun into thread.");
            CreateItem(items, "Papyrus", ItemTier.Harvest, 1, "Tall reeds from the marshlands. Pressed into writing material.");
            CreateItem(items, "Coal", ItemTier.Harvest, 1, "Combustible sedimentary rock. Burns hot enough to smelt steel.");

            // === T1 REFINED (Basic Processing) ===
            CreateItem(items, "Copper Ingot", ItemTier.Refined, 2, "Smelted copper bar. Soft and conductive.");
            CreateItem(items, "Tin Ingot", ItemTier.Refined, 2, "Refined tin bar. Too soft for tools, but perfect for alloys.");
            CreateItem(items, "Iron Ingot", ItemTier.Refined, 3, "Standard iron bar. Strong and durable.");
            CreateItem(items, "Gold Ingot", ItemTier.Refined, 5, "Refined gold bar. A sign of wealth and power.");
            CreateItem(items, "Bricks", ItemTier.Refined, 2, "Fired clay blocks. Uniform and stackable for construction.");
            CreateItem(items, "Cut Stone", ItemTier.Refined, 2, "Stone hewn into regular shapes for precise building.");
            CreateItem(items, "Glass", ItemTier.Refined, 3, "Clear material made from melted sand. Fragile but useful.");
            CreateItem(items, "Planks", ItemTier.Refined, 2, "Wood sawn into flat boards. Basic building material.");
            CreateItem(items, "Linen Thread", ItemTier.Refined, 2, "Fine thread spun from flax fibers.");
            CreateItem(items, "Papyrus Sheet", ItemTier.Refined, 2, "Dried and pressed papyrus, ready for writing.");

            // === T2 CRAFTED (Secondary Processing) ===
            CreateItem(items, "Bronze", ItemTier.Crafted, 4, "An alloy of copper and tin. Harder than its components.");
            CreateItem(items, "Steel", ItemTier.Crafted, 5, "Iron infused with carbon. Superior strength and edge retention.");
            CreateItem(items, "Cement", ItemTier.Crafted, 4, "A binding powder made from crushed stone and clay.");
            CreateItem(items, "Linen Cloth", ItemTier.Crafted, 4, "Woven fabric. Light, breathable, and versatile.");
            CreateItem(items, "Rope", ItemTier.Crafted, 3, "Twisted flax fibers. Strong enough to haul heavy stones.");
            CreateItem(items, "Glassware", ItemTier.Crafted, 5, "Bottles, vases, and vessels blown from molten glass.");

            // === T3 ARTISAN (Skilled Craftwork) ===
            CreateItem(items, "Bronze Tools", ItemTier.Artisan, 8, "Durable tools for farming and crafting.");
            CreateItem(items, "Iron Tools", ItemTier.Artisan, 10, "Heavy-duty tools for mining and construction.");
            CreateItem(items, "Pottery", ItemTier.Artisan, 8, "Ceramic vessels for storing grain, water, and wine.");
            CreateItem(items, "Stone Blocks", ItemTier.Artisan, 8, "Large, perfectly fitted stones for monumental architecture.");
            CreateItem(items, "Gold Jewelry", ItemTier.Artisan, 15, "Ornate necklaces and rings worn by the elite.");
            CreateItem(items, "Leather", ItemTier.Artisan, 6, "Tanned animal hide. Tough and flexible.");
            CreateItem(items, "Scroll", ItemTier.Artisan, 7, "A roll of papyrus containing knowledge, laws, or inventory.");

            // === T4 FINE (High-Quality Goods) ===
            CreateItem(items, "Bronze Weapons", ItemTier.Fine, 15, "Swords and spears cast from bronze. Reliable in battle.");
            CreateItem(items, "Iron Weapons", ItemTier.Fine, 20, "Forged iron blades. Sharper and stronger than bronze.");
            CreateItem(items, "Bronze Armor", ItemTier.Fine, 18, "Plated armor offering protection against improved weaponry.");
            CreateItem(items, "Furniture", ItemTier.Fine, 12, "Finely crafted tables and chairs for comfort.");
            CreateItem(items, "Mosaic", ItemTier.Fine, 16, "Intricate patterns made from colored glass and stone.");

            // === T5 GRAND (Large Constructions) ===
            CreateItem(items, "Chariot Frame", ItemTier.Grand, 30, "The sturdy wooden chassis of a war chariot.");
            CreateItem(items, "Statue Base", ItemTier.Grand, 28, "A massive stone pedestal to support a monument.");
            CreateItem(items, "Temple Column", ItemTier.Grand, 25, "A fluted column to hold up the roofs of the gods.");
            CreateItem(items, "War Engine Parts", ItemTier.Grand, 35, "Complex mechanical components for siege weaponry.");

            // === T6 MASTERWORK (Ultimate Achievements) ===
            CreateItem(items, "Chariot", ItemTier.Masterwork, 80, "A swift horse-drawn vehicle for war and racing.");
            CreateItem(items, "Bronze Statue", ItemTier.Masterwork, 70, "A colossal figure cast in enduring bronze.");
            CreateItem(items, "Temple Component", ItemTier.Masterwork, 60, "Prefabricated sections of a grand temple.");
            CreateItem(items, "War Catapult", ItemTier.Masterwork, 90, "A devastating siege engine capable of hurling massive stones.");
            CreateItem(items, "Royal Throne", ItemTier.Masterwork, 100, "A seat of gold and fine cloth, fit for a pharaoh or emperor.");

            // === FOOD CHAIN ===
            CreateItem(items, "Grain", ItemTier.Harvest, 1, "Staple crop. The basis of all civilization.", isFood: true, nutrition: 1);
            CreateItem(items, "Grapes", ItemTier.Harvest, 1, "Sweet fruit grown in vineyards.", isFood: true, nutrition: 1);
            CreateItem(items, "Olives", ItemTier.Harvest, 1, "Oily fruit from the sacred tree.", isFood: true, nutrition: 1);
            CreateItem(items, "Cattle", ItemTier.Harvest, 2, "Livestock raised for meat and leather.", isFood: true, nutrition: 2);

            CreateItem(items, "Flour", ItemTier.Refined, 2, "Ground grain, ready for baking.", isFood: true, nutrition: 2);
            CreateItem(items, "Bread", ItemTier.Crafted, 4, "Baked dough. The daily meal of the people.", isFood: true, nutrition: 5);
            CreateItem(items, "Wine", ItemTier.Crafted, 5, "Fermented grape juice. Enjoyed by all social classes.", isFood: true, nutrition: 4);
            CreateItem(items, "Olive Oil", ItemTier.Crafted, 4, "Pressed oil. Used for cooking and lighting.", isFood: true, nutrition: 3);
            CreateItem(items, "Meat", ItemTier.Refined, 4, "Cut of fresh meat.", isFood: true, nutrition: 6);
            CreateItem(items, "Feast", ItemTier.Fine, 15, "A lavish spread of bread, wine, and meat.", isFood: true, nutrition: 20);

            return items;
        }

        private static void CreateItem(Dictionary<string, ItemDefinition> dict, string name, ItemTier tier, int techPoints, string description, bool isFood = false, int nutrition = 0)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = name.Replace(" ", "");

            var so = new SerializedObject(item);
            so.FindProperty("itemName").stringValue = name;
            so.FindProperty("description").stringValue = description;
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
            // SacredEarth: Mining/extraction from the earth angers earth gods
            blueprints.Add(CreateBlueprint("Copper Ingot", BlueprintType.Forge, items["Copper Ingot"], 2f,
                new[] { (items["Copper Ore"], 2) }, isStarter: true, unlockCost: 0, displeasure: 8, source: DispleasureSource.SacredEarth,
                "Smelts raw copper ore into usable ingots."));

            blueprints.Add(CreateBlueprint("Tin Ingot", BlueprintType.Forge, items["Tin Ingot"], 2f,
                new[] { (items["Tin Ore"], 2) }, isStarter: true, unlockCost: 0, displeasure: 8, source: DispleasureSource.SacredEarth,
                "Refines tin ore. Essential for bronze production."));

            // SlaveLabor: Iron requires backbreaking labor
            blueprints.Add(CreateBlueprint("Iron Ingot", BlueprintType.Forge, items["Iron Ingot"], 3f,
                new[] { (items["Iron Ore"], 2) }, isStarter: false, unlockCost: 20, displeasure: 12, source: DispleasureSource.SlaveLabor,
                "Requires high heat to smelt iron ore into strong ingots."));

            blueprints.Add(CreateBlueprint("Gold Ingot", BlueprintType.Forge, items["Gold Ingot"], 4f,
                new[] { (items["Gold Ore"], 2) }, isStarter: false, unlockCost: 50, displeasure: 10, source: DispleasureSource.SacredEarth,
                "Purifies gold ore into valuable bullion."));

            blueprints.Add(CreateBlueprint("Bronze", BlueprintType.Forge, items["Bronze"], 3f,
                new[] { (items["Copper Ingot"], 1), (items["Tin Ingot"], 1) }, isStarter: false, unlockCost: 30, displeasure: 10, source: DispleasureSource.Craftsmanship,
                "Alloys copper and tin to create Bronze, the metal of the age."));

            // SlaveLabor: Steel production is extremely demanding
            blueprints.Add(CreateBlueprint("Steel", BlueprintType.Forge, items["Steel"], 5f,
                new[] { (items["Iron Ingot"], 2), (items["Coal"], 1) }, isStarter: false, unlockCost: 80, displeasure: 18, source: DispleasureSource.SlaveLabor,
                "Advanced metallurgy combining iron and carbon for superior hardness."));

            // === KILN BLUEPRINTS (Heat Processing) ===
            // SacredEarth: Clay extraction from the earth
            blueprints.Add(CreateBlueprint("Bricks", BlueprintType.Kiln, items["Bricks"], 2f,
                new[] { (items["Clay"], 2) }, isStarter: true, unlockCost: 0, displeasure: 6, source: DispleasureSource.SacredEarth,
                "Fires clay into hard, durable bricks."));

            blueprints.Add(CreateBlueprint("Glass", BlueprintType.Kiln, items["Glass"], 3f,
                new[] { (items["Sand"], 2) }, isStarter: false, unlockCost: 25, displeasure: 8, source: DispleasureSource.Craftsmanship,
                "Melts sand into clear glass."));

            blueprints.Add(CreateBlueprint("Glassware", BlueprintType.Kiln, items["Glassware"], 4f,
                new[] { (items["Glass"], 2) }, isStarter: false, unlockCost: 60, displeasure: 6, source: DispleasureSource.Craftsmanship,
                "Shapes molten glass into intricate vessels."));

            blueprints.Add(CreateBlueprint("Pottery", BlueprintType.Kiln, items["Pottery"], 3f,
                new[] { (items["Bricks"], 2), (items["Glassware"], 1) }, isStarter: false, unlockCost: 100, displeasure: 8, source: DispleasureSource.Craftsmanship,
                "Creates fine ceramics for storage and trade."));

            blueprints.Add(CreateBlueprint("Bread", BlueprintType.Kiln, items["Bread"], 3f,
                new[] { (items["Flour"], 2) }, isStarter: false, unlockCost: 30, displeasure: 4, source: DispleasureSource.Craftsmanship,
                "Bakes dough into nourishing bread."));

            // === WORKSHOP BLUEPRINTS (Basic Crafting) ===
            // SacredEarth: Stone quarrying disturbs the earth
            blueprints.Add(CreateBlueprint("Cut Stone", BlueprintType.Workshop, items["Cut Stone"], 2f,
                new[] { (items["Stone"], 2) }, isStarter: true, unlockCost: 0, displeasure: 2, source: DispleasureSource.SacredEarth,
                "Shapes raw stone into building blocks."));

            blueprints.Add(CreateBlueprint("Planks", BlueprintType.Workshop, items["Planks"], 2f,
                new[] { (items["Wood"], 2) }, isStarter: true, unlockCost: 0, displeasure: 2, source: DispleasureSource.Craftsmanship,
                "Saws logs into versatile wooden planks."));

            blueprints.Add(CreateBlueprint("Linen Thread", BlueprintType.Workshop, items["Linen Thread"], 2f,
                new[] { (items["Flax"], 2) }, isStarter: true, unlockCost: 0, displeasure: 1, source: DispleasureSource.Craftsmanship,
                "Spins flax fibers into strong thread."));

            blueprints.Add(CreateBlueprint("Papyrus Sheet", BlueprintType.Workshop, items["Papyrus Sheet"], 2f,
                new[] { (items["Papyrus"], 2) }, isStarter: true, unlockCost: 0, displeasure: 1, source: DispleasureSource.Craftsmanship,
                "Processes reeds into writing material."));

            blueprints.Add(CreateBlueprint("Rope", BlueprintType.Workshop, items["Rope"], 2f,
                new[] { (items["Flax"], 3) }, isStarter: false, unlockCost: 15, displeasure: 1, source: DispleasureSource.Craftsmanship,
                "Twists fibers into heavy-duty rope."));

            blueprints.Add(CreateBlueprint("Linen Cloth", BlueprintType.Workshop, items["Linen Cloth"], 3f,
                new[] { (items["Linen Thread"], 2) }, isStarter: false, unlockCost: 25, displeasure: 2, source: DispleasureSource.Craftsmanship,
                "Weaves thread into fine cloth."));

            blueprints.Add(CreateBlueprint("Cement", BlueprintType.Workshop, items["Cement"], 3f,
                new[] { (items["Clay"], 1), (items["Stone"], 1), (items["Sand"], 1) }, isStarter: false, unlockCost: 40, displeasure: 5, source: DispleasureSource.SacredEarth,
                "Mixes ingredients to create a strong binding agent."));

            blueprints.Add(CreateBlueprint("Bronze Tools", BlueprintType.Workshop, items["Bronze Tools"], 4f,
                new[] { (items["Bronze"], 2), (items["Planks"], 1) }, isStarter: false, unlockCost: 60, displeasure: 4, source: DispleasureSource.Craftsmanship,
                "Fashions bronze and wood into reliable tools."));

            blueprints.Add(CreateBlueprint("Iron Tools", BlueprintType.Workshop, items["Iron Tools"], 4f,
                new[] { (items["Iron Ingot"], 2), (items["Planks"], 1) }, isStarter: false, unlockCost: 80, displeasure: 5, source: DispleasureSource.Craftsmanship,
                "Creates superior tools from iron."));

            // SlaveLabor: Heavy stone blocks require brutal labor
            blueprints.Add(CreateBlueprint("Stone Blocks", BlueprintType.Workshop, items["Stone Blocks"], 4f,
                new[] { (items["Cut Stone"], 2), (items["Cement"], 1) }, isStarter: false, unlockCost: 100, displeasure: 4, source: DispleasureSource.SlaveLabor,
                "Assembles massive masonry blocks."));

            blueprints.Add(CreateBlueprint("Scroll", BlueprintType.Workshop, items["Scroll"], 3f,
                new[] { (items["Papyrus Sheet"], 2), (items["Planks"], 1) }, isStarter: false, unlockCost: 50, displeasure: 2, source: DispleasureSource.Craftsmanship,
                "Prepares finished scrolls for the library."));

            // === ARTISAN BLUEPRINTS (Complex Assembly) ===
            blueprints.Add(CreateBlueprint("Gold Jewelry", BlueprintType.Artisan, items["Gold Jewelry"], 5f,
                new[] { (items["Gold Ingot"], 1), (items["Glassware"], 1) }, isStarter: false, unlockCost: 150, displeasure: 3, source: DispleasureSource.Craftsmanship,
                "Crafts exquisite jewelry for the nobility."));

            blueprints.Add(CreateBlueprint("Bronze Weapons", BlueprintType.Artisan, items["Bronze Weapons"], 5f,
                new[] { (items["Bronze Tools"], 2), (items["Leather"], 1) }, isStarter: false, unlockCost: 120, displeasure: 6, source: DispleasureSource.Craftsmanship,
                "Smiths weapons of war from bronze."));

            blueprints.Add(CreateBlueprint("Iron Weapons", BlueprintType.Artisan, items["Iron Weapons"], 6f,
                new[] { (items["Iron Tools"], 2), (items["Leather"], 1) }, isStarter: false, unlockCost: 180, displeasure: 8, source: DispleasureSource.Craftsmanship,
                "Forges deadly iron weaponry."));

            blueprints.Add(CreateBlueprint("Bronze Armor", BlueprintType.Artisan, items["Bronze Armor"], 6f,
                new[] { (items["Bronze"], 3), (items["Leather"], 2) }, isStarter: false, unlockCost: 160, displeasure: 8, source: DispleasureSource.Craftsmanship,
                "Beats bronze plates into protective armor."));

            blueprints.Add(CreateBlueprint("Furniture", BlueprintType.Artisan, items["Furniture"], 4f,
                new[] { (items["Planks"], 2), (items["Linen Cloth"], 1) }, isStarter: false, unlockCost: 100, displeasure: 3, source: DispleasureSource.Craftsmanship,
                "Assembles comfortable and stylish furniture."));

            blueprints.Add(CreateBlueprint("Mosaic", BlueprintType.Artisan, items["Mosaic"], 5f,
                new[] { (items["Glass"], 2), (items["Stone"], 2) }, isStarter: false, unlockCost: 140, displeasure: 4, source: DispleasureSource.Craftsmanship,
                "Lays tile and glass into beautiful artworks."));

            blueprints.Add(CreateBlueprint("Chariot Frame", BlueprintType.Artisan, items["Chariot Frame"], 8f,
                new[] { (items["Planks"], 4), (items["Bronze"], 2), (items["Rope"], 2) }, isStarter: false, unlockCost: 250, displeasure: 10, source: DispleasureSource.Craftsmanship,
                "Builds the chassis for a chariot."));

            blueprints.Add(CreateBlueprint("Statue Base", BlueprintType.Artisan, items["Statue Base"], 8f,
                new[] { (items["Stone Blocks"], 4), (items["Bronze"], 2) }, isStarter: false, unlockCost: 220, displeasure: 8, source: DispleasureSource.SlaveLabor,
                "Prepares the foundation for a great statue."));

            blueprints.Add(CreateBlueprint("Temple Column", BlueprintType.Artisan, items["Temple Column"], 7f,
                new[] { (items["Stone Blocks"], 4), (items["Cement"], 2) }, isStarter: false, unlockCost: 200, displeasure: 8, source: DispleasureSource.SlaveLabor,
                "Carves and assembles a monolithic column."));

            // SlaveLabor: War engines require massive forced labor
            blueprints.Add(CreateBlueprint("War Engine Parts", BlueprintType.Artisan, items["War Engine Parts"], 10f,
                new[] { (items["Iron Tools"], 4), (items["Rope"], 4) }, isStarter: false, unlockCost: 300, displeasure: 12, source: DispleasureSource.SlaveLabor,
                "Fabricates complex mechanisms for siege engines."));

            // === MASTERWORK BLUEPRINTS (Final Products) ===
            blueprints.Add(CreateBlueprint("Chariot", BlueprintType.Artisan, items["Chariot"], 12f,
                new[] { (items["Chariot Frame"], 1), (items["Bronze"], 2), (items["Leather"], 2) }, isStarter: false, unlockCost: 500, displeasure: 15, source: DispleasureSource.Craftsmanship,
                "Assembles a complete war chariot ready for battle.", workforce: 5));

            blueprints.Add(CreateBlueprint("Bronze Statue", BlueprintType.Artisan, items["Bronze Statue"], 12f,
                new[] { (items["Statue Base"], 1), (items["Bronze"], 4) }, isStarter: false, unlockCost: 450, displeasure: 12, source: DispleasureSource.Craftsmanship,
                "Casts and erects a monumental bronze statue.", workforce: 4));

            blueprints.Add(CreateBlueprint("Temple Component", BlueprintType.Artisan, items["Temple Component"], 10f,
                new[] { (items["Temple Column"], 2), (items["Mosaic"], 1) }, isStarter: false, unlockCost: 400, displeasure: 10, source: DispleasureSource.Craftsmanship,
                "Constructs a section of a grand temple.", workforce: 3));

            // SlaveLabor: War catapult requires extreme labor
            blueprints.Add(CreateBlueprint("War Catapult", BlueprintType.Artisan, items["War Catapult"], 15f,
                new[] { (items["War Engine Parts"], 1), (items["Rope"], 4), (items["Planks"], 4) }, isStarter: false, unlockCost: 600, displeasure: 18, source: DispleasureSource.SlaveLabor,
                "Builds a terrifying catapult for crushing city walls.", workforce: 6));

            blueprints.Add(CreateBlueprint("Royal Throne", BlueprintType.Artisan, items["Royal Throne"], 15f,
                new[] { (items["Furniture"], 2), (items["Gold Jewelry"], 2), (items["Linen Cloth"], 2) }, isStarter: false, unlockCost: 800, displeasure: 8, source: DispleasureSource.Craftsmanship,
                "Crafts a throne suitable for a god-king.", workforce: 4));

            // === KITCHEN BLUEPRINTS (Food Processing) ===
            blueprints.Add(CreateBlueprint("Flour", BlueprintType.Kitchen, items["Flour"], 2f,
                new[] { (items["Grain"], 2) }, isStarter: true, unlockCost: 0, displeasure: 1, source: DispleasureSource.Craftsmanship,
                "Grinds grain into fine flour."));

            blueprints.Add(CreateBlueprint("Wine", BlueprintType.Kitchen, items["Wine"], 4f,
                new[] { (items["Grapes"], 3) }, isStarter: false, unlockCost: 40, displeasure: 2, source: DispleasureSource.Craftsmanship,
                "Presses and ferments grapes into wine."));

            blueprints.Add(CreateBlueprint("Olive Oil", BlueprintType.Kitchen, items["Olive Oil"], 3f,
                new[] { (items["Olives"], 3) }, isStarter: false, unlockCost: 35, displeasure: 2, source: DispleasureSource.Craftsmanship,
                "Presses olives to extract valuable oil."));

            blueprints.Add(CreateBlueprint("Meat", BlueprintType.Kitchen, items["Meat"], 4f,
                new[] { (items["Cattle"], 1) }, isStarter: false, unlockCost: 50, displeasure: 3, source: DispleasureSource.Craftsmanship,
                "Prepares cuts of meat from livestock."));

            blueprints.Add(CreateBlueprint("Leather", BlueprintType.Kitchen, items["Leather"], 3f,
                new[] { (items["Cattle"], 1) }, isStarter: false, unlockCost: 45, displeasure: 3, source: DispleasureSource.Craftsmanship,
                "Tans hides into durable leather."));

            blueprints.Add(CreateBlueprint("Feast", BlueprintType.Kitchen, items["Feast"], 8f,
                new[] { (items["Bread"], 1), (items["Wine"], 1), (items["Meat"], 1) }, isStarter: false, unlockCost: 150, displeasure: 5, source: DispleasureSource.Craftsmanship,
                "Prepares a magnificent feast."));

            return blueprints;
        }

        private static BlueprintDefinition CreateBlueprint(string name, BlueprintType type, ItemDefinition outputItem, float time, (ItemDefinition item, int amount)[] inputs, bool isStarter, int unlockCost, int displeasure, DispleasureSource source, string description, int workforce = 1)
        {
            var bp = ScriptableObject.CreateInstance<BlueprintDefinition>();
            bp.name = $"BP_{name.Replace(" ", "")}";

            var so = new SerializedObject(bp);
            so.FindProperty("blueprintName").stringValue = name;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("productionTime").floatValue = time;
            so.FindProperty("isStarterCard").boolValue = isStarter;
            so.FindProperty("unlockCost").intValue = unlockCost;
            so.FindProperty("divineDispleasure").intValue = displeasure;
            so.FindProperty("displeasureSource").enumValueIndex = (int)source;
            so.FindProperty("workforceRequirement").intValue = workforce;

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
    }
}

