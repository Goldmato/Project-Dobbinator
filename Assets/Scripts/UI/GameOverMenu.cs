using UnityEngine;

public class GameOverMenu : MonoBehaviour 
{
	const string MAIN_SCENE = "Main";

	public void OnRestartButtonClicked() 
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(MAIN_SCENE);
	}
}
