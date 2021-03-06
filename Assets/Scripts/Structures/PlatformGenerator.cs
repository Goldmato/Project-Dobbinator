﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(DynamicTerrain))]
public class PlatformGenerator : MonoBehaviour
{
	// Fields and properties 
	[SerializeField] [Range(-1000f, 1000f)] float m_SkyPlatformBaseHeightOffset;
	[SerializeField] [Range(0.001f, 1000f)] float m_SkyPlatformHeightInterval;
	[SerializeField] [Range(0.001f, 100f)] float m_SkyPlatformSpawnChance;
	[SerializeField] [Range(0.001f, 100f)] float m_SkyPlatformSpawnEntropy;
	[SerializeField] [Range(0.001f, 10f)]  float m_SkyPlatformHeightVariance;
	[SerializeField] [Range(0.001f, 1.0f)] float m_HeightTolerance;
	[SerializeField] [Range(0.001f, 1.0f)] float m_SlopeTolerance;
	[SerializeField] [Range(1, 100)]	   int   m_ScanSquareExtent;
	[SerializeField] [Range(1, 99)]		   int   m_ScanSquareInnerExtent;

	[SerializeField] List<GameObject> m_GroundPlatforms;
	[SerializeField] List<GameObject> m_SkyPlatforms;

	[SerializeField] List<float> m_GroundSpawnChances;
	[SerializeField] List<float> m_SkySpawnChances;

	IEnumerable m_CurrentState;

	List<Vector3>  m_HillPeaks = new List<Vector3>();

	DynamicTerrain m_DynamicTerrain;
	TerrainData    m_TerrainData;

	string m_PlatformStatistics;

	float m_SkyLayerHeight;

	int[] m_PlatformsSpawned;
    int m_SkyPlatformLayers;
	int m_SkyLayerIndex;

	const float DELAY = 0.1f;

	void Start()
	{
		m_DynamicTerrain = GetComponent<DynamicTerrain>();

		// Set the inital state and run the state machine
		StartCoroutine(GenerateStuctures());
	}

	IEnumerator GenerateStuctures()
    {
        m_TerrainData = null;

        // Wait for terrain to finish generating
        while (m_TerrainData == null)
        {
            m_TerrainData = m_DynamicTerrain.GetTerrainData();
            yield return new WaitForSeconds(0.1f);
        }

        // Find all points on the map which would be considered "hill peaks" by the algorithm
        GameManager.Current.LoadState = "Calculating Hill Peaks";
        GameManager.Current.LoadValue = 0.6f;
        FindHillPeaks();
        yield return new WaitForSeconds(DELAY);

        // Instantiate a set number of platform layers, starting with the ground layer
        GameManager.Current.LoadState = "Spawning Platform Layers [Ground]";
        GameManager.Current.LoadValue = 0.7f;
        SpawnGroundPlatformLayer();
        yield return new WaitForSeconds(DELAY);

        var baseLoadValue = GameManager.Current.LoadValue;
        var col = GetComponent<Collider>();
        CalculateSkyLayerHeights(col);

        for (m_SkyLayerIndex = 0; m_SkyLayerIndex < m_SkyPlatformLayers; m_SkyLayerIndex++)
        {
            GameManager.Current.LoadState = "Spawning Platform Layers [Sky " + (m_SkyLayerIndex + 1) + "]";
            GameManager.Current.LoadValue = baseLoadValue + ((1.0f / m_SkyPlatformLayers * m_SkyLayerIndex) * (1.0f - baseLoadValue));
            SpawnSkyPlatformLayer();
            yield return new WaitForSeconds(DELAY);
        }

        // Finish execution
        GameManager.Current.LoadState = "Done";
        GameManager.Current.LoadValue = 1.0f;

		Debug.Log("PLATFORM STATISTICS \n" +
				  "------------------- \n" + m_PlatformStatistics);
    }

    void CalculateSkyLayerHeights(Collider col)
    {
		m_SkyPlatformLayers = Mathf.CeilToInt((col.bounds.size.y - m_DynamicTerrain.TerrainMaxHeight - 
											   m_SkyPlatformBaseHeightOffset) / m_SkyPlatformHeightInterval);
		m_SkyLayerHeight = m_DynamicTerrain.TerrainMaxHeight + m_SkyPlatformBaseHeightOffset;
    }

    void FindHillPeaks()
	{
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

	void SpawnGroundPlatformLayer()
	{
		// Create a container to hold all the ground platforms
		var groundPlatformContainer = new GameObject("ground_platforms");
		groundPlatformContainer.transform.position = Vector3.zero;

		m_PlatformsSpawned = new int[m_GroundPlatforms.Count];

		// Create a random Platform at each hill point
		foreach (Vector3 pos in m_HillPeaks)
		{
			GameObject platformToSpawn = null;

			// Hopefully this doesn't cause any crashes...
			while(platformToSpawn == null)
            {
                platformToSpawn = FindRandomPlatform(m_GroundPlatforms, m_GroundSpawnChances);
            }

            float x = pos.x / m_TerrainData.heightmapWidth;
			float y = pos.z / m_TerrainData.heightmapHeight;

			Vector3 peakPos = new Vector3(x * m_TerrainData.size.x + transform.position.x, pos.y + transform.position.y,
										  y * m_TerrainData.size.z + transform.position.z);
			// Debug.Log(string.Format("X Rotation: {0}, Z Rotation: {1}", rotX, rotZ));
			GameObject newPlatform = Instantiate(platformToSpawn, peakPos,
								   Quaternion.Euler(0, Random.Range(0, 360), 0)) as GameObject;
			newPlatform.transform.SetParent(groundPlatformContainer.transform);

			// Change the platform scale depending on the difficulty
			newPlatform.transform.localScale = new Vector3 (newPlatform.transform.localScale.x / GameManager.Current.DifficultyFactor, 
															newPlatform.transform.localScale.y, 
															newPlatform.transform.localScale.z / GameManager.Current.DifficultyFactor);

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

		RecordPlatformStatistics("GROUND LAYER STATISTICS \n");
	}

    void SpawnSkyPlatformLayer()
	{
		var skyPlatformContainer = new GameObject("sky_platforms [" + (m_SkyLayerIndex + 1) + "]");
		skyPlatformContainer.transform.position = new Vector3(0, m_SkyLayerHeight, 0);

		float spawnChance = m_SkyPlatformSpawnChance;
		int scanEmptySpace = m_ScanSquareInnerExtent * 2 + 1;

		m_PlatformsSpawned = new int[m_SkyPlatforms.Count];

		for(int x = ((int)transform.position.x + MovingPlatform.MoveRange); 
				x < (m_TerrainData.size.x + transform.position.x) - MovingPlatform.MoveRange; x += m_ScanSquareExtent)
		{
			if(x + m_ScanSquareExtent > (m_TerrainData.size.x + transform.position.x) - MovingPlatform.MoveRange)
				break;
			for(int z = ((int)transform.position.z + MovingPlatform.MoveRange); 
					z < (m_TerrainData.size.z + transform.position.z) - MovingPlatform.MoveRange; z += m_ScanSquareExtent)
			{
				if(z + m_ScanSquareExtent > (m_TerrainData.size.z + transform.position.z) - MovingPlatform.MoveRange)
					break;
				if(Random.Range(0f, 100f) <= spawnChance)
				{
					GameObject platformToSpawn = null;

					// Hopefully this doesn't cause any crashes...
					while(platformToSpawn == null)
					{
						platformToSpawn = FindRandomPlatform(m_SkyPlatforms, m_SkySpawnChances);
					}

					// Find a random spot within the inner scan sqare extent range
					int randX = Random.Range(x + scanEmptySpace, (x + m_ScanSquareExtent) - scanEmptySpace);
					int randZ = Random.Range(z + scanEmptySpace, (z + m_ScanSquareExtent) - scanEmptySpace);

					var platform = Instantiate(platformToSpawn, new Vector3(randX, m_SkyLayerHeight + 
											   Random.Range(-m_SkyPlatformHeightVariance, m_SkyPlatformHeightVariance), randZ), 
											   Quaternion.identity) as GameObject;
					platform.transform.SetParent(skyPlatformContainer.transform);

					spawnChance = m_SkyPlatformSpawnChance;
				}
				else
				{
					spawnChance += m_SkyPlatformSpawnEntropy;
				}
			}
		}

		// If this is the last layer, find a random platform to set as
		// the portal platform base
		if(m_SkyLayerIndex + 1 == m_SkyPlatformLayers &&
		   GameManager.Current.PortalPlatform == null)
		{
			SetRandomPortalPlatform(skyPlatformContainer);
		}

		m_SkyLayerHeight += m_SkyPlatformHeightInterval;

		RecordPlatformStatistics("SKY LAYER[" + (m_SkyLayerIndex + 1) + "] STATISTICS \n");
	}

	GameObject FindRandomPlatform(List<GameObject> platList, List<float> spawnChanceList)
    {
        for (int i = 0; i < platList.Count; i++)
        {
            if (spawnChanceList[i] == 0)
                spawnChanceList[i] = 0.5f;
            if (Random.value <= spawnChanceList[i])
            {
				m_PlatformsSpawned[i]++;
				if(platList[i] != null)
            		return platList[i];
				else
                	throw new UnityException("No platform assigned to Ground Platform spawn chance [" + i + "]");
            }
        }

		return null;
    }

	void RecordPlatformStatistics(string info)
	{
		m_PlatformStatistics += info;

		for(int i = 0; i < m_PlatformsSpawned.Length; i++)
		{
			m_PlatformStatistics += "Instances of platform[" + i + "] : " + m_PlatformsSpawned[i] + "\n";
		}
	}

	void SetRandomPortalPlatform(GameObject parentContainer)
	{
		int randPlat = Random.Range(0, parentContainer.transform.childCount);
		GameManager.Current.PortalPlatform = parentContainer.transform.GetChild(randPlat).gameObject;
	}

	#region DEPRECATED

	//void RemoveOverlappingPlatforms()
	//{
	//	float scanSquareExtent = m_TerrainData.size.x / 32;
	//	float scanSquareHalfExtent = scanSquareExtent / 2;
	//	int removedPlatforms = 0;

	//	for(float x = transform.position.x; x < m_TerrainData.size.x + transform.position.x; x += scanSquareExtent)
	//	{
	//		for(float y = transform.position.z; y < m_TerrainData.size.z + transform.position.z; y += scanSquareExtent)
	//		{
	//			List<Collider> overlappingPlatform = new List<Collider>();

	//			// Find all Platforms within the defined space
	//			foreach(Collider col in Physics.OverlapBox(new Vector3(x + scanSquareHalfExtent, m_TerrainData.size.y / 2, y + scanSquareHalfExtent),
	//													   new Vector3(scanSquareHalfExtent, m_TerrainData.size.y, scanSquareHalfExtent)))
	//			{
	//				if(col.GetComponent<IPlatform>() != null)
	//					overlappingPlatform.Add(col);
	//			}

	//			if(overlappingPlatform.Count >= 1)
	//			{
	//				int platformToKeep = 0;
	//				float highestPoint = 0;

	//				// Find the highest Platform in the local list
	//				for(int i = 0; i < overlappingPlatform.Count; i++)
	//				{
	//					if(overlappingPlatform[i].transform.position.y > highestPoint)
	//					{
	//						platformToKeep = i;
	//						highestPoint = overlappingPlatform[i].transform.position.y;
	//					}
	//				}

	//				// Destroy all but the highest Platform in the isolated region
	//				for(int i = 0; i < overlappingPlatform.Count; i++)
	//				{
	//					if(i != platformToKeep)
	//					{
	//						Destroy(overlappingPlatform[i].gameObject);
	//						removedPlatforms++;
	//						continue;
	//					}

	//					if(GameManager.Current.StartPlatform == null)
	//					{
	//						GameManager.Current.StartPlatform = overlappingPlatform[i].gameObject;
	//					}

	//					var platformScript = overlappingPlatform[i].GetComponent<IInitializable>();

	//					if(platformScript != null)
	//					{
	//						platformScript.Initialize();
	//					}
	//				}
	//			}
	//		}
	//	}

	//	Debug.Log(removedPlatforms + " Platforms removed! " + (m_HillPeaks.Count - removedPlatforms) + " Platforms remaining.");
	//}

	#endregion

}
			