using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Data (optional)")]
    [Tooltip("If assigned, its CurrentSceneIndex / CurrentSceneName debug fields are updated each load.")]
    [SerializeField] private SceneLoader_ScriptableObject sceneData;

    /// <summary>
    /// Loads a scene by its build index asynchronously. Wire this to a Unity Button's OnClick
    /// and set the int parameter to the scene's build index.
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        if (sceneData != null)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            sceneData.SetCurrentScene(sceneIndex, sceneName);
        }

        SceneManager.LoadSceneAsync(sceneIndex);
    }
}
