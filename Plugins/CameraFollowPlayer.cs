using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour 
{
	// Fields and properties
	[SerializeField] private GameObject player;

	Vector3 offset;

	void Start() 
	{
		// Find the player via tag if not assigned in the inspector
		if(player == null)
			player = GameObject.FindGameObjectWithTag("Player");

		// Set the offset to be the original distance between the camera and player
		offset = transform.position - player.transform.position;
	}

	void FixedUpdate() 
	{
		Vector3 newPos = transform.position;

		if(player != null)
			newPos = player.transform.position + offset;

		transform.position = newPos;
	}
	
}
