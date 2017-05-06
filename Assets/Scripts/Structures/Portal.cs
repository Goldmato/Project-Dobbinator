using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
	// Public delegates and events
	public delegate void PortalEventHandler ();
	public event PortalEventHandler OnPortalActivated;

	void OnTriggerEnter(Collider other) 
	{
		if(other.CompareTag("Player"))
		{
			if(OnPortalActivated != null) 
				OnPortalActivated();
		}
	}
}
