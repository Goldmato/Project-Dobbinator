﻿using UnityEngine;

public class SpringLauncher : MonoBehaviour, IPlatform
{
	// Fields and properties
	[SerializeField] [Range(0.001f, 1000f)] private float forceFactor = 100f;
	[SerializeField] [Range(0.001f, 5f)]    private float springDistance = 2.5f;
	[SerializeField] [Range(0.001f, 5f)]    private float springTime = 2.5f;
	[SerializeField] [Range(0.001f, 5f)]    private float scaleFactor = 1.5f;
	[SerializeField] [Range(0.001f, 2f)]    private float soundPitchLow = 0.5f;
	[SerializeField] [Range(0.001f, 2f)]    private float soundPitchHigh = 1.5f;

	[SerializeField] Transform m_PlatformBase;

	AudioSource m_AudioSource;
	Vector3	    m_TargetPos;
	Vector3     m_TargetScale;
	Vector3		m_OriginalPos;
	Vector3     m_OriginalScale;
	Vector3		m_MoveVelocity;
	Vector3     m_ScaleVelocity;
	byte		m_MovePlatform = DISABLED;

	static bool m_NullExceptionFlag;

	const byte  STAGE_ONE = 2, STAGE_TWO = 1, DISABLED = 0;

	void Start()
	{
		// Simple check and error handling for assigning the platform base transform
		try
		{
			if(m_PlatformBase == null)
				m_PlatformBase = transform.FindChild("platform_base");
			m_OriginalScale = m_PlatformBase.localScale;
		}
		catch(System.NullReferenceException exception)
		{
			Debug.Break();
			m_PlatformBase = this.transform;
			string errMessage = ("Please either assign a transform to the 'Platform Base' field or rename " +
						         "the relevant child object to 'platform_base'");
			if(!m_NullExceptionFlag)
			{
				m_NullExceptionFlag = true;
				throw new System.Exception(errMessage, exception);
			}
		}
		finally
		{
			m_OriginalPos = transform.position;
			m_AudioSource = GetComponent<AudioSource>();
		}
	}

	void FixedUpdate()
	{
		if(m_MovePlatform > DISABLED)
		{
			transform.position = Vector3.SmoothDamp(transform.position, m_TargetPos,
														   ref m_MoveVelocity, springTime);
			m_PlatformBase.localScale = Vector3.SmoothDamp(m_PlatformBase.localScale, m_TargetScale,
													  ref m_ScaleVelocity, springTime);

			if (Vector3.Distance(transform.position, m_TargetPos) <= 0.1f)
			{
				// Debug.Log("Platform reached end of Stage " + ((STAGE_ONE + 1) - m_MovePlatform));
				m_TargetPos = m_OriginalPos;
				m_TargetScale = m_OriginalScale;
				if(m_MovePlatform > DISABLED)
					m_MovePlatform--;
			}
		}
	}

	public void PlayerBoost(FirstPersonController character)
	{
		// If ApplyForce isn't on cooldown, play the main audio clip and 
		// apply a "spring" effect to the platform

		if(character.ApplyForce(transform.up * forceFactor))
		{
			m_AudioSource.pitch = Random.Range(soundPitchLow, soundPitchHigh);
			m_AudioSource.Play();
			if (m_MovePlatform <= DISABLED)
				PlatformSpringEffect();
			GameManager.Current.AddScore(10);
		}
	}

	void PlatformSpringEffect()
	{
		m_TargetPos  =  transform.position - (transform.up * springDistance);
		m_TargetScale = m_PlatformBase.localScale * scaleFactor;
		m_MoveVelocity = Vector3.zero;
		m_ScaleVelocity = Vector3.zero;
		m_MovePlatform = STAGE_ONE;
	}
}
