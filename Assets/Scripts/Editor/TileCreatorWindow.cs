using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace CarbonWorld.Editor
{
    public class TileCreatorWindow : EditorWindow
    {
        private Sprite coreSprite;
        private Sprite resourceSprite;
        private Sprite productionSprite;
        private Sprite enhancementSprite;
        private Sprite hoverHighlightSprite;
        private Sprite selectedHighlightSprite;

        private string outputFolder = "Assets/Tiles";

        [MenuItem("Tools/Carbon World/Create Hex Tiles")]
        public static void ShowWindow()
        {
            GetWindow<TileCreatorWindow>("Create Hex Tiles");
        }

        private void OnGUI()
        {
            GUILayout.Label("Create Tile Assets from Sprites", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            EditorGUILayout.Space();

            GUILayout.Label("Tile Sprites", EditorStyles.boldLabel);
            coreSprite = (Sprite)EditorGUILayout.ObjectField("Core Tile", coreSprite, typeof(Sprite), false);
            resourceSprite = (Sprite)EditorGUILayout.ObjectField("Resource Tile", resourceSprite, typeof(Sprite), false);
            productionSprite = (Sprite)EditorGUILayout.ObjectField("Production Tile", productionSprite, typeof(Sprite), false);
            enhancementSprite = (Sprite)EditorGUILayout.ObjectField("Enhancement Tile", enhancementSprite, typeof(Sprite), false);

            EditorGUILayout.Space();
            GUILayout.Label("Highlight Sprites (Optional)", EditorStyles.boldLabel);
            hoverHighlightSprite = (Sprite)EditorGUILayout.ObjectField("Hover Highlight", hoverHighlightSprite, typeof(Sprite), false);
            selectedHighlightSprite = (Sprite)EditorGUILayout.ObjectField("Selected Highlight", selectedHighlightSprite, typeof(Sprite), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Tile Assets", GUILayout.Height(30)))
            {
                CreateTiles();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "1. Assign sprites for each tile type\n" +
                "2. Click 'Create Tile Assets'\n" +
                "3. Tiles will be created in the output folder\n" +
                "4. Assign the created tiles to your WorldMap component",
                MessageType.Info);
        }

        private void CreateTiles()
        {
            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                var parts = outputFolder.Split('/');
                var currentPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var newPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = newPath;
                }
            }

            int created = 0;

            if (coreSprite != null)
            {
                CreateTile(coreSprite, "CoreTile");
                created++;
            }
            if (resourceSprite != null)
            {
                CreateTile(resourceSprite, "ResourceTile");
                created++;
            }
            if (productionSprite != null)
            {
                CreateTile(productionSprite, "ProductionTile");
                created++;
            }
            if (enhancementSprite != null)
            {
                CreateTile(enhancementSprite, "EnhancementTile");
                created++;
            }
            if (hoverHighlightSprite != null)
            {
                CreateTile(hoverHighlightSprite, "HoverHighlightTile");
                created++;
            }
            if (selectedHighlightSprite != null)
            {
                CreateTile(selectedHighlightSprite, "SelectedHighlightTile");
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Tiles Created", $"Created {created} tile assets in {outputFolder}", "OK");
        }

        private void CreateTile(Sprite sprite, string tileName)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;

            var path = $"{outputFolder}/{tileName}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null)
            {
                existing.sprite = sprite;
                EditorUtility.SetDirty(existing);
                Debug.Log($"Updated existing tile: {path}");
            }
            else
            {
                AssetDatabase.CreateAsset(tile, path);
                Debug.Log($"Created tile: {path}");
            }
        }
    }
}
