using System.Collections;

using UnityEngine;

// Public container for game states
public static class GameStates
{
	// Static fields and properties
	public static bool Running = true;

	static int m_ScoreBreakpoint = SCORE_INTERVAL;

	// Static delegates and events
	public delegate void ScoreEventHandler(int score);
	public static event ScoreEventHandler OnScoreEvent;

	// Constants
	const int SCORE_INTERVAL = 50;

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

	public static IEnumerable MainGame()
	{
		while(Running)
		{
			if(GameManager.Current.Score >= m_ScoreBreakpoint)
			{
				// Call the main score Event if there are listeners
				if(OnScoreEvent != null)
				{
					Debug.Log("OnScoreEvent() Called!");
					OnScoreEvent(GameManager.Current.Score);
				}
				m_ScoreBreakpoint += SCORE_INTERVAL;
			}

			yield return null;
		}
	}
}