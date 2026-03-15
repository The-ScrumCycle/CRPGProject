using UnityEngine;
using State;

public class ClarissaBossFight : MonoBehaviour
{
    private GameState state;

    private void Start()
    {
        state = GameState.Instance;
    }

    // activate flag on destroy
    private void OnDestroy()
    {
        if(state == null)
        {
            state = GameState.Instance;
        }

        this.state.setFlag("ogreBossDestroyed");
    }
}
