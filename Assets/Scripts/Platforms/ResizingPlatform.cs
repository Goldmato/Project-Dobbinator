using UnityEngine;

public class ResizingPlatform : Platform
{
	// Fields and properties
	Vector3   m_OriginalScale;
	int       m_ScaleIndex;

	protected override void Start()
	{
		base.Start();
		m_OriginalScale = m_PlatformBase.transform.localScale;
		m_ScaleIndex = Random.Range(0, PlatformController.Instance.CurrentScaleFactor.Length);
		
	}
	
	void FixedUpdate()
	{
		// Only run if the PlatformController exists
		if(PlatformController.Instance != null)
		{
			m_PlatformBase.localScale = m_OriginalScale * 
									    PlatformController.Instance.CurrentScaleFactor[m_ScaleIndex];
		}
	}
}
