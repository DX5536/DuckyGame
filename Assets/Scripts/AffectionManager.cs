using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

/// Scene-persistent manager for NPC affection values.
/// Exposes two Yarn commands - <![CDATA[<<increase_affection npcName amount>>]]> and
/// <![CDATA[<<decrease_affection npcName amount>>]]> - that route to per-NPC events.
public class AffectionManager : MonoBehaviour
{
    // One entry per NPC. Pairs the NPC scriptable object with its personal events so the Inspector shows them side-by-side (no parallel-list bookkeeping).

    [System.Serializable]
    public class NPCAffectionEntry
    {
        [Tooltip("The NPC scriptable object this entry represents.")]
        public InteractableNPC npc;

        [Tooltip("Fires ONLY when this specific NPC's affection increases.")]
        public UnityEvent onSpecificIncrease;

        [Tooltip("Fires ONLY when this specific NPC's affection decreases.")]
        public UnityEvent onSpecificDecrease;
    }

    /// <summary>Singleton so the static Yarn commands can find the live instance.</summary>
    public static AffectionManager Instance { get; private set; }

    [Header("NPC Roster")]
    [Tooltip("Every NPC in the game that has an affection value. Order matters - specific events are looked up by NPC index.")]
    [SerializeField] private List<NPCAffectionEntry> npcs = new List<NPCAffectionEntry>();

    [Header("Global Events (fire on ANY NPC's change)")]
    [SerializeField] private UnityEvent onIncreaseAffection;
    [SerializeField] private UnityEvent onDecreaseAffection;

    //NPC-name -> roster-index lookup, built once in Awake. No per-call linear scan or if-else chain.
    private Dictionary<string, int> nameToIndex;

    private void Awake()
    {
        // Singleton with DontDestroyOnLoad so YarnCommand callers always find a live instance, even after scene loads.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
        ResetAffectionForDebug();
    }

    // Zeroes out any roster NPC whose ResetAffection_DEBUG flag is on. Runs on THIS
    // MonoBehaviour's Awake so it fires reliably every play session, even when
    // "Enter Play Mode -> Reload Domain" is disabled (which can make the SO's own
    // OnEnable skip firing between plays).
    private void ResetAffectionForDebug()
    {
        for (int i = 0; i < npcs.Count; i++)
        {
            NPCAffectionEntry entry = npcs[i];
            if (entry == null || entry.npc == null) continue;
            if (entry.npc.ResetAffection_DEBUG)
            {
                entry.npc.AffectionValue = 0;
            }
        }
    }

    private void BuildLookup()
    {
        nameToIndex = new Dictionary<string, int>(npcs.Count);
        for (int i = 0; i < npcs.Count; i++)
        {
            NPCAffectionEntry entry = npcs[i];
            if (entry == null || entry.npc == null) continue;
            string key = entry.npc.NPCName;
            if (string.IsNullOrEmpty(key)) continue;
            nameToIndex[key] = i;
        }
    }

    // ---------- Yarn Commands ----------

    /// Yarn: <![CDATA[<<increase_affection Sarah 5>>]]>. Adds affection; the InteractableNPC
    /// setter clamps to +20 automatically. Fires the global increase event AND this NPC's specific increase event.
    [YarnCommand("increase_affection")]
    public static void IncreaseAffection(string npcNameYarn, int affectionValueYarn)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[AffectionManager] IncreaseAffection called but no instance is in the scene.");
            return;
        }
        Instance.ApplyAffectionChange(npcNameYarn, Mathf.Abs(affectionValueYarn), isIncrease: true);
    }

    /// <summary>
    /// Yarn: <![CDATA[<<decrease_affection Sarah 5>>]]>. Subtracts affection; the InteractableNPC
    /// setter clamps to -20 automatically. Always treats the amount as its absolute value so a
    /// negative in Yarn can't accidentally re-invert the operation.
    /// </summary>
    [YarnCommand("decrease_affection")]
    public static void DecreaseAffection(string npcNameYarn, int affectionValueYarn)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[AffectionManager] DecreaseAffection called but no instance is in the scene.");
            return;
        }
        Instance.ApplyAffectionChange(npcNameYarn, Mathf.Abs(affectionValueYarn), isIncrease: false);
    }

    // ---------- Internals ----------

    private void ApplyAffectionChange(string npcName, int magnitude, bool isIncrease)
    {
        // Dictionary lookup is O(1) and works for 5, 10, or 100 NPCs the same way.
        // No if-else chain, no linear scan.
        if (!nameToIndex.TryGetValue(npcName, out int index))
        {
            Debug.LogWarning($"[AffectionManager] No NPC with name '{npcName}' in the roster.", this);
            return;
        }

        NPCAffectionEntry entry = npcs[index];
        InteractableNPC npc = entry.npc;

        // The InteractableNPC.AffectionValue setter already clamps to [-20, +20].
        if (isIncrease)
        {
            npc.AffectionValue += magnitude;
            onIncreaseAffection?.Invoke();
            entry.onSpecificIncrease?.Invoke();
        }
        else
        {
            npc.AffectionValue -= magnitude;
            onDecreaseAffection?.Invoke();
            entry.onSpecificDecrease?.Invoke();
        }
    }
}
