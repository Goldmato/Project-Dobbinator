using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
	// Fields and properties
	bool m_PortalActivated;

	// Public delegates and events
	public delegate void PortalEventHandler ();
	public event PortalEventHandler OnPortalActivated;

	void OnTriggerEnter(Collider other) 
	{
		if(!m_PortalActivated)
		{
			if(OnPortalActivated != null  && other.CompareTag("Player")) 
			{
				OnPortalActivated();
				m_PortalActivated = true;
			}
		}
	}
}
