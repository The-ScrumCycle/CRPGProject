using UnityEngine;
using System.Collections.Generic;

namespace Game.Exploration
{
    /// <summary>
    /// Attached to a monster in the overworld to define what units spawn alongside it in combat
    /// e.g determines a list of enemies that appear in combat vs the player
    /// </summary>
    public class EncounterConfig : MonoBehaviour
    {
        [Header("Combat Encounter Setup")]
        [Tooltip("List the exact prefab tags to spawn (e.g skeleton_melee, skeleton_ranged, hydra, healer)")]
        public List<string> additionalEnemies = new List<string>();
    }
}
