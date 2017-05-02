using UnityEngine;

public class ResizingPlatform : MonoBehaviour, IInitializable
{
	// Fields and properties
	Transform platformBase;
	Vector3   originalScale;
	bool      scriptEnabled;
	int       scaleIndex;

	public void Initialize()
	{
		// Initialization method to be called remotely (by the Platform generator)
		scaleIndex = Random.Range(0, PlatformController.Instance.CurrentScaleFactor.Length);
		platformBase = transform.Find("platform_base");
		originalScale = platformBase.localScale;
		scriptEnabled = true;
	}
	
	void FixedUpdate()
	{
		// Only run if the script is enabled
		if(scriptEnabled && PlatformController.Instance != null)
		{
			platformBase.localScale = originalScale * 
									  PlatformController.Instance.CurrentScaleFactor[scaleIndex];
		}
	}
}
