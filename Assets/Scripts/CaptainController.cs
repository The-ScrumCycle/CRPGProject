using UnityEngine;

public class CaptainController : MonoBehaviour
{

    [Header("Components")]
    public GameObject Player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // when I click on captain , I should be able to load in the boat
    void OnMouseDown ()
    {
        if (Vector3.Distance(Player.transform.position, transform.position) > 10f)
        {
            return;
        }

        Debug.Log("clicked");

       


    }



}
