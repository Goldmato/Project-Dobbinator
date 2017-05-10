using UnityEngine;

public class GameOverMenu : MonoBehaviour 
{
	const string MAIN_SCENE = "Main";

	void Start() 
	{
		// Re-enable the cursor 
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void OnRestartButtonClicked() 
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(MAIN_SCENE);
	}
}
