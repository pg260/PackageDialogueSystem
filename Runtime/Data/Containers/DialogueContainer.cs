using System.Collections.Generic;
using Runtime.DialogueSystem.Runtime.Data.Enums;
using Runtime.DialogueSystem.Runtime.Data.Nodes;
using UnityEngine;

namespace Runtime.DialogueSystem.Runtime.Data.Containers
{
    /// <summary>
    /// Container de diálogo que armazena uma lista de nós e fornece métodos de acesso a eles.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue System/Dialogue Container")]
    public class DialogueContainer : ScriptableObject
    {
        /// <summary>
        /// Lista de nós que compõem este diálogo.
        /// </summary>
        public List<DialogueNode> Nodes = new();
    
        /// <summary>
        /// Encontra um nó pelo seu identificador único.
        /// </summary>
        /// <param name="nodeId">ID do nó a ser encontrado.</param>
        /// <returns>O nó correspondente ou null se não encontrado.</returns>
        public DialogueNode FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                Debug.LogError("FindNode recebeu nodeId nulo ou vazio.");
                return null;
            }

            var node = Nodes.Find(n => n.NodeId == nodeId);
            if (node == null)
            {
                Debug.LogError($"Nó '{nodeId}' não encontrado no container '{name}'.");
            }
            return node;
        }
    
        /// <summary>
        /// Determina o próximo nó a partir do nó atual, baseado no tipo de fluxo.
        /// </summary>
        /// <param name="currentNode">Nó atual no fluxo de diálogo.</param>
        /// <returns>Próximo nó ou null se não houver continuação.</returns>
        public DialogueNode GetNextNode(DialogueNode currentNode)
        {
            if (currentNode == null)
            {
                Debug.LogError("GetNextNode recebeu currentNode nulo.");
                return null;
            }

            switch (currentNode.NodeType)
            {
                case EDialogueType.Message:
                    // Avança automaticamente para o DefaultNextNodeId
                    return FindNode(currentNode.DefaultNextNodeId);

                case EDialogueType.Branching:
                    // Checa condições de ramificação sequencialmente
                    foreach (var branch in currentNode.BranchConditions)
                    {
                        if (EvaluateCondition(branch))
                            return FindNode(branch.TargetNodeId);
                    }
                    return null;

                case EDialogueType.Choice:
                case EDialogueType.Event:
                default:
                    // Para escolhas e eventos, o fluxo é controlado externamente
                    return null;
            }
        }

        /// <summary>
        /// Avalia se a condição de ramificação é atendida.
        /// </summary>
        private bool EvaluateCondition(DialogueBranchCondition branch)
        {
            // Aqui você deve implementar a lógica de avaliação do estado do jogo,
            // por exemplo: verificar variáveis, inventário, quests, etc.
            // Este método deve ser estendido conforme suas necessidades.
            Debug.LogWarning("EvaluateCondition ainda não implementado. Sempre retorna false.");
            return false;
        }
    }
}