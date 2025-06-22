using System;
using UnityEditor;
using Runtime.DialogueSystem.Runtime.Data.Containers;

[InitializeOnLoad]
public class DialogueAssetHandler
{
    static DialogueAssetHandler()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        Object selectedObject = Selection.activeObject;
        
        if (selectedObject is DialogueContainer container)
        {
            var window = EditorWindow.GetWindow<GraphEditorWindow>(false, null, false);
            if (window != null)
            {
                window.LoadGraphAsset(container);
            }
        }
    }
}