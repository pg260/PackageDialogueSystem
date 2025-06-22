using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Runtime.DialogueSystem.Runtime.Data.Enums;
using Runtime.DialogueSystem.Runtime.Data.Nodes;
using Runtime.DialogueSystem.Runtime.Data.Containers;
using UnityEditor;
using System;

public class DialogueGraphView : GraphView
{
    public Action<IEnumerable<ISelectable>> OnNodeSelected;
    
    public DialogueGraphView()
    {
        Insert(0, new GridBackground());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        this.RegisterCallback<ContextualMenuPopulateEvent>(menuEvent =>
        {
            menuEvent.menu.MenuItems().Clear();

            var mousePos = viewTransform.matrix.inverse.MultiplyPoint(menuEvent.localMousePosition);

            if (menuEvent.target is GraphView || menuEvent.target is GridBackground)
            {
                foreach (EDialogueType type in Enum.GetValues(typeof(EDialogueType)))
                {
                    menuEvent.menu.AppendAction($"Create Node/{type}", _ => AddElement(CreateNode(mousePos, type)));
                }
            }
            else if (menuEvent.target is DialogueNodeView || (menuEvent.target as VisualElement)?.GetFirstAncestorOfType<DialogueNodeView>() != null)
            {
                DialogueNodeView targetNode = (menuEvent.target as DialogueNodeView) 
                                              ?? (menuEvent.target as VisualElement)?.GetFirstAncestorOfType<DialogueNodeView>();
                if (targetNode != null && !targetNode.IsEntryPoint)
                {
                    menuEvent.menu.AppendAction("Set as Entry Point", _ => SetEntryPointNode(targetNode));
                }
                menuEvent.menu.AppendAction("Delete", _ => DeleteSelection());
            }
            
        });
    }
    private void SetEntryPointNode(DialogueNodeView entryNode)
    {
        var oldEntryPoint = nodes.OfType<DialogueNodeView>().FirstOrDefault(n => n.IsEntryPoint);
        if (oldEntryPoint != null)
        {
            oldEntryPoint.SetEntryPoint(false);
        }
        
        entryNode.SetEntryPoint(true);
    }
    
    public override void AddToSelection(ISelectable selectable)
    {
        base.AddToSelection(selectable);
        OnNodeSelected?.Invoke(this.selection);
    }
    
    private DialogueNodeView CreateNode(Vector2 position, EDialogueType nodeType)
    {
        var nodeView = new DialogueNodeView();
        nodeView.SetPosition(new Rect(position, new Vector2(200, 150)));

        var dialogueNode = new DialogueNode
        {
            NodeId = nodeView.Guid,
            NodeType = nodeType,
            Position = position
        };
        
        nodeView.DialogueNode = dialogueNode;
        nodeView.BuildNode();
        
        if (GraphEditorWindow.ActiveGraph != null)
        {
            GraphEditorWindow.ActiveGraph.Nodes.Add(dialogueNode);
            if (GraphEditorWindow.ActiveGraph.Nodes.Count == 1)
                SetEntryPointNode(nodeView);
        }
        
        return nodeView;
    }
    public void LoadGraph(DialogueContainer dialogueContainer)
    {
        DeleteElements(graphElements);
        if (dialogueContainer == null) return;

        var nodeViews = new Dictionary<string, DialogueNodeView>();
        
        foreach (var nodeData in dialogueContainer.Nodes)
        {
            var nodeView = new DialogueNodeView
            {
                Guid = nodeData.NodeId,
                DialogueNode = nodeData,
                Position = nodeData.Position
            };
        
            nodeView.BuildNode();
            nodeView.SetPosition(new Rect(nodeView.Position, new Vector2(200, 150)));

            AddElement(nodeView);
            nodeViews.Add(nodeData.NodeId, nodeView);
        }
        
        if (dialogueContainer.Nodes.Count > 0)
        {
            string entryNodeId = dialogueContainer.Nodes[0].NodeId;
            
            if (nodeViews.TryGetValue(entryNodeId, out var entryPointView))
            {
                entryPointView.SetEntryPoint(true);
            }
        }
        
        foreach (var sourceNodeData in dialogueContainer.Nodes)
        {
            var sourceView = nodeViews[sourceNodeData.NodeId];
            
            switch (sourceNodeData.NodeType)
            {
                case EDialogueType.Message:
                case EDialogueType.Event:
                    if (!string.IsNullOrEmpty(sourceNodeData.DefaultNextNodeId) && nodeViews.TryGetValue(sourceNodeData.DefaultNextNodeId, out var nextNodeView))
                    {
                        var outputPort = sourceView.outputContainer.Q<Port>();
                        var inputPort = nextNodeView.inputContainer.Q<Port>();
                        var edge = outputPort.ConnectTo(inputPort);
                        AddElement(edge);
                    }
                    break;
                case EDialogueType.Choice:
                    for (int i = 0; i < sourceNodeData.Choices.Count; i++)
                    {
                        var choice = sourceNodeData.Choices[i];
                        if (!string.IsNullOrEmpty(choice.TargetNodeId) && nodeViews.TryGetValue(choice.TargetNodeId, out var targetNode))
                        {
                            var outputPort = sourceView.outputContainer[i] as Port;
                            var inputPort = targetNode.inputContainer.Q<Port>();
                            var edge = outputPort.ConnectTo(inputPort);
                            AddElement(edge);
                        }
                    }
                    break;
                case EDialogueType.Branching:
                     for (int i = 0; i < sourceNodeData.BranchConditions.Count; i++)
                     {
                         var branch = sourceNodeData.BranchConditions[i];
                         if (!string.IsNullOrEmpty(branch.TargetNodeId) && nodeViews.TryGetValue(branch.TargetNodeId, out var targetNode))
                         {
                             var outputPort = sourceView.outputContainer[i] as Port;
                             var inputPort = targetNode.inputContainer.Q<Port>();
                             var edge = outputPort.ConnectTo(inputPort);
                             AddElement(edge);
                         }
                     }
                     break;
            }
        }
    }
    
    public void SaveGraph(DialogueContainer dialogueContainer)
    {
        if (dialogueContainer == null) return;
        
        var nodeViews = nodes.Cast<DialogueNodeView>().ToList();
        foreach (var nodeView in nodeViews)
        {
            nodeView.DialogueNode.Position = nodeView.GetPosition().position;
            nodeView.DialogueNode.DefaultNextNodeId = string.Empty;
            nodeView.DialogueNode.Choices.ForEach(c => c.TargetNodeId = string.Empty);
            nodeView.DialogueNode.BranchConditions.ForEach(b => b.TargetNodeId = string.Empty);
        }
        
        foreach (var edge in edges)
        {
            var outputNodeView = edge.output.node as DialogueNodeView;
            var inputNodeView = edge.input.node as DialogueNodeView;

            if (outputNodeView == null || inputNodeView == null) continue;

            switch (outputNodeView.DialogueNode.NodeType)
            {
                case EDialogueType.Message:
                case EDialogueType.Event:
                    outputNodeView.DialogueNode.DefaultNextNodeId = inputNodeView.Guid;
                    break;
                
                case EDialogueType.Choice:
                    var choice = edge.output.userData as DialogueChoice;
                    if (choice != null)
                    {
                        choice.TargetNodeId = inputNodeView.Guid;
                    }
                    break;
                
                case EDialogueType.Branching:
                    var branch = edge.output.userData as DialogueBranchCondition;
                    if (branch != null)
                    {
                        branch.TargetNodeId = inputNodeView.Guid;
                    }
                    break;
            }
        }
        
        dialogueContainer.Nodes.Clear(); 
        
        var entryPointView = nodeViews.FirstOrDefault(nv => nv.IsEntryPoint);
        
        if (entryPointView != null)
        {
            dialogueContainer.Nodes.Add(entryPointView.DialogueNode);
            foreach (var nodeView in nodeViews.Where(nv => nv != entryPointView))
            {
                dialogueContainer.Nodes.Add(nodeView.DialogueNode);
            }
        }
        else
        {
            dialogueContainer.Nodes.AddRange(nodeViews.Select(nv => nv.DialogueNode));
        }
        
        EditorUtility.SetDirty(dialogueContainer);
        AssetDatabase.SaveAssets();
    }
    
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }
}