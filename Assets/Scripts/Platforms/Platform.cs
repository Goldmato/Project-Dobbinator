using System.Collections;

using UnityEngine;

public enum PlatformType : byte { Ground, Sky }

[RequireComponent(typeof(AudioSource))]
public class Platform : MonoBehaviour 
{
	[SerializeField] protected Transform m_PlatformBase;
	[SerializeField] protected PlatformType m_PlatformType;

	[SerializeField] [Range(1, 1000)] 		protected int   m_ScoreValue = 10;
	[SerializeField] [Range(0.001f, 1000f)] protected float m_ForceFactor = 100f;
	[SerializeField] [Range(0.001f, 2f)]    protected float m_SoundPitchLow = 0.5f;
	[SerializeField] [Range(0.001f, 2f)]    protected float m_SoundPitchHigh = 1.5f;

	protected AudioSource  m_AudioSource;
	protected Renderer[]   m_Renderer;

	static bool m_NullExceptionFlag;

	protected virtual void Start()
	{
		// Simple check and error handling for assigning the platform base transform
		try
		{
			if(m_PlatformBase == null)
				m_PlatformBase = transform.FindChild("platform_base");
		}
		catch(System.NullReferenceException exception)
		{
			Debug.Break();
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
			m_AudioSource = GetComponent<AudioSource>();
			m_Renderer = GetComponentsInChildren<Renderer>();
		}
	}

	public virtual void PlayerBoost(FirstPersonController character)
	{
		if(character.ApplyForce(transform.up * m_ForceFactor))
		{
			GameManager.Current.AddScore(m_ScoreValue, m_PlatformType);
			m_AudioSource.pitch = Random.Range(m_SoundPitchLow, m_SoundPitchHigh);
			m_AudioSource.Play();
		}
	}
}
