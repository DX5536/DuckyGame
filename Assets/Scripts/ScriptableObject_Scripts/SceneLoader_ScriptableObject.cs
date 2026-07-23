using UnityEngine;

[CreateAssetMenu(fileName = "SceneLoader", menuName = "ScriptableObject/SceneLoader", order = 4)]
public class SceneLoader_ScriptableObject : ScriptableObject
{
    [Header("Current Scene Index (get only - for debug)")]
    [SerializeField] private int currentSceneIndex;
    public int CurrentSceneIndex
    {
        get { return currentSceneIndex; }
    }

    [Header("Current Scene Name (get only - for debug)")]
    [SerializeField] private string currentSceneName;
    public string CurrentSceneName
    {
        get { return currentSceneName; }
    }

    [Header("Scene Indices")]
    [Tooltip("List of scene build indices this loader can jump between.")]
    [SerializeField] private int[] sceneIndices;
    public int[] SceneIndices
    {
        get { return sceneIndices; }
    }

    // Called by SceneLoader after a load request so the debug fields stay in sync.
    public void SetCurrentScene(int index, string name)
    {
        currentSceneIndex = index;
        currentSceneName = name;
    }
}
