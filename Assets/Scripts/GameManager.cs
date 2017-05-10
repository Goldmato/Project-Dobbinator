﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class GameManager : MonoBehaviour
{
	// Field and properties
	IEnumerable m_GameState;

	public static GameManager Current { get { return m_Instance; } }
	private static GameManager m_Instance;

	public GameObject StartPlatform { get { return m_StartPlatform; } set { m_StartPlatform = value; } }
	public GameObject PortalPlatform { get { return m_PortalPlatform; } set { m_PortalPlatform = value; } }

	public string LoadState { get { return m_LoadState; } set { m_LoadState = value; } }
	public float  LoadValue { get { return m_LoadValue; } set { m_LoadValue = value; } }
	public float  DifficultyFactor { get { return m_DiffFactor; } }
	public int Score { get { return m_CurrentScore; } }
	public int Streak { get { return m_CurrentStreak; } }

	// Private serialized fields
	[SerializeField] [Range(1, 1000)] private int m_ArenaTimer = 300;

	[SerializeField] [Range(1, 5)] private float m_EasyDiffFactor = 1f;
	[SerializeField] [Range(1, 5)] private float m_MediumDiffFactor = 2.5f;
	[SerializeField] [Range(1, 5)] private float m_HardDiffFactor = 5f;
	[SerializeField] [Range(1, 5)] private float m_GameOverDelay = 0.5f;

	[SerializeField] private GameObject m_LoadScreenPrefab;
	[SerializeField] private GameObject m_GameCanvasPrefab;
	[SerializeField] private GameObject m_ArenaPrefab;
	[SerializeField] private GameObject m_PortalPrefab;
	[SerializeField] private GameObject m_Player;

	List<GameObject> m_Arena = new List<GameObject>();
	FirstPersonController m_PlayerController;
	GameObject m_StartPlatform;
	GameObject m_PortalPlatform;
	GameCanvas m_GameCanvas;
	Portal     m_CurrentPortal;

	string m_LoadState;
	float  m_LoadValue;
	float  m_DiffFactor;
	float  m_StreakBonus;
	int    m_CurrentStreak;
	int    m_CurrentScore;
	int    m_MinScore = 10000;
	int    m_ArenaIndex;
	int    m_LastPlatformID;
	bool   m_TextIncrementing;

	// Delegates and events
	public delegate void DefaultEventHandler();
	public static event DefaultEventHandler OnResetStreak;

	// Constants
	const string GAME_OVER_SCENE = "GameOver";
	const string TEXT_GROW = "grow_text";
	const float  STREAK_BONUS = 2.5f;
	
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
			}

			// Find all necessary resources if not assigned
			if(m_ArenaPrefab == null)
				m_ArenaPrefab = Resources.Load<GameObject>("Structures/arena_01");
			if(m_PortalPrefab == null)
				m_PortalPrefab = Resources.Load<GameObject>("Structures/portal_01");
			if(m_LoadScreenPrefab == null)
				m_LoadScreenPrefab = Resources.Load<GameObject>("UI/loading_screen");
			if(m_GameCanvasPrefab == null)
				m_GameCanvasPrefab = Resources.Load<GameObject>("UI/game_canvas");
		}
		catch(System.NullReferenceException exception)
		{
			throw new UnityException("Please make sure all GameManager fields are assigned!", exception);
		}

		// Set the difficulty factor
		switch(PlayerPrefs.GetString(MainMenu.DIFF_KEY, "Medium"))
		{
			case "Easy":
				m_DiffFactor = m_EasyDiffFactor;
				break;
			case "Medium":
				m_DiffFactor = m_MediumDiffFactor;
				break;
			case "Hard":
				m_DiffFactor = m_HardDiffFactor;
				break;
		}

		// Set the initial state and start the FSM 
		Debug.Log("Current difficulty factor: " + m_DiffFactor);
		SetGameState(LoadScreen());
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

	// Add score and multiply it by a streak bonus
	public void AddScore(int score, PlatformType platType, int platformID)
	{
		// First check if the player is jumping on the same platform
		if(platformID == m_LastPlatformID) 
			return;
		m_LastPlatformID = platformID;

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

	public void ResetStreak(bool callEvent = false, bool resetStreak = true)
	{
		if(callEvent && OnResetStreak != null)
			OnResetStreak();
		if(resetStreak)
			m_CurrentStreak = 0;
	}

	public void EndGame() 
	{
		StartCoroutine(GameOver());
	}

	public void ExitGame()
	{
		Application.Quit();
	}

	IEnumerator GameOver()
	{
		yield return new WaitForSeconds(m_GameOverDelay);
		UnityEngine.SceneManagement.SceneManager.LoadScene(GAME_OVER_SCENE);
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
		} while (finalScore != m_CurrentScore && m_GameCanvas.ScoreText != null);
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

	IEnumerable LoadScreen()
	{
		m_Player.SetActive(false);
		var loadScreen = Instantiate(m_LoadScreenPrefab) as GameObject;
		var loadScript = loadScreen.GetComponent<LoadingScreen>();

		var arenaSpawnPos = Vector3.zero;
		if(m_ArenaIndex > 0)
		{
			var prevArenaPos = m_Arena[m_ArenaIndex - 1].transform.position;
			var prevArenaSize = m_Arena[m_ArenaIndex - 1].GetComponent<Collider>().bounds.size;

			arenaSpawnPos = prevArenaPos;

			if(Random.value > 0.5f)
				arenaSpawnPos.z += prevArenaSize.z;
			else
				arenaSpawnPos.x += prevArenaSize.x;
		}

		m_Arena.Add(Instantiate(m_ArenaPrefab, arenaSpawnPos, Quaternion.identity) as GameObject);

		foreach(var current in GameStates.LoadingScreen(loadScript))
		{
			yield return current;
		}

		Destroy(loadScreen);
		// Set the player position to the start platform defined by the PlatformGenerator
		m_Player.SetActive(true);

		if(m_ArenaIndex == 0)
		{
			var tData = m_Arena[m_ArenaIndex].GetComponent<DynamicTerrain>().GetTerrainData();
			SetPlayerPosition(m_StartPlatform != null ? new Vector3(m_StartPlatform.transform.position.x,
														  m_StartPlatform.transform.position.y + 50,
														  m_StartPlatform.transform.position.z) :
														new Vector3(tData.size.x / 2, tData.size.y, tData.size.z / 2));

		}

		// Spawn a single portal at the platform set by PlatformGenerator
		if(PortalPlatform != null)
		{
			// Remove the platform script from the portal platform
			Destroy(PortalPlatform.GetComponent<Platform>());

			m_CurrentPortal = Instantiate(m_PortalPrefab, PortalPlatform.transform.position,
										  PortalPlatform.transform.rotation).GetComponent<Portal>();
			
			// Move the portal above the platform based on its height
			m_CurrentPortal.transform.Translate(new Vector3(0, m_CurrentPortal.GetComponent<Collider>().bounds.size.y, 0));

			m_CurrentPortal.OnPortalActivated += () => { GameStates.Running = false; };
			
			Debug.Log("Portal spawned at:" + PortalPlatform.transform.position);
		}

		SetGameState(MainGame());
	}

	IEnumerable MainGame()
	{
		var canvas = Instantiate(m_GameCanvasPrefab) as GameObject;
		m_GameCanvas = canvas.GetComponent<GameCanvas>();
		m_GameCanvas.ScoreText.text = "Score: 0";

		foreach(var current in GameStates.MainGame(m_ArenaTimer, 50, 25))
		{
			yield return current;
		}

		// If there's a portal, remove the public gameObject reference
		if(m_CurrentPortal != null) 
		{
			m_PortalPlatform = null;
		}

		Destroy(canvas);
		m_Player.SetActive(false);
		m_MinScore *= 2;

		m_ArenaIndex++;
		SetGameState(LoadScreen());
	}
}
