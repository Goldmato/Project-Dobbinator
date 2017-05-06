using UnityEngine;

[RequireComponent(typeof(FirstPersonController))]
public class PlayerBuffHandler : MonoBehaviour
{
	// Serialized fields viewable in the inspector
	[SerializeField] PlayerStats m_BonusStats;

	// Private references
	FirstPersonController m_Player;

	void Start()
	{
		m_Player = GetComponent<FirstPersonController>();
	}

	// Subscribe to necessary events 
	void OnEnable() { GameStates.OnScoreBreakpoint += BoostPlayerStats; 
					  GameManager.OnResetStreak += ResetPlayerStats; }

	// Unsubscribe from all events
	void OnDisable() { GameStates.OnScoreBreakpoint -= BoostPlayerStats; 
					   GameManager.OnResetStreak -= ResetPlayerStats; }

	void BoostPlayerStats(int score)
	{
		// Do stuff to the player here, most likely just adding/multiplying the 
		// player's stats by a another predefined stat variable
		m_Player.Stats += m_BonusStats;
	}

	void ResetPlayerStats() 
	{
		// Reset the players stats
		m_Player.Stats = m_Player.OriginalStats;
	}

}
