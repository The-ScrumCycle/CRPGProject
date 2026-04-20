using UnityEngine;
using State;
using Game.Combat;

public class VarekBossFight : MonoBehaviour
{
    private GameState state;

    private void Start()

    {
        Debug.Log("init bossfight script");
        state = GameState.Instance;
    }

    private void OnDestroy()
    {
        Debug.Log("Destroying");
        if (state == null)
            state = GameState.Instance;

        state.setFlag("varek_defeated");
    }

}
