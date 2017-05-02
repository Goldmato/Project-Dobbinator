using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(DynamicTerrain))]
public class PlatformGenerator : MonoBehaviour
{
	// Fields and properties 
	[SerializeField] [Range(0.001f, 1.0f)] float m_HeightTolerance;
	[SerializeField] [Range(0.001f, 1.0f)] float m_SlopeTolerance;
	[SerializeField] [Range(1, 100)]	   int   m_ScanSquareExtent;
	[SerializeField] [Range(1, 99)]		   int m_ScanSquareInnerExtent;

	[SerializeField] List<GameObject> m_Platforms;
	[SerializeField] GameObject		  m_Player;

	IEnumerable m_CurrentState;

	List<Vector3>  m_HillPeaks = new List<Vector3>();
	DynamicTerrain m_DynamicTerrain;
	TerrainData    m_TerrainData;

	float m_TerrainMaxHeight;

	const float DELAY = 0.25f;

	void Start()
	{
		m_DynamicTerrain = GetComponent<DynamicTerrain>();

		// If there is no reference to the main player gameObject, find it via tag
		if (m_Player == null)
			m_Player = GameObject.FindGameObjectWithTag("Player");

		// Set the inital state and run the state machine
		StartCoroutine(GenerateStuctures());
	}

	IEnumerator GenerateStuctures()
	{
		m_TerrainData = null;
		m_TerrainMaxHeight = 0;

		// Wait for terrain to finish generating
		while(m_TerrainData == null)
		{
			m_TerrainData = m_DynamicTerrain.GetTerrainData();
			yield return new WaitForSeconds(0.1f);
		}

		// Find all points on the map which would be considered "hill peaks" by the algorithm
		GameManager.Current.LoadState = "Calculating Hill Peaks";
		GameManager.Current.LoadValue = 0.65f;
		FindHillPeaks();
		yield return new WaitForSeconds(DELAY);

		// Instantiate a Platform at each point in the hillPeaks list
		GameManager.Current.LoadState = "Spawning Platforms";
		GameManager.Current.LoadValue = 0.85f;
		SpawnPlatforms();
		yield return new WaitForSeconds(DELAY);

		// Clear up most of the excess Platforms 
		//GameManager.Current.LoadState = "Removing Excess Platforms";
		//GameManager.Current.LoadValue = 0.95f;
		//RemoveOverlappingPlatforms();
		//yield return new WaitForSeconds(DELAY);

		// Finish execution
		GameManager.Current.LoadState = "Done";
		GameManager.Current.LoadValue = 1.0f;
	}

	void FindHillPeaks()
	{
		m_TerrainMaxHeight *= m_TerrainData.size.y;

		if(m_ScanSquareInnerExtent > m_ScanSquareExtent)
			throw new UnityException("Please make sure the ScanSquareInnerExtent field is a lower value " +
								     "than SqanSquareExtent");

		// Scan through the terrain in a grid pattern to find a set number of peaks
		for(int x = 32; x < m_TerrainData.heightmapWidth - 32; x += m_ScanSquareExtent)
		{
			for(int y = 32; y < m_TerrainData.heightmapHeight - 32; y += m_ScanSquareExtent)
			{
				float minHeight = m_TerrainData.size.y * (1 - m_HeightTolerance);
				float maxHeight = 0;
				int peakX = 0;
				int peakY = 0;

				// Search the current grid index
				for(int bX = x; bX < x + m_ScanSquareInnerExtent; bX++)
				{
					for(int bY = y; bY < y + m_ScanSquareInnerExtent; bY++)
					{
						float height = m_TerrainData.GetHeight(bX, bY);

						if(height > maxHeight && height >= minHeight && 
						   m_TerrainData.GetSteepness((float)bX / m_TerrainData.heightmapWidth, 
												    (float)bY / m_TerrainData.heightmapHeight) / 90f <= m_SlopeTolerance) 
						{
							maxHeight = height;
							peakX = bX;
							peakY = bY;
						}
					}
				}
				if(maxHeight > 0)
					m_HillPeaks.Add(new Vector3(peakX, maxHeight, peakY));
			}
		}
	}

	void SpawnPlatforms()
	{
		// Create a random Platform at each hill point
		foreach (Vector3 pos in m_HillPeaks)
		{
			float x = pos.x / m_TerrainData.heightmapWidth;
			float y = pos.z / m_TerrainData.heightmapHeight;

			Vector3 peakPos = new Vector3(x * m_TerrainData.size.x, pos.y,
										  y * m_TerrainData.size.z);
			// Debug.Log(string.Format("X Rotation: {0}, Z Rotation: {1}", rotX, rotZ));
			GameObject newPlatform = Instantiate(m_Platforms[Random.Range(0, m_Platforms.Count)], peakPos,
								   Quaternion.Euler(0, Random.Range(0, 360), 0)) as GameObject;

			// Set the start platform if null
			if(GameManager.Current.StartPlatform == null)
				GameManager.Current.StartPlatform = newPlatform;

			// Send out a raycast from the new Platform towards the ground, and set the 
			// Platform's angle to be equal to the ground's normal direction
			RaycastHit hitInfo;
			if (Physics.Raycast(new Vector3(newPlatform.transform.position.x,
										    newPlatform.transform.position.y + 1,
										    newPlatform.transform.position.z), -newPlatform.transform.up, out hitInfo))
			{
				newPlatform.transform.up = hitInfo.normal;
			}
		}

		Debug.Log(m_HillPeaks.Count + " Platforms created!");
	}

	void RemoveOverlappingPlatforms()
	{
		float scanSquareExtent = m_TerrainData.size.x / 32;
		float scanSquareHalfExtent = scanSquareExtent / 2;
		int removedPlatforms = 0;

		for(float x = 0; x < m_TerrainData.size.x; x += scanSquareExtent)
		{
			for(float y = 0; y < m_TerrainData.size.z; y += scanSquareExtent)
			{
				List<Collider> overlappingPlatform = new List<Collider>();

				// Find all Platforms within the defined space
				foreach(Collider col in Physics.OverlapBox(new Vector3(x + scanSquareHalfExtent, m_TerrainData.size.y / 2, y + scanSquareHalfExtent),
														   new Vector3(scanSquareHalfExtent, m_TerrainData.size.y, scanSquareHalfExtent)))
				{
					if(col.GetComponent<IPlatform>() != null)
						overlappingPlatform.Add(col);
				}

				if(overlappingPlatform.Count >= 1)
				{
					int platformToKeep = 0;
					float highestPoint = 0;

					// Find the highest Platform in the local list
					for(int i = 0; i < overlappingPlatform.Count; i++)
					{
						if(overlappingPlatform[i].transform.position.y > highestPoint)
						{
							platformToKeep = i;
							highestPoint = overlappingPlatform[i].transform.position.y;
						}
					}

					// Destroy all but the highest Platform in the isolated region
					for(int i = 0; i < overlappingPlatform.Count; i++)
					{
						if(i != platformToKeep)
						{
							Destroy(overlappingPlatform[i].gameObject);
							removedPlatforms++;
							continue;
						}

						if(GameManager.Current.StartPlatform == null)
						{
							GameManager.Current.StartPlatform = overlappingPlatform[i].gameObject;
						}

						var platformScript = overlappingPlatform[i].GetComponent<IInitializable>();

						if(platformScript != null)
						{
							platformScript.Initialize();
						}
					}
				}
			}
		}

		Debug.Log(removedPlatforms + " Platforms removed! " + (m_HillPeaks.Count - removedPlatforms) + " Platforms remaining.");
	}
}
			