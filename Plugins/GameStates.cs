using System.Collections;

using UnityEngine;

// Static public container for game states
public static class GameStates
{
	// Static public properties
	public static int SecondsLeft { get { return m_SecondsLeft; } }

	// Static public fields
	public static bool Running = true;

	// Static delegates and events
	public delegate void ValueEventHandler(int value);
	public static event ValueEventHandler OnScoreBreakpoint;
	public static event ValueEventHandler OnTimerChange;

	// Static private fields
	private static int m_SecondsLeft;
	private static int m_ScoreBreakpoint = SCORE_INTERVAL;

	// Constants
	const int SCORE_INTERVAL = 50;
	const string GAME_OVER_SCENE = "GameOver";

	public static IEnumerable MainMenu(MainMenu menu)
	{
		while(Running)
		{
			if(menu.PlayButtonPressed)
			{
				break;
			}
			if(menu.ExitButtonPressed)
			{
				GameManager.Current.ExitGame();
			}
			yield return null;
		}
	}

	public static IEnumerable LoadingScreen(LoadingScreen loadScreen) 
	{
		var progress = "";
		var barSpeed = 0f;

		while(Running)
		{
			if(loadScreen.ExitButtonPressed)
			{
				GameManager.Current.ExitGame();
			}
			if(progress != GameManager.Current.LoadState)
			{
				progress = GameManager.Current.LoadState;
				loadScreen.loadingText.text = progress;
			}

			// Transition smoothly to the target loading bar value
			loadScreen.loadingBar.value = Mathf.SmoothDamp(loadScreen.loadingBar.value, 
															GameManager.Current.LoadValue, ref barSpeed, 0.25f);
			if(Mathf.Approximately(loadScreen.loadingBar.value, 1f))
				break;

			yield return null;
		}
			
	}

	public static IEnumerable MainGame(int startSeconds)
	{
		m_SecondsLeft = startSeconds;
		float secondBreakpoint = Time.timeSinceLevelLoad;

		while(Running)
		{
			// Check if a second has passed
			if(Time.timeSinceLevelLoad >= secondBreakpoint)
			{
				secondBreakpoint = Time.timeSinceLevelLoad + 1;
				m_SecondsLeft--;
				if(OnTimerChange != null)
				{
					OnTimerChange(m_SecondsLeft);
				}
			}

			// End the game if the timer reaches 0
			if(m_SecondsLeft <= 0)
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene(GAME_OVER_SCENE);
			}

			if(GameManager.Current.Score >= m_ScoreBreakpoint)
			{
				if(OnScoreBreakpoint != null)
				{
					// Debug.Log("OnScoreEvent() Called!");
					OnScoreBreakpoint(GameManager.Current.Score);
				}
				m_ScoreBreakpoint += SCORE_INTERVAL;
			}

			yield return null;
		}
	}
}