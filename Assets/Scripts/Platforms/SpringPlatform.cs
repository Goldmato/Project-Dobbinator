using System.Collections;

using UnityEngine;

public class SpringPlatform : Platform
{
	// Fields and properties
	[SerializeField] [Range(0.001f, 100f)]  private float m_DisableDelay = 10f;
	[SerializeField] [Range(0.001f, 5f)]    private float m_SpringDistance = 2.5f;
	[SerializeField] [Range(0.001f, 5f)]    private float m_SpringTime = 2.5f;
	[SerializeField] [Range(0.001f, 5f)]    private float m_ScaleFactor = 1.5f;

	Vector3	    m_TargetPos;
	Vector3     m_TargetScale;
	Vector3		m_OriginalPos;
	Vector3     m_OriginalScale;
	Vector3		m_MoveVelocity;
	Vector3     m_ScaleVelocity;
	byte		m_MovePlatform = DISABLED;

	const byte  STAGE_ONE = 2, STAGE_TWO = 1, DISABLED = 0;
	const float MIN_ALPHA = 0.5f;

	protected override void Start()
	{
		base.Start();
		m_OriginalScale = m_PlatformBase.localScale;
		m_OriginalPos = transform.position;
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
				if(m_Renderer[i].material.color.a > MIN_ALPHA)  
				{
					m_Renderer[i].material.color = new Color(m_Renderer[i].material.color.r, m_Renderer[i].material.color.g,
															m_Renderer[i].material.color.b, m_Renderer[i].material.color.a - 0.01f);
				}
			}

			if (Vector3.Distance(transform.position, m_TargetPos) <= 0.1f)
			{
				// Debug.Log("Platform reached end of Stage " + ((STAGE_ONE + 1) - m_MovePlatform));
				m_TargetPos = m_OriginalPos;
				m_TargetScale = m_OriginalScale;
				m_MovePlatform--;
			}
		}
	}

	public override void PlayerBoost(FirstPersonController character)
	{
		// If ApplyForce isn't on cooldown, play the main audio clip and 
		// apply a "spring" effect to the platform and disable the collider
		// and shadow rendering
		if(character.ApplyForce(transform.up * m_ForceFactor))
		{
			if(m_MovePlatform <= DISABLED)
				StartSpringEffect();
			StartCoroutine(DisablePlatform());
			GameManager.Current.AddScore(m_ScoreValue, m_PlatformType);
			m_AudioSource.pitch = Random.Range(m_SoundPitchLow, m_SoundPitchHigh);
			m_AudioSource.Play();
		}
	}

	void StartSpringEffect()
	{
		m_TargetPos  =  transform.position - (transform.up * m_SpringDistance);
		m_TargetScale = m_PlatformBase.localScale * m_ScaleFactor;
		m_MoveVelocity = Vector3.zero;
		m_ScaleVelocity = Vector3.zero;
		m_MovePlatform = STAGE_ONE;
	}

    IEnumerator DisablePlatform() 
	{
		var col = m_PlatformBase.GetComponent<Collider>();

		// Disable shadows and collisions
		for(int i = 0; i < m_Renderer.Length; i++)
		{
			m_Renderer[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		}

		col.enabled = false;
		yield return new WaitForSeconds(m_DisableDelay);
		col.enabled = true;

		// Set transparency of all renderers back to max
		for(int i = 0; i < m_Renderer.Length; i++)
		{
			m_Renderer[i].material.color = new Color(m_Renderer[i].material.color.r, m_Renderer[i].material.color.g,
													 m_Renderer[i].material.color.b, 1f);
		}
	}
}
