using UnityEngine;

public class CombatController : MonoBehaviour
{
    [Header("Grid Options")]
    [SerializeField] private HexGrid grid;

    [Header("Entity Options")]
    [SerializeField] GameObject characterObj;
    [SerializeField] Vector2Int startPos;
    [SerializeField] Vector2Int startPos2;

    private bool activeCharacter = false;

    private GridEntity character;
    private GridEntity character2;

    void Start()
    {
        character = new GridEntity(
            grid, 
            Instantiate(characterObj, grid.GridToWorld(startPos), Quaternion.identity), 
            startPos);
        grid.AddOccupant(character);

        character2 = new GridEntity(
            grid, 
            Instantiate(characterObj, grid.GridToWorld(startPos2), Quaternion.identity), 
            startPos2);
        grid.AddOccupant(character2);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            activeCharacter = !activeCharacter;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (activeCharacter)
            {
                character.Move(grid.GetActiveHexPos());
            }
            else
            {
                character2.Move(grid.GetActiveHexPos());
            }
        }
    }
}
