using System.Linq;
using CarterGames.Assets.AudioManager;
using CarterGames.Assets.AudioManager.Editor;
using CarterGames.Shared.AudioManager.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// Custom inspector for AudioManager_System. Adds the searchable clip / group dropdown
/// (the same one Carter Games' InspectorAudioClipPlayer uses) to each entry's Request field.
/// </summary>
[CustomEditor(typeof(AudioManager_System))]
public sealed class AudioManager_SystemEditor : Editor
{
    // The search provider fires a single global callback; we track which SerializedProperty
    // to write into via these statics.
    private static SerializedObject targetObjRef;
    private static string targetPropertyPath;

    private static readonly string[] SingleGroupOptions = { "Single", "Group" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty entriesProp = serializedObject.FindProperty("entries");
        SerializedProperty warnProp = serializedObject.FindProperty("warnOnMissingKey");

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Audio Entries", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            if (!DrawEntry(entriesProp, i))
            {
                // Entry was removed - stop iterating to avoid indexing into a stale array.
                break;
            }
        }

        EditorGUILayout.Space(6);

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("+ Add Entry", GUILayout.Height(24)))
        {
            entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
            SerializedProperty added = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
            added.FindPropertyRelative("key").stringValue = string.Empty;
            added.FindPropertyRelative("request").stringValue = string.Empty;
            added.FindPropertyRelative("isGroup").boolValue = false;
            added.FindPropertyRelative("volume").floatValue = 1f;
            added.FindPropertyRelative("pitch").floatValue = 1f;
            added.isExpanded = true;
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.PropertyField(warnProp);

        serializedObject.ApplyModifiedProperties();
    }

    /// <returns>false if this entry was removed and iteration should stop.</returns>
    private bool DrawEntry(SerializedProperty entriesProp, int index)
    {
        SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);
        SerializedProperty keyProp = entry.FindPropertyRelative("key");
        SerializedProperty requestProp = entry.FindPropertyRelative("request");
        SerializedProperty isGroupProp = entry.FindPropertyRelative("isGroup");
        SerializedProperty volumeProp = entry.FindPropertyRelative("volume");
        SerializedProperty pitchProp = entry.FindPropertyRelative("pitch");

        EditorGUILayout.BeginVertical("HelpBox");

        // ---- Header: foldout + delete ----
        EditorGUILayout.BeginHorizontal();
        string title = string.IsNullOrEmpty(keyProp.stringValue) ? $"Entry {index}" : keyProp.stringValue;
        entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, title, true, EditorStyles.foldoutHeader);

        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("X", GUILayout.Width(24)))
        {
            entriesProp.DeleteArrayElementAtIndex(index);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
            return false;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (!entry.isExpanded)
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
            return true;
        }

        EditorGUILayout.Space(3);

        EditorGUILayout.PropertyField(keyProp);

        EditorGUILayout.Space(3);

        // ---- Request: Single | Group toolbar + searchable dropdown ----
        EditorGUILayout.LabelField("Request", EditorStyles.miniBoldLabel);
        int newIndex = GUILayout.Toolbar(isGroupProp.boolValue ? 1 : 0, SingleGroupOptions);
        isGroupProp.boolValue = newIndex == 1;

        DrawRequestField(requestProp, isGroupProp.boolValue);

        EditorGUILayout.Space(3);
        EditorGUILayout.PropertyField(volumeProp);
        EditorGUILayout.PropertyField(pitchProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Per-play Events", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("onStarted"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("onLooped"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("onCompleted"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3);
        return true;
    }

    private void DrawRequestField(SerializedProperty requestProp, bool isGroup)
    {
        EditorGUILayout.BeginHorizontal();

        string displayName = GetDisplayName(requestProp.stringValue, isGroup);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(displayName);
        EditorGUI.EndDisabledGroup();

        string btnLabel = string.IsNullOrEmpty(requestProp.stringValue)
            ? (isGroup ? "Select Group" : "Select Clip")
            : (isGroup ? "Change Group" : "Change Clip");

        if (GUILayout.Button(btnLabel, GUILayout.Width(110)))
        {
            OpenSearchProvider(requestProp, isGroup);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OpenSearchProvider(SerializedProperty requestProp, bool isGroup)
    {
        targetObjRef = serializedObject;
        targetPropertyPath = requestProp.propertyPath;

        if (isGroup)
        {
            SearchProviderInstancing.SearchProviderGroups.SelectionMade.Clear();
            SearchProviderInstancing.SearchProviderGroups.SelectionMade.Add(OnGroupSelected);
            SearchProviderInstancing.SearchProviderGroups.Open();
        }
        else
        {
            SearchProviderInstancing.SearchProviderLibrary.SelectionMade.Clear();
            SearchProviderInstancing.SearchProviderLibrary.SelectionMade.Add(OnClipSelected);
            SearchProviderInstancing.SearchProviderLibrary.Open();
        }
    }

    private static void OnClipSelected(SearchTreeEntry entry)
    {
        SearchProviderInstancing.SearchProviderLibrary.SelectionMade.Remove(OnClipSelected);
        if (targetObjRef == null) return;

        SerializedProperty prop = targetObjRef.FindProperty(targetPropertyPath);
        prop.stringValue = ((AudioData)entry.userData).id;
        targetObjRef.ApplyModifiedProperties();
        targetObjRef.Update();
    }

    private static void OnGroupSelected(SearchTreeEntry entry)
    {
        SearchProviderInstancing.SearchProviderGroups.SelectionMade.Remove(OnGroupSelected);
        if (targetObjRef == null) return;

        SerializedProperty prop = targetObjRef.FindProperty(targetPropertyPath);
        string groupName = ((GroupData)entry.userData).GroupName;

        var lib = ScriptableRef.GetAssetDef<AudioLibrary>().AssetRef;
        if (lib != null)
        {
            var match = lib.GroupsLookup.FirstOrDefault(kv => kv.Value.GroupName.Equals(groupName));
            prop.stringValue = match.Key ?? string.Empty;
        }

        targetObjRef.ApplyModifiedProperties();
        targetObjRef.Update();
    }

    private static string GetDisplayName(string idOrName, bool isGroup)
    {
        if (string.IsNullOrEmpty(idOrName)) return string.Empty;

        var lib = ScriptableRef.GetAssetDef<AudioLibrary>().AssetRef;
        if (lib == null) return idOrName;

        if (isGroup)
        {
            return lib.GroupsLookup != null && lib.GroupsLookup.ContainsKey(idOrName)
                ? lib.GroupsLookup[idOrName].GroupName
                : idOrName;
        }

        return lib.LibraryLookup != null && lib.LibraryLookup.ContainsKey(idOrName)
            ? lib.LibraryLookup[idOrName].key
            : idOrName;
    }
}
