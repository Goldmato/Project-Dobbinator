using System.Collections;

using UnityEngine;

// TEST COMMENT

public class GameManager : MonoBehaviour
{
	// Field and properties
	IEnumerable m_GameState;

	public static GameManager Current { get { return m_Instance; } }
	private static GameManager m_Instance;

	public GameObject StartPlatform { get { return m_StartPlatform; } set { m_StartPlatform = value; } }

	public string LoadState { get { return m_LoadState; } set { m_LoadState = value; } }
	public float  LoadValue { get { return m_LoadValue; } set { m_LoadValue = value; } }
	public int Score { get { return m_CurrentScore; } }
	public int Streak { get { return m_CurrentStreak; } }

	[SerializeField] GameObject m_MainMenuPrefab;
	[SerializeField] GameObject m_LoadScreenPrefab;
	[SerializeField] GameObject m_GameCanvasPrefab;
	[SerializeField] GameObject m_ArenaPrefab;
	[SerializeField] GameObject m_Player;

	FirstPersonController m_PlayerController;
	GameObject  m_StartPlatform;
	GameCanvas m_GameCanvas;

	string m_LoadState;
	float  m_LoadValue;
	float  m_StreakBonus;
	int    m_CurrentStreak;
	int    m_CurrentScore;
	bool   m_TextIncrementing;

	// Constants
	const string TEXT_GROW = "grow_text";
	const float  STREAK_BONUS = 2.5f;

	// Unsubscribe from events
	void OnDisable() { if(m_PlayerController != null) m_PlayerController.OnPlayerGrounded -= OnPlayerGrounded; }

	void Start()
	{
		m_Instance = this;

		try
		{
			// Find player via tag 
			if(m_Player == null)
			{
				m_Player = GameObject.FindGameObjectWithTag("Player");
				m_PlayerController = m_Player.GetComponent<FirstPersonController>();
				m_PlayerController.OnPlayerGrounded += OnPlayerGrounded;
			}

			// Find the main menu, load screen, and arena through the resources system
			if(m_ArenaPrefab == null)
				m_ArenaPrefab = Resources.Load<GameObject>("Structures/arena_01");
			if(m_MainMenuPrefab == null)
				m_MainMenuPrefab = Resources.Load<GameObject>("UI/main_menu");
			if(m_LoadScreenPrefab == null)
				m_LoadScreenPrefab = Resources.Load<GameObject>("UI/loading_screen");
			if(m_GameCanvasPrefab == null)
				m_GameCanvasPrefab = Resources.Load<GameObject>("UI/game_canvas");
		}
		catch(System.NullReferenceException exception)
		{
			Debug.LogError("Please make sure all GameManager fields are assigned!");
			Debug.Break();
		}

		// Set the initial state and start the FSM 
		SetGameState(StartGame());
		StartCoroutine(FiniteStateMachine());
	}

	// Use this method instead of setting the state directly
	void SetGameState(IEnumerable state)
	{
		m_GameState = state;
	}

	void SetPlayerPosition(Vector3 pos)
	{
		m_Player.transform.position = pos;
	}

	void OnPlayerGrounded()
	{
		Debug.Log("Player Grounded!");
		ResetStreak();
	}

	// Add score and multiply it by a streak bonus
	public void AddScore(int score)
	{
		m_CurrentStreak++;
		m_StreakBonus = m_CurrentStreak * STREAK_BONUS;
		if(m_GameCanvas != null)
		{
			m_GameCanvas.ScoreAnimator.SetTrigger(TEXT_GROW);

			if (!m_TextIncrementing)
				StartCoroutine(IncrementScoreText(m_CurrentScore));
		}
		m_CurrentScore += Mathf.RoundToInt(score + m_StreakBonus);
	}

	public void ResetStreak()
	{
		m_CurrentStreak = 0;
	}

	public void ExitGame()
	{
		Application.Quit();
	}

	IEnumerator IncrementScoreText(int oldScore)
	{
		m_TextIncrementing = true;
		float scoreVelocity = 0f;
		float newScore = oldScore;
		int finalScore = 0;

		do
		{
			newScore = Mathf.SmoothDamp(newScore, m_CurrentScore, ref scoreVelocity, 1f);
			finalScore = Mathf.CeilToInt(newScore);
			m_GameCanvas.ScoreText.text = "Score: " + finalScore.ToString();
			yield return null;
		} while (finalScore != m_CurrentScore);
		m_TextIncrementing = false;
	}

	IEnumerator FiniteStateMachine()
	{
		// Keep running while there is a valid state
		while(m_GameState != null)
		{
			foreach(var current in m_GameState)
			{
				yield return current;
			}
		}
	}

	IEnumerable StartGame()
	{
		var menu = Instantiate(m_MainMenuPrefab) as GameObject;
		var menuScript = menu.GetComponent<MainMenu>();

		m_Player.SetActive(false);

		foreach(var current in GameStates.MainMenu(menuScript))
		{
			yield return current;
		}

		Destroy(menu);


		var loadScreen = Instantiate(m_LoadScreenPrefab) as GameObject;
		var loadScript = loadScreen.GetComponent<LoadingScreen>();
		var arena = Instantiate(m_ArenaPrefab) as GameObject;

		foreach(var current in GameStates.LoadingScreen(loadScript))
		{
			yield return current;
		}

		Destroy(loadScreen);


		// Set the player position to the start platform defined by the PlatformGenerator
		var tData = arena.GetComponent<DynamicTerrain>().GetTerrainData();
		m_Player.SetActive(true);
		SetPlayerPosition(m_StartPlatform != null ? new Vector3(m_StartPlatform.transform.position.x,
													  m_StartPlatform.transform.position.y + 50,
													  m_StartPlatform.transform.position.z) :
										            new Vector3(tData.size.x / 2, tData.size.y, tData.size.z / 2));

		SetGameState(PhaseOne());
	}

	IEnumerable PhaseOne()
	{
		var canvas = Instantiate(m_GameCanvasPrefab) as GameObject;
		m_GameCanvas = canvas.GetComponent<GameCanvas>();
		m_GameCanvas.ScoreText.text = "Score: 0";

		foreach(var current in GameStates.MainGame())
		{
			yield return current;
		}

		SetGameState(null);
	}
}