using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ToggleGroup))]
public class MainMenu : MonoBehaviour
{
	// Fields and properties
	[SerializeField] Toggle m_EasyToggle, m_MediumToggle, m_HardToggle;

	string m_Difficulty;

	const string MAIN_SCENE = "Main";
	public const string DIFF_KEY = "Difficulty";

	void Start() 
	{
		m_Difficulty = PlayerPrefs.GetString(DIFF_KEY, "Medium");
		switch(m_Difficulty) 
		{
			case "Easy":
				m_EasyToggle.isOn = true;
				break;
			case "Medium":
				m_MediumToggle.isOn = true;
				break;
			case "Hard":
				m_HardToggle.isOn = true;
				break;
		}
	}

	public void OnPlayButton()
	{
		PlayerPrefs.SetString(DIFF_KEY, m_Difficulty);
		PlayerPrefs.Save();
		SceneManager.LoadScene(MAIN_SCENE);
	}

	public void OnExitButton() { Application.Quit(); }

	public void EasyToggleHandler(bool state) { if(state) m_Difficulty = "Easy"; }

	public void MediumToggleHandler(bool state) { if(state) m_Difficulty = "Medium"; }

	public void HardToggleHandler(bool state) { if(state) m_Difficulty = "Hard"; }
}
