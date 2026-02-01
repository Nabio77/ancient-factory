using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements; // For ObjectField
using CarbonWorld.Core.Data;

namespace CarbonWorld.Editor.TechTree
{
    public class TechTreeGraphWindow : EditorWindow
    {
        private TechTreeGraphView _graphView;
        private TechTreeGraph _currentGraph;

        [MenuItem("Carbon World/Tech Tree Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<TechTreeGraphWindow>();
            window.titleContent = new GUIContent("Tech Tree");
        }

        public static void Open(TechTreeGraph graph)
        {
            var window = GetWindow<TechTreeGraphWindow>();
            window.titleContent = new GUIContent("Tech Tree");
            window.LoadGraph(graph);
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }

        private void ConstructGraphView()
        {
            _graphView = new TechTreeGraphView
            {
                name = "Tech Tree Graph"
            };

            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();

            // Save Button
            var saveButton = new UnityEditor.UIElements.ToolbarButton(() =>
            {
                _graphView.SaveGraph();
                Debug.Log("Graph Saved");
            })
            {
                text = "Save Asset"
            };
            toolbar.Add(saveButton);

            // Add Node Button (Simple fallback if drag and drop is tricky)
            var addNodeButton = new UnityEditor.UIElements.ToolbarButton(() =>
            {
                // Open object picker for blueprint
                EditorGUIUtility.ShowObjectPicker<BlueprintDefinition>(null, false, "", 1);
            })
            {
                text = "Add Node..."
            };
            toolbar.Add(addNodeButton);
            
            rootVisualElement.Add(toolbar);
        }
        
        // Handle Object Picker result
        void OnGUI()
        {
             if (Event.current.type == EventType.ExecuteCommand && 
                 Event.current.commandName == "ObjectSelectorClosed" &&
                 EditorGUIUtility.GetObjectPickerControlID() == 1)
             {
                 var obj = EditorGUIUtility.GetObjectPickerObject();
                 if (obj is BlueprintDefinition bp)
                 {
                     _graphView.CreateNode(bp, new Vector2(100, 100)); // Default pos
                     Event.current.Use();
                 }
             }
        }

        public void LoadGraph(TechTreeGraph graph)
        {
            _currentGraph = graph;
            _graphView.PopulateView(graph);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
#pragma warning disable 618
            var obj = EditorUtility.InstanceIDToObject(instanceId);
#pragma warning restore 618
            if (obj is TechTreeGraph graph)
            {
                Open(graph);
                return true;
            }
            return false;
        }
    }
}
