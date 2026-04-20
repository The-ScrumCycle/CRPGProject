using UnityEngine;
using Game.Core;
using System.Data.Common;
using Game.Core.Transitions;

public class CleanUpController : MonoBehaviour
{
    public static CleanUpController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); //so we dont get stale singletons in non exploration scenes
    }

    // destroy player, sceneManager, PartyManager and music controller.
    public void CleanUp()
    {
        if (PlayerController.Instance != null)
            Destroy(PlayerController.Instance.gameObject);

        if (GameStateManager.Instance != null)
            Destroy(GameStateManager.Instance.gameObject);

        if (PartyManager.Instance != null)
            Destroy(PartyManager.Instance.gameObject);

        if (MusicController.Instance != null)
            Destroy(MusicController.Instance.gameObject);

        if (CombatTransitionData.Instance != null)
            Destroy(CombatTransitionData.Instance.gameObject);
    }

   
}
