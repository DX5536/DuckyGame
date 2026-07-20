using UnityEngine;
using Yarn.Unity;
using Yarn.Unity.Attributes;

public class YarnSpinner_TriggerSpecificNode : MonoBehaviour
{
    [Header("Yarn Setup")]
    [Tooltip("The Dialogue Runner that will play the selected node.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("The Yarn Project that contains the node.")]
    [SerializeField] private YarnProject yarnProject;

    [Tooltip("The node to play when TriggerNode() is called. The dropdown lists every node in the Yarn Project above.")]
    [YarnNode(nameof(yarnProject))]
    [SerializeField] private string nodeName;

    /// <summary>
    /// Wire this up to any UnityEvent (e.g. NPC_TriggerEvents.OnCollide) to play the selected Yarn node.
    /// </summary>
    public void TriggerNode()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError($"[{name}] TriggerSpecificNode: Dialogue Runner is not assigned.", this);
            return;
        }

        if (yarnProject == null)
        {
            Debug.LogError($"[{name}] TriggerSpecificNode: Yarn Project is not assigned.", this);
            return;
        }

        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogError($"[{name}] TriggerSpecificNode: No node selected in the dropdown.", this);
            return;
        }

        // Guard against a stale node name that no longer exists in the current Yarn Project.
        if (System.Array.IndexOf(yarnProject.NodeNames, nodeName) < 0)
        {
            Debug.LogError($"[{name}] TriggerSpecificNode: Node '{nodeName}' does not exist in Yarn Project '{yarnProject.name}'.", this);
            return;
        }

        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.LogWarning($"[{name}] TriggerSpecificNode: A dialogue is already running; ignoring request to play '{nodeName}'.", this);
            return;
        }

        // Fire-and-forget; StartDialogue returns a YarnTask we don't need to await from a UnityEvent callback.
        _ = dialogueRunner.StartDialogue(nodeName);
    }
}
