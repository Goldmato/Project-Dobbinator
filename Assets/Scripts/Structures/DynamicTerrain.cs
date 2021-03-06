﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class DynamicTerrain : MonoBehaviour
{
	// Public properties
	public float TerrainHeightScale { get { return m_TerrainHeight; } }
	public float TerrainMaxHeight { get { return m_MaxHeight * m_TerrainData.size.y; } }

	// Private serialized fields
	[SerializeField] private Transform m_DeathZone;
	[SerializeField] private Texture2D[] m_SplatTextures;
	[SerializeField] [Range(0.01f, 1f)] private float m_BorderHeight = 0.1f;
	[SerializeField] [Range(0.01f, 1f)] private float m_TerrainHeight = 0.5f;

	TerrainData m_TerrainData;

	// Private fields
	float m_MaxHeight;
	float m_TerrainBorderHeight;
	bool  m_TerrainFinishedGenerating = false;

	// Constants
	const float DELAY = 0.1f;
	const string WALL_TOP_NAME = "wall_top";
	const string WALL_BOTTOM_NAME = "wall_bottom";
	const string WALL_RIGHT_NAME = "wall_right";
	const string WALL_LEFT_NAME = "wall_left";
	const string DEATH_ZONE_NAME = "death_zone";

	void Start()
	{
		// If the wall references are empty, find them via searching through the child objects
		// Otherwise, throw a new exception error and pause the game
		List<string> missingChildren = new List<string>();

		try
		{
			if(m_DeathZone == null)
			{
				m_DeathZone = transform.FindChild(DEATH_ZONE_NAME);
				if(m_DeathZone == null)
					missingChildren.Add(DEATH_ZONE_NAME);
			}
		}
		catch (System.NullReferenceException exception)
		{
			string errMessage = "Error! Please make sure this script is attached to a gameObject with the specified children: ";
			foreach(string child in missingChildren)
			{
				errMessage += child + " | ";
			}
			throw new UnityException(errMessage, exception);
		}

		// Start the main method
		StartCoroutine(GenerateTerrain());
	}

	IEnumerator GenerateTerrain()
	{
		// Add terrain and terrainCollider components to the empty gameObject
		Terrain terrain = gameObject.AddComponent<Terrain>();
		TerrainCollider terrainCollider = gameObject.AddComponent<TerrainCollider>(); 

		// Generate the terrain data
		m_TerrainData = new TerrainData();

		// Initialize the terrain data for both the terrain and its collider
		GameManager.Current.LoadState = "Building Terrain Data";
		GameManager.Current.LoadValue = 0.05f;
		BuildTerrainData();
		yield return new WaitForSeconds(DELAY);

		// Randomly generate a heightmap for the terrain using procedural noise
		GameManager.Current.LoadState = "Applying Heightmap";
		GameManager.Current.LoadValue = 0.35f;
		RandomizeTerrainHeights();
		yield return new WaitForSeconds(DELAY);

		// Initialize the terrain textures
		GameManager.Current.LoadState = "Building Texture Data";
		GameManager.Current.LoadValue = 0.40f;
		BuildSplats();
		yield return new WaitForSeconds(DELAY);

		// Apply the terrain textures 
		GameManager.Current.LoadState = "Applying Textures";
		GameManager.Current.LoadValue = 0.50f;
		PaintTerrainBySlopes();
		yield return new WaitForSeconds(DELAY);

		terrain.terrainData = m_TerrainData;
		terrainCollider.terrainData = m_TerrainData;

		m_TerrainFinishedGenerating = true;
	}

	public TerrainData GetTerrainData()
	{
		if(m_TerrainFinishedGenerating)
		{
			return m_TerrainData;
		}
		return null;
	}

	void BuildTerrainData()
	{
		// Calculate the resolutions and terrain size based on the distance between arena walls
		var col = GetComponent<Collider>();
		int mapResX = Mathf.RoundToInt(col.bounds.size.x);
		int mapResY = Mathf.RoundToInt(col.bounds.size.y * m_TerrainHeight);
		int mapResZ = Mathf.RoundToInt(col.bounds.size.z);

		m_TerrainData.heightmapResolution = mapResX / 2 + 1;
		m_TerrainData.baseMapResolution = mapResX / 2 + 1;
		m_TerrainData.SetDetailResolution(mapResX, 16);

		// Set terrain size AFTER setting the resolution
		m_TerrainData.size = new Vector3(mapResX, mapResY, mapResZ);
		m_TerrainBorderHeight = m_BorderHeight * m_TerrainData.size.y;

		// Scale the deathzone height to the border height
		m_DeathZone.localScale = new Vector3(m_DeathZone.localScale.x,
											(m_TerrainBorderHeight / 2) - 1,
											 m_DeathZone.localScale.z);
	}

	void BuildSplats()
	{
		SplatPrototype[] splatPrototypes = new SplatPrototype[m_SplatTextures.Length];

		for(int i = 0; i < m_SplatTextures.Length; i++)
		{
			splatPrototypes[i] = new SplatPrototype();
			splatPrototypes[i].texture = m_SplatTextures[i];
		}

		m_TerrainData.splatPrototypes = splatPrototypes;
	}

	void RandomizeTerrainHeights()
	{
		int noiseSeed =	   Random.Range(0, int.MaxValue);
		var noiseMap =     new LibNoise.Noise2D(m_TerrainData.heightmapWidth, m_TerrainData.heightmapHeight);
		var noiseModule =  new LibNoise.Generator.Perlin();

		// Configure the noise parameters
		noiseModule.OctaveCount = 20;
		noiseModule.Persistence = 0.45f;
		noiseModule.Seed = noiseSeed;
		noiseMap.Generator = noiseModule;

		// Generate the noise map
		noiseMap.GeneratePlanar(-1.5f, 1.5, -1.5, 1.5f, true);

		// Get a two-dimensional array of heights from the noise map
		float borderHeight = m_TerrainBorderHeight / m_TerrainData.size.y;
		float[,] heights = noiseMap.GetData();

		// Loop through every "pixel" and set the height to a random value
		for(int x = 0; x < m_TerrainData.heightmapWidth; x++)
		{
			for(int y = 0; y < m_TerrainData.heightmapHeight; y++)
			{
				// Divide the height by 2 so it doesn't flow over the top of the arena
				heights[x, y] /= 2f;

				// Fill in flat areas with random noise
				if(heights[x, y] <= borderHeight &&
				   heights[x, y] >= borderHeight / 2)
				{
					float newHeight = Random.Range(0f, borderHeight / 2);
					heights[x, y] = Mathf.Lerp(heights[x, y], newHeight, 0.5f);
				}

				// Update the maxHeight variable if the current height point breaks the previous record
				if(heights[x, y] > m_MaxHeight)
					m_MaxHeight = heights[x, y];
			}
		}

		// Debug.Log("Max Height: " + m_MaxHeight);

		// Store the result back into terrainData
		m_TerrainData.SetHeights(0, 0, heights);
	}

	void PaintTerrainBySlopes()
	{
		float[,,] splatMaps = m_TerrainData.GetAlphamaps(0, 0, m_TerrainData.alphamapWidth, m_TerrainData.alphamapHeight);

		// Loop through every pixel of the alphamap and set the opacity of each texture
		for(int aX = 0; aX < m_TerrainData.alphamapWidth; aX++)
		{
			for(int aY = 0; aY < m_TerrainData.alphamapHeight; aY++)
			{
				float x = (float)aX / m_TerrainData.alphamapWidth;
				float y = (float)aY / m_TerrainData.alphamapHeight;
				int hX = Mathf.RoundToInt(((float)m_TerrainData.heightmapWidth / m_TerrainData.alphamapWidth) * aX);
				int hY = Mathf.RoundToInt(((float)m_TerrainData.heightmapHeight / m_TerrainData.alphamapHeight) * aY);

				float angle = m_TerrainData.GetSteepness(y, x); // Note: x and y are in reverse order here, for some reason this works
				float height = m_TerrainData.GetHeight(hY, hX);
				float steepness = angle / 90.0f;
				float flatness = 1 - (steepness * 1.25f);

				// '0' is "grass", '1' is "dirt", '2' is "rock", '3' is "metal", '4' is "lava"
				if(height <= m_TerrainBorderHeight)
				{
					splatMaps[aX, aY, 0] = 0f;
					splatMaps[aX, aY, 1] = 0f;
					splatMaps[aX, aY, 2] = 0f;
					splatMaps[aX, aY, 3] = steepness + (height / 10);
					splatMaps[aX, aY, 4] = flatness;
				}
				else
				{
					splatMaps[aX, aY, 0] = flatness;
					if(steepness >= 0.65f)
					{
						splatMaps[aX, aY, 1] = flatness * flatness;
						splatMaps[aX, aY, 2] = steepness;
					}
					else
					{
						splatMaps[aX, aY, 1] = steepness;
					}
				}
			}
		}

		m_TerrainData.SetAlphamaps(0, 0, splatMaps);
	}
}
