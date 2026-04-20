using UnityEngine;
using UnityEngine.SceneManagement;

public class DestroyController : MonoBehaviour
{
    [SerializeField] private string[] scenesWhereItShouldDie;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (string sceneName in scenesWhereItShouldDie)
        {
            if (scene.name == sceneName)
            {
                Destroy(gameObject);
                return;
            }
        }

        
    }

}