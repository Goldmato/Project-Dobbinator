using UnityEngine;

public class MainMenu : MonoBehaviour
{
	// Fields and properties
	public bool PlayButtonPressed { get { return m_playTrigger; } }
	public bool ExitButtonPressed { get { return m_exitTrigger; } }

	bool m_playTrigger;
	bool m_exitTrigger;

	public void OnPlayButton()
	{
		m_playTrigger = true;
	}

	public void OnExitButton()
	{
		m_exitTrigger = true;
	}
}
