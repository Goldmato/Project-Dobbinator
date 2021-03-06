﻿using UnityEngine;
using UnityEngine.UI;

public class GameCanvas : MonoBehaviour
{
	// Properties
	public Text ScoreText { get { return m_ScoreText; } }
	public Animator ScoreAnimator { get { return m_ScoreAnimator; } }

	// Serialized fields visible in the inspector
	[SerializeField] private Text	  m_ScoreText;
	[SerializeField] private Text     m_StreakText;
	[SerializeField] private Text     m_TimerText;
	[SerializeField] private Animator m_ScoreAnimator;
	[SerializeField] private Slider   m_StreakBar;

	// Private fields
	string m_Minutes;
	string m_Seconds;
	float  m_SliderHeight;
	int    m_OldStreak;

	// Constants
	const float  STREAK_SUB_VALUE = 0.005f;

	// Subscribe/Unsubscribe from events
	void OnEnable() { GameStates.OnTimerChange += UpdateTimerText; }
	void OnDisable() { GameStates.OnTimerChange -= UpdateTimerText; }

	void Start()
	{
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

	void UpdateTimerText(int seconds)
	{
		System.TimeSpan ts = System.TimeSpan.FromSeconds(seconds);
		m_Seconds = ts.Seconds.ToString();

		if(ts.Seconds < 10)
			m_Seconds = "0" + m_Seconds;

		if(ts.Minutes == 0)
			m_TimerText.text = m_Seconds;
		else
		{
			m_Minutes = ts.Minutes.ToString();
			m_TimerText.text = string.Format("{0}:{1}", m_Minutes, m_Seconds);
		}
	}
}
