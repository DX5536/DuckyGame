using System.Collections.Generic;
using CarterGames.Assets.AudioManager;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Scene-level dispatcher on top of Carter Games' AudioManager.
/// Define named audio "entries" in the Inspector; call Play("key") from any UnityEvent.
/// Each entry keeps its own Started / Looped / Completed UnityEvents so downstream logic
/// can react to a specific sound finishing (same benefit as InspectorAudioClipPlayer).
/// </summary>
public class AudioManager_System : MonoBehaviour
{
    [System.Serializable]
    public class AudioEntry
    {
        [Tooltip("The key you pass to Play(...), e.g. \"hover\", \"snap\", \"click\".")]
        public string key;

        [Tooltip("Clip (or Group) name exactly as it appears in the Carter Games library.")]
        public string request;

        [Tooltip("Tick this if 'request' is a Group name; leave off for a single clip.")]
        public bool isGroup;

        [Range(0f, 2f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;

        [Header("Per-play Events (mirrors InspectorAudioClipPlayer)")]
        public UnityEvent onStarted;
        public UnityEvent onLooped;
        public UnityEvent onCompleted;
    }

    [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();

    [Tooltip("Log a warning when Play() is called with a key not present in the list above.")]
    [SerializeField] private bool warnOnMissingKey = true;

    // Fast lookup so Play("hover") is O(1) instead of scanning the list every call.
    private Dictionary<string, AudioEntry> lookup;

    private void Awake()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, AudioEntry>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            AudioEntry e = entries[i];
            if (string.IsNullOrEmpty(e.key)) continue;
            lookup[e.key] = e;
        }
    }

    /// <summary>
    /// Play an entry by key. Wire this to any UnityEvent (String) and type the key in the field.
    /// </summary>
    public void Play(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (lookup == null) BuildLookup();

        if (!lookup.TryGetValue(key, out AudioEntry entry))
        {
            if (warnOnMissingKey)
            {
                Debug.LogWarning($"[{name}] AudioManager_System: no entry with key '{key}'. Add one in the Inspector.", this);
            }
            return;
        }

        AudioPlayer player;
        try
        {
            player = entry.isGroup
                ? AudioManager.PlayGroup(entry.request, entry.volume, entry.pitch)
                : AudioManager.Play(entry.request, entry.volume, entry.pitch);
        }
        catch (MissingReferenceException ex)
        {
            // Carter's internal pool holds a destroyed AudioPlayer reference. Almost always caused by
            // Unity's "Enter Play Mode Options" with Reload Domain disabled - static pool state persists
            // across play sessions while the pooled GameObjects get destroyed.
            // Fix: Project Settings > Editor > Enter Play Mode Options -> tick "Reload Domain".
            Debug.LogError($"[{name}] AudioManager_System: Carter's audio pool is holding a destroyed reference. " +
                           $"Enable 'Reload Domain' in Project Settings > Editor > Enter Play Mode Options, or restart the Editor. " +
                           $"({ex.Message})", this);
            return;
        }

        // Null when Carter's AudioManager is globally disabled (Settings.PlayAudioState == Disabled).
        if (player == null) return;

        // Wire this specific playback's callbacks back to the entry's UnityEvents.
        // A fresh AudioPlayer is created each Play call, so no double-fire across plays.
        player.Started.Add(() => entry.onStarted?.Invoke());
        player.Looped.Add(() => entry.onLooped?.Invoke());
        player.Completed.Add(() => entry.onCompleted?.Invoke());
    }
}
