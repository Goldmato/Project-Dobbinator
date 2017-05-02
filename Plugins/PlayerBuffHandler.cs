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
	void OnEnable() { GameStates.OnScoreEvent += ScoreEventHandler; }

	// Unsubscribe from all events
	void OnDisable() { GameStates.OnScoreEvent -= ScoreEventHandler; }

	void ScoreEventHandler(int score)
	{
		// Do stuff to the player here, most likely just adding/multiplying the 
		// player's stats by a another predefined stat variable
		m_Player.Stats += m_BonusStats;
	}

}
