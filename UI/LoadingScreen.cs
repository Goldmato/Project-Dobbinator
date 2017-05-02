using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
	// Fields and properties
	public Text   loadingText;
	public Slider loadingBar;

	public bool ExitButtonPressed { get { return m_ExitTrigger; } }

	bool m_ExitTrigger;

	void Start()
	{
		if(loadingText == null)
			loadingText = transform.Find("loading_text").GetComponent<Text>();
		if(loadingBar == null)
			loadingBar = transform.Find("loading_bar").GetComponent<Slider>();
	}

	public void OnExitButton()
	{
		m_ExitTrigger = true;
	}
}
