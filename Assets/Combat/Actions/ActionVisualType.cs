namespace Game.Combat.Actions
{
    ///<summary>
    /// Enumerated types of actions used for defining the type of visualizing action on the visual grid layer
    /// We may move away from this design of seperating HighlightType and ActionVisualType if later we find
    /// that each ActionVisualType should be its own HighlightType, but currently I've seperated it since actions
    /// can have a super type e.g a HighLightType that highlights actions the same way
    ///<summary>
    public enum ActionVisualType
    {
        None,
        Move,
        MeleeAttack,
        RangedAttack,
        Grapple,
        Heal,
        Push,
        Pull
    }
}
