namespace Game.Combat.Actions
{
    /// <summary>
    /// Check ActionVisualType, this is an abstracted version of that enumerated type which defines actions into 
    /// larger categories to display on the visual layer (action intents).
    /// Higher numeric value overrides lower on the same tile, e.g HighlightType also defines a hierarchy of highlights.
    /// ex : green = movement, red = attack
    /// <summary>
    public enum HighlightType
    {
        None = 0,
        PlayerMove = 1,
        PlayerAttack = 2,
        AI_Move = 3,
        AI_Attack = 4
    }
}
