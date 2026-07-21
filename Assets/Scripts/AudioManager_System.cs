using System.Collections.Generic;
using System.Reflection;
using CarterGames.Assets.AudioManager;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Scene-level dispatcher on top of Carter Games' AudioManager.
/// Define named audio "entries" in the Inspector; call Play("key") from any UnityEvent.
/// Each entry keeps its own Started / Looped / Completed UnityEvents so downstream logic
/// can react to a specific sound finishing (same benefit as InspectorAudioClipPlayer).
///
/// If a Manual AudioPlayer is assigned, Play() drives it directly instead of going through
/// Carter's static pool - useful when the pool holds stale references (e.g. Fast Play Mode
/// without Reload Domain).
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

    [Header("Manual AudioPlayer (optional)")]
    [Tooltip("If assigned, Play() drives THIS AudioPlayer directly instead of Carter's pooled static AudioManager.Play. " +
             "Assign the AudioPlayer component sitting on your scene GameObject (e.g. the +[AudioManager] - Audio Player prefab). " +
             "Leave empty to use the pool.")]
    [SerializeField] private AudioPlayer manualPlayer;

    // Fast lookup so Play("hover") is O(1) instead of scanning the list every call.
    private Dictionary<string, AudioEntry> lookup;

    // Cached reflection handles for the two Carter-private setters we need to bypass.
    private static PropertyInfo isInitializedProp;
    private static PropertyInfo recycleOnCompleteProp;

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

        if (manualPlayer != null)
        {
            PlayWithManualPlayer(entry);
        }
        else
        {
            PlayWithPool(entry);
        }
    }

    // ---------- Manual player path (bypasses Carter's pool) ----------

    private void PlayWithManualPlayer(AudioEntry entry)
    {
        // Carter's Initialize is a one-shot (early-returns if IsInitialized is true), so we reset
        // it via reflection to let us switch clips on the same scene AudioPlayer between calls.
        SetIsInitialized(manualPlayer, false);

        var settings = new AudioClipSettings(new IEditModule[]
        {
            new VolumeEdit(entry.volume),
            new PitchEdit(entry.pitch)
        });

        if (entry.isGroup) manualPlayer.InitializeGroup(entry.request, settings);
        else manualPlayer.Initialize(entry.request, settings);

        // Initialize sets RecycleOnComplete = true. Flip it off so the player won't try to
        // reparent itself to Carter's DontDestroyOnLoad pool when the clip finishes.
        SetRecycleOnComplete(manualPlayer, false);

        // The Evts on a reused player accumulate subscribers; clear before re-adding for THIS entry.
        manualPlayer.Started.Clear();
        manualPlayer.Looped.Clear();
        manualPlayer.Completed.Clear();
        manualPlayer.Started.Add(() => entry.onStarted?.Invoke());
        manualPlayer.Looped.Add(() => entry.onLooped?.Invoke());
        manualPlayer.Completed.Add(() => entry.onCompleted?.Invoke());

        manualPlayer.Play();
    }

    // ---------- Pool path (original behavior) ----------

    private void PlayWithPool(AudioEntry entry)
    {
        AudioPlayer player;
        try
        {
            player = entry.isGroup
                ? AudioManager.PlayGroup(entry.request, entry.volume, entry.pitch)
                : AudioManager.Play(entry.request, entry.volume, entry.pitch);
        }
        catch (MissingReferenceException ex)
        {
            Debug.LogError($"[{name}] AudioManager_System: Carter's audio pool holds a destroyed reference. " +
                           $"Fix by enabling 'Reload Domain' in Project Settings > Editor > Enter Play Mode Options, " +
                           $"OR assign a Manual AudioPlayer on this component. ({ex.Message})", this);
            return;
        }

        if (player == null) return;

        // Pool gives us a fresh player each call, so no need to clear old subscribers first.
        player.Started.Add(() => entry.onStarted?.Invoke());
        player.Looped.Add(() => entry.onLooped?.Invoke());
        player.Completed.Add(() => entry.onCompleted?.Invoke());
    }

    // ---------- Reflection helpers ----------

    private static void SetIsInitialized(AudioPlayer player, bool value)
    {
        if (isInitializedProp == null)
        {
            isInitializedProp = typeof(AudioPlayer).GetProperty("IsInitialized",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }
        isInitializedProp?.SetValue(player, value);
    }

    private static void SetRecycleOnComplete(AudioPlayer player, bool value)
    {
        if (recycleOnCompleteProp == null)
        {
            // Property getter is public, setter is private - GetProperty finds it under Public flag.
            recycleOnCompleteProp = typeof(AudioPlayer).GetProperty("RecycleOnComplete",
                BindingFlags.Public | BindingFlags.Instance);
        }
        recycleOnCompleteProp?.SetValue(player, value);
    }
}
