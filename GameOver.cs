using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
	private float gameOverDelay = 3f;

	void OnTriggerEnter (Collider other)
	{
		// end the game if the player has entered the death zone
		if (other.gameObject.tag == "PlayerCollider") {
			StartCoroutine (GameOverMethod ());
		}
	}

	IEnumerator GameOverMethod ()
	{
		yield return new WaitForSeconds (gameOverDelay);
		SceneManager.LoadScene ("GameOver");
	}
}
