using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using Runtime.DialogueSystem.Runtime.Data.Containers;
using Runtime.DialogueSystem.Runtime.Data.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

public class GraphEditorWindow : EditorWindow
{
    #region Constants
    
    private const float FILE_LIST_PANE_WIDTH = 250;
    private const float INSPECTOR_PANE_WIDTH = 300;

    private const string NEW_DIALOGUE_TITLE = "New Dialogue";
    private const string NEW_DIALOGUE_DEFAULT_NAME = "NewDialogue";
    private const string NEW_DIALOGUE_MESSAGE = "Please name the new dialogue.";

    private const string DUPLICATE_DIALOGUE_TITLE = "Duplicate";
    private const string DUPLICATE_DIALOGUE_MESSAGE = "Rename the copy.";
    private const string DUPLICATE_NAME_FORMAT = "{0}_copy";

    private const string DELETE_DIALOGUE_TITLE = "Delete";
    private const string DELETE_CONFIRM_YES = "Yes, Delete";
    private const string DELETE_CONFIRM_CANCEL = "Cancel";
    private const string DELETE_CONFIRMATION_MESSAGE_FORMAT = "Are you sure you want to delete '{0}'? This action cannot be undone.";

    private const string SAVE_SUCCESS_TITLE = "Saved!";
    private const string SAVE_SUCCESS_MESSAGE_FORMAT = "The dialogue '{0}' was saved.";

    private const string GRAPH_VIEW_NAME = "Graph Editor";
    private const string NO_GRAPH_SELECTED_TEXT = "No graph selected.";
    private const string INSPECTOR_DEFAULT_TEXT = "Select a Dialogue Node.";
    private const string INSPECTOR_NODE_NOT_FOUND_TEXT = "Error: Node not found.";
    
    #endregion

    private DialogueGraphView _graphView;
    private ListView _graphListView;
    private static DialogueContainer _activeGraph;
    private VisualElement _inspectorPane;
    private Label _activeFileNameLabel;

    public static DialogueContainer ActiveGraph => _activeGraph;

    [MenuItem("Tools/Dialogue Graph View Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<GraphEditorWindow>();
        window.titleContent = new GUIContent("Dialogue Graph View Editor");
    }

    private void OnEnable()
    {
        ConstructLayout();
        PopulateGraphList();
    }

    private void OnDisable()
    {
        if (_graphView != null)
        {
            _graphView.OnNodeSelected -= OnNodeSelectionChanged;
        }
        _graphListView.selectionChanged -= OnGraphSelectionChanged;
    }
    
    private void ConstructLayout()
    {
        rootVisualElement.Clear();

        var mainSplitView = new TwoPaneSplitView(1, INSPECTOR_PANE_WIDTH, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(mainSplitView);

        var leftAndCenterSplitView = new TwoPaneSplitView(0, FILE_LIST_PANE_WIDTH, TwoPaneSplitViewOrientation.Horizontal);
        mainSplitView.Add(leftAndCenterSplitView);
        
        // make panel and add it using the methods
        var fileListPane = CreateFileListPane();
        var graphPane = CreateGraphPane();
        _inspectorPane = CreateInspectorPane();

        leftAndCenterSplitView.Add(fileListPane);
        leftAndCenterSplitView.Add(graphPane);
        mainSplitView.Add(_inspectorPane);
        
        _graphView.OnNodeSelected += OnNodeSelectionChanged;

        UpdateInspector(null);
    }

    private VisualElement CreateFileListPane()
    {
        var pane = new VisualElement
        {
            style =
            {
                minWidth = FILE_LIST_PANE_WIDTH,
                flexDirection = FlexDirection.Column
            }
        };
    
        var buttonContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        pane.Add(buttonContainer);

        var createButton = new Button(() => CreateNewGraph(true)) { text = NEW_DIALOGUE_TITLE };
        buttonContainer.Add(createButton);

        var refreshButton = new Button(PopulateGraphList) { tooltip = "Refresh list of graphs" };
        refreshButton.Add(new Image { image = EditorGUIUtility.IconContent("d_Refresh").image });
        buttonContainer.Add(refreshButton);

        _graphListView = new ListView { style = { flexGrow = 1 } };
        pane.Add(_graphListView);

        _graphListView.makeItem = () => new Label();
        _graphListView.bindItem = (element, i) =>
        {
            var graph = _graphListView.itemsSource[i] as DialogueContainer;
            (element as Label).text = graph != null ? graph.name : "N/A";
        };
    
        _graphListView.selectionChanged += OnGraphSelectionChanged;
        AddContextMenuToListView();

        return pane;
    }
    
    private VisualElement CreateGraphPane()
    {
        var pane = new VisualElement();
        
        var toolbar = new Toolbar();
        _activeFileNameLabel = new Label(NO_GRAPH_SELECTED_TEXT)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginLeft = 10,
                flexGrow = 1
            }
        };
        toolbar.Add(_activeFileNameLabel);
        
        var spacer = new VisualElement { style = { flexGrow = 1 } };
        toolbar.Add(spacer);
        
        var refreshButton = new Button(() =>
        {
            if (_activeGraph != null)
            {
                _graphView.SaveGraph(_activeGraph);
                _graphView.LoadGraph(_activeGraph);
            }
        })
        {
            text = "Refresh" 
        };
        toolbar.Add(refreshButton);

        var saveButton = new Button(SaveGraphData) { text = "Save" };
        toolbar.Add(saveButton);
        pane.Add(toolbar);

        _graphView = new DialogueGraphView { name = GRAPH_VIEW_NAME, style = { flexGrow = 1 } };
        pane.Add(_graphView);

        return pane;
    }
    
    private VisualElement CreateInspectorPane()
    {
        var pane = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                paddingLeft = 10,
                paddingRight = 10
            }
        };
        return pane;
    }
    
    private void OnNodeSelectionChanged(IEnumerable<ISelectable> selection)
    {
        var selectedNodeView = selection.FirstOrDefault() as DialogueNodeView;
        UpdateInspector(selectedNodeView?.DialogueNode);
    }
    
    private void UpdateInspector(DialogueNode node)
    {
        _inspectorPane.Clear();

        if (node == null)
        {
            _inspectorPane.Add(new Label(INSPECTOR_DEFAULT_TEXT));
            return;
        }

        var serializedObject = new SerializedObject(_activeGraph);
        var nodesProperty = serializedObject.FindProperty("Nodes");
        nodesProperty.isExpanded = true;
        int nodeIndex = _activeGraph.Nodes.FindIndex(n => n.NodeId == node.NodeId);

        if (nodeIndex == -1)
        {
            _inspectorPane.Add(new Label(INSPECTOR_NODE_NOT_FOUND_TEXT));
            return;
        }

        var nodeProperty = nodesProperty.GetArrayElementAtIndex(nodeIndex);
        var propertyField = new PropertyField(nodeProperty);
        nodeProperty.isExpanded = true;
        
        propertyField.Bind(serializedObject);
        _inspectorPane.Add(propertyField);
    }

    private void AddContextMenuToListView()
    {
        var menuManipulator = new ContextualMenuManipulator(menuEvent =>
        {
            if (_graphListView.selectedItem is not DialogueContainer selectedGraph) return;
            menuEvent.menu.AppendAction("Duplicate", _ => DuplicateGraph(selectedGraph));
            menuEvent.menu.AppendAction("Delete", _ => DeleteGraph(selectedGraph));
        });
        _graphListView.AddManipulator(menuManipulator);
    }

    private void PopulateGraphList()
    {
        var guids = AssetDatabase.FindAssets("t:DialogueContainer");
        var graphs = guids.Select(guid => AssetDatabase.LoadAssetAtPath<DialogueContainer>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        
        _graphListView.itemsSource = graphs;
        _graphListView.RefreshItems();
    }

    private void OnGraphSelectionChanged(IEnumerable<object> selectedItems)
    {
        _activeGraph = selectedItems.FirstOrDefault() as DialogueContainer;

        if (_activeGraph != null)
        {
            _graphView.LoadGraph(_activeGraph);
            _activeFileNameLabel.text = $"{_activeGraph.name}";
        }
        else
        {
            _graphView.LoadGraph(null);
            _activeFileNameLabel.text = NO_GRAPH_SELECTED_TEXT;
        }
        UpdateInspector(null);
    }

    private DialogueContainer CreateNewGraph(bool clearGraph)
    {
        string path = EditorUtility.SaveFilePanelInProject(NEW_DIALOGUE_TITLE, NEW_DIALOGUE_DEFAULT_NAME, "asset", NEW_DIALOGUE_MESSAGE);
        if (string.IsNullOrEmpty(path)) return null;
        
        var newGraph = CreateInstance<DialogueContainer>();
        if (!clearGraph)
        {
            _graphView.SaveGraph(newGraph);
        }

        AssetDatabase.CreateAsset(newGraph, path);
        AssetDatabase.SaveAssets();
        PopulateGraphList();
        _graphListView.SetSelection(_graphListView.itemsSource.IndexOf(newGraph));
        return newGraph;
    }

    private void SaveGraphData()
    {
        if (_activeGraph == null)
        {
            var newGraph = CreateNewGraph(false);
            if (newGraph != null)
            {
                EditorUtility.DisplayDialog(SAVE_SUCCESS_TITLE, string.Format(SAVE_SUCCESS_MESSAGE_FORMAT, newGraph.name), "OK");
            }
            return;
        }
        _graphView.SaveGraph(_activeGraph);
        EditorUtility.DisplayDialog(SAVE_SUCCESS_TITLE, string.Format(SAVE_SUCCESS_MESSAGE_FORMAT, _activeGraph.name), "OK");
    }

    private void DuplicateGraph(DialogueContainer originalGraph)
    {
        string originalPath = AssetDatabase.GetAssetPath(originalGraph);
        string newPath = EditorUtility.SaveFilePanelInProject(DUPLICATE_DIALOGUE_TITLE, 
            string.Format(DUPLICATE_NAME_FORMAT, originalGraph.name), 
            "asset", 
            DUPLICATE_DIALOGUE_MESSAGE);
        if (string.IsNullOrEmpty(newPath)) return;
        AssetDatabase.CopyAsset(originalPath, newPath);
        AssetDatabase.SaveAssets();
        PopulateGraphList();
    }

    private void DeleteGraph(DialogueContainer graphToDelete)
    {
        string path = AssetDatabase.GetAssetPath(graphToDelete);
        if (EditorUtility.DisplayDialog(DELETE_DIALOGUE_TITLE, 
                string.Format(DELETE_CONFIRMATION_MESSAGE_FORMAT, graphToDelete.name), 
                DELETE_CONFIRM_YES,
                DELETE_CONFIRM_CANCEL))
        {
            if (_activeGraph == graphToDelete)
            {
                _graphListView.ClearSelection();
                _activeGraph = null;
            }
            
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            PopulateGraphList();
        }
    }
    public void LoadGraphAsset(DialogueContainer container)
    {
        if (container == null) return;
        _activeGraph = container;
        
        if (_graphView != null)
        {
            _graphView.LoadGraph(_activeGraph);
        }
    
        if (_activeFileNameLabel != null)
        {
            _activeFileNameLabel.text = _activeGraph.name;
        }

        if (_graphListView != null)
        {
            _graphListView.SetSelection(_graphListView.itemsSource.IndexOf(_activeGraph));
            _graphListView.ScrollToItem(_graphListView.selectedIndex);
        }
        UpdateInspector(null);
    }
}