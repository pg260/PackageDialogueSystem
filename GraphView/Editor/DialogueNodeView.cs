using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;
using Runtime.DialogueSystem.Runtime.Data.Nodes;
using Runtime.DialogueSystem.Runtime.Data.Enums;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
public class DialogueNodeView : Node
{
    #region Constants

    private const int MAX_CHARS_ENTRY_PREVIEW = 200;
    private const int MAX_LINE_CHAR_COUNTS = 40;
    private const int MAX_CHARS_CHOICE_PREVIEW = 30;
    private static readonly Color COLOR_ENTRY_DEFAULT = new Color(0.55f, 0.29f, 0.04f);

    #endregion
    
    public string Guid;
    public DialogueNode DialogueNode;
    public Vector2 Position;
    public bool IsEntryPoint { get; private set; }
    
    public DialogueNodeView()
    {
        this.Guid = System.Guid.NewGuid().ToString();
        this.viewDataKey = this.Guid;
    }
    
    public void BuildNode()
    {
        inputContainer.Clear();
        outputContainer.Clear();
        extensionContainer.Clear();
        
        DefineTitle();

        DefineEntryPreview();
        
        var inputPort = CreatePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);
        
        switch (DialogueNode.NodeType)
        {
            case EDialogueType.Message://uses the events outputs, because it same output
            case EDialogueType.Event:
                var singleOutputPort = CreatePort(Direction.Output, Port.Capacity.Single);
                singleOutputPort.portName = "Next";
                outputContainer.Add(singleOutputPort);
                break;
            case EDialogueType.Choice:
                DialogueNode.Choices.ForEach(AddChoicePort);
                var choiceButton = new Button(() => {
                    var newChoice = new DialogueChoice();
                    DialogueNode.Choices.Add(newChoice);
                    AddChoicePort(newChoice);
                }) { text = "Add Choice" };
                extensionContainer.Add(choiceButton);
                break;
            case EDialogueType.Branching:
                DialogueNode.BranchConditions.ForEach(AddBranchPort);
                 var branchButton = new Button(() => {
                    var newBranch = new DialogueBranchCondition();
                    DialogueNode.BranchConditions.Add(newBranch);
                    AddBranchPort(newBranch);
                 }) { text = "Add Branch" };
                extensionContainer.Add(branchButton);
                break;
        }
        
        RefreshExpandedState();
        RefreshPorts();
    }
    public void SetEntryPoint(bool isEntry)
    {
        IsEntryPoint = isEntry;
        var titleContainer = this.Q("title");

        if (isEntry)
        {
            titleContainer.style.backgroundColor = new StyleColor(COLOR_ENTRY_DEFAULT); 
        }
        else
        {
            titleContainer.style.backgroundColor = new StyleColor(StyleKeyword.Null);
        }
    }
    private void DefineEntryPreview()
    {
        if (DialogueNode != null && DialogueNode.LocalizedText != null)
        {
            string localizedText = EditorLocalizationUtils.GetEditorLocalizedString(DialogueNode.LocalizedText);
            if (localizedText.Length > MAX_CHARS_ENTRY_PREVIEW)
            {
                localizedText = localizedText.Substring(0, MAX_CHARS_ENTRY_PREVIEW) + "...";
            }
            string wrappedText = WrapText(localizedText, MAX_LINE_CHAR_COUNTS);
            var nodeBodyLabel = new Label(wrappedText);
            nodeBodyLabel.style.whiteSpace = WhiteSpace.Normal;
            nodeBodyLabel.style.paddingLeft = 5;
            nodeBodyLabel.style.paddingRight = 5;
            mainContainer.Add(nodeBodyLabel);
        }
    }
    
    private static string WrapText(string text, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLineLength)
        {
            return text;
        }
        var words = text.Split(' ');
        var sb = new System.Text.StringBuilder();
        var currentLineLength = 0;
        foreach (var word in words)
        {
            if (currentLineLength + word.Length + 1 > maxLineLength)
            {
                sb.Append("\n");
                currentLineLength = 0;
            }
            if (currentLineLength > 0)
            {
                sb.Append(" ");
                currentLineLength++;
            }
            sb.Append(word);
            currentLineLength += word.Length;
        }

        return sb.ToString();
    }

    public void DefineTitle()
    {
        title = $"<b>{DialogueNode.NodeType.ToString().ToUpper()}</b>";
    }
    private void AddChoicePort(DialogueChoice choice)
    {
        var port = CreatePort(Direction.Output);
        port.userData = choice; 
        
        string choiceText = EditorLocalizationUtils.GetEditorLocalizedString(choice.ChoiceLocalizationKey);
        var choiceIndex = DialogueNode.Choices.IndexOf(choice);

        if (!string.IsNullOrEmpty(choiceText))
        {
            if (choiceText.Length > MAX_CHARS_CHOICE_PREVIEW)
            {
                port.portName = choiceText.Substring(0, MAX_CHARS_CHOICE_PREVIEW) + "...";
            }
            else
            {
                port.portName = choiceText;
            }
        }
        else
        {
            port.portName = $"<i>empty</i>";
        }

        var deleteButton = new Button(() => RemoveChoicePort(choice)) { text = "X" };
        port.Add(deleteButton);
        
        outputContainer.Add(port);
        RefreshPorts();
    }
    
    private void AddBranchPort(DialogueBranchCondition branch)
    {
        var port = CreatePort(Direction.Output);
        port.userData = branch;
        
        var branchIndex = DialogueNode.BranchConditions.IndexOf(branch);
        port.portName = $"Branch {branchIndex + 1}";
        
        var deleteButton = new Button(() => RemoveBranchPort(branch)) { text = "X" };
        port.Add(deleteButton);

        outputContainer.Add(port);
        RefreshPorts();
    }
    
    private void RemoveChoicePort(DialogueChoice choice)
    {
        DialogueNode.Choices.Remove(choice);
        var portToRemove = outputContainer.Children().FirstOrDefault(x => (x as Port)?.userData == choice) as Port;
        
        if (portToRemove != null)
        {
            var edgesToRemove = portToRemove.connections.ToList();
            foreach (var edge in edgesToRemove)
            {
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);
                
                edge.RemoveFromHierarchy();
            }
            outputContainer.Remove(portToRemove);
        }
    
        RefreshPorts();
    }
    
    private void RemoveBranchPort(DialogueBranchCondition branch)
    {
        DialogueNode.BranchConditions.Remove(branch);
        var portToRemove = outputContainer.Children().FirstOrDefault(x => (x as Port)?.userData == branch);
        if (portToRemove != null)
        {
             if((portToRemove as Port).connected)
             {
                 var edgeToRemove = this.GetContainingScope().Query<Edge>().ToList().First(e => e.output == portToRemove);
                 edgeToRemove.input.Disconnect(edgeToRemove);
                 edgeToRemove.RemoveFromHierarchy();
             }
             outputContainer.Remove(portToRemove);
        }
        RefreshPorts();
    }
    
    private Port CreatePort(Direction direction, Port.Capacity capacity = Port.Capacity.Single)
    {
        return Port.Create<Edge>(Orientation.Horizontal, direction, capacity, typeof(bool));
    }
}