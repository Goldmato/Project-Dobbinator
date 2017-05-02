using UnityEngine;
using UnityEngine.UI;

public class GameCanvas : MonoBehaviour
{
	// Properties
	public Text ScoreText { get { return m_ScoreText; } }
	public Animator ScoreAnimator { get { return m_ScoreAnimator; } }

	// Serialized fields visible in the inspector
	[SerializeField] private Text	  m_ScoreText;
	[SerializeField] private Text     m_StreakText;
	[SerializeField] private Animator m_ScoreAnimator;
	[SerializeField] private Slider   m_StreakBar;

	// Private fields
	float m_SliderHeight;
	int   m_OldStreak;

	// Constants
	const string STREAK_BACKGND_NAME = "streak_background";
	const string SCORE_TEXT_NAME = "score_text";
	const string STREAK_TEXT_NAME = "streak_text";
	const string STREAK_SLIDER_NAME = "slider_bar";
	const float  STREAK_SUB_VALUE = 0.005f;

	void Start()
	{
		if(m_ScoreText == null)
		{
			try
			{
				var scoreTF = transform.FindChild(SCORE_TEXT_NAME);
				m_ScoreText = scoreTF.GetComponent<Text>();
				m_ScoreAnimator = scoreTF.parent.GetComponent<Animator>();
			}
			catch(System.NullReferenceException exception)
			{
				CreateMissingChildException(exception, SCORE_TEXT_NAME);
			}
		}
		if(m_StreakBar == null)
		{
			try
			{
				m_StreakBar = transform.FindChild(STREAK_SLIDER_NAME).GetComponent<Slider>();
				m_StreakText = transform.FindChild(STREAK_TEXT_NAME).GetComponent<Text>();
			}
			catch(System.NullReferenceException exception)
			{
				CreateMissingChildException(exception, STREAK_SLIDER_NAME);
			}
		}

		m_SliderHeight = m_StreakBar.fillRect.rect.height;
		m_OldStreak = GameManager.Current.Streak;
		ResetStreakBar();
	}

	void FixedUpdate()
	{
		// Lower the bar every frame
		if(GameManager.Current.Streak != 0)
		{
			m_StreakBar.value -= STREAK_SUB_VALUE;
			Vector2 textPos = new Vector2(0, m_SliderHeight * ((m_StreakBar.value - 0.5f) * 0.8f));
			m_StreakText.rectTransform.localPosition = textPos;
			if(GameManager.Current.Streak > m_OldStreak)
			{
				RefillStreakBar(GameManager.Current.Streak);
			}
		}
		else
		{
			// Reset the streak bonus
			if(GameManager.Current.Streak != 0)
				GameManager.Current.ResetStreak();
			ResetStreakBar();
		}

		m_OldStreak = GameManager.Current.Streak;
	}

	void RefillStreakBar(int streak)
	{
		m_StreakBar.value = 1f;
		m_StreakText.text = streak.ToString();
	}

	void ResetStreakBar()
	{
		m_StreakBar.value = 0;
		m_StreakText.text = "0";
		m_StreakText.rectTransform.localPosition = new Vector2(0, m_SliderHeight * -0.5f * 0.8f);
	}

	void CreateMissingChildException(System.Exception exception, string childName)
	{
		var errMessage = string.Format("Could not find child with name '{0}' \n." +
											   "Please rename the object or assign it in the inspector", childName);
		throw new UnityException(errMessage, exception);
	}
}
