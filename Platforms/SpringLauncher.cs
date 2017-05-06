using UnityEngine;

public class SpringLauncher : MonoBehaviour, IPlatform
{
	// Fields and properties
	[SerializeField] [Range(1, 1000)] private int m_ScoreValue = 10;

	[SerializeField] [Range(0.001f, 1000f)] private float m_ForceFactor = 100f;
	[SerializeField] [Range(0.001f, 5f)]    private float m_SpringDistance = 2.5f;
	[SerializeField] [Range(0.001f, 5f)]    private float m_SpringTime = 2.5f;
	[SerializeField] [Range(0.001f, 5f)]    private float m_ScaleFactor = 1.5f;
	[SerializeField] [Range(0.001f, 2f)]    private float m_SoundPitchLow = 0.5f;
	[SerializeField] [Range(0.001f, 2f)]    private float m_SoundPitchHigh = 1.5f;

	[SerializeField] Transform m_PlatformBase;

	AudioSource m_AudioSource;
	Renderer[]	m_Renderer;

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
			m_Renderer = GetComponentsInChildren<Renderer>();
		}
	}

	void FixedUpdate()
	{
		if(m_MovePlatform > DISABLED)
		{
			transform.position = Vector3.SmoothDamp(transform.position, m_TargetPos,
														   ref m_MoveVelocity, m_SpringTime);
			m_PlatformBase.localScale = Vector3.SmoothDamp(m_PlatformBase.localScale, m_TargetScale,
													  ref m_ScaleVelocity, m_SpringTime);

			for(int i = 0; i < m_Renderer.Length; i++)
			{
				m_Renderer[i].material.color = new Color(m_Renderer[i].material.color.r, m_Renderer[i].material.color.g,
														 m_Renderer[i].material.color.b, m_Renderer[i].material.color.a - 0.01f);
				if(m_Renderer[i].material.color.a <= 0.01f)
					m_PlatformBase.GetComponent<Collider>().enabled = false;
			}

			if (Vector3.Distance(transform.position, m_TargetPos) <= 0.1f)
			{
				// Debug.Log("Platform reached end of Stage " + ((STAGE_ONE + 1) - m_MovePlatform));
				m_TargetPos = m_OriginalPos;
				m_TargetScale = m_OriginalScale;
				m_MovePlatform--;
				if(m_MovePlatform <= DISABLED)
					Destroy(gameObject);
			}
		}
	}

	public void PlayerBoost(FirstPersonController character)
	{
		// If ApplyForce isn't on cooldown, play the main audio clip and 
		// apply a "spring" effect to the platform
		if(character.ApplyForce(transform.up * m_ForceFactor))
		{
			// Disable shadows and collisions
			for(int i = 0; i < m_Renderer.Length; i++)
			{
				m_Renderer[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			}
			m_PlatformBase.GetComponent<Collider>().enabled = false;

			m_AudioSource.pitch = Random.Range(m_SoundPitchLow, m_SoundPitchHigh);
			m_AudioSource.Play();
			if(m_MovePlatform <= DISABLED)
				PlatformSpringEffect();
			GameManager.Current.AddScore(m_ScoreValue);
		}
	}

	void PlatformSpringEffect()
	{
		m_TargetPos  =  transform.position - (transform.up * m_SpringDistance);
		m_TargetScale = m_PlatformBase.localScale * m_ScaleFactor;
		m_MoveVelocity = Vector3.zero;
		m_ScaleVelocity = Vector3.zero;
		m_MovePlatform = STAGE_ONE;
	}
}
