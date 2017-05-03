using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
	// Private serialized fields
	[SerializeField] private float gameOverDelay = 3f;

	void OnTriggerEnter (Collider other)
	{
		// End the game if the player has entered the death zone
		if (other.gameObject.CompareTag("PlayerCollider")) {
			StartCoroutine (GameOverMethod ());
		}
	}

	IEnumerator GameOverMethod ()
	{
		yield return new WaitForSeconds (gameOverDelay);
		SceneManager.LoadScene ("GameOver");
	}
}
