using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class NoiseMapGeneration : MonoBehaviour
{
	public static float[, ] GenerateNoiseMap(int mapDepth, int mapWidth, float scale, float offsetX, float offsetZ, Waves[] waves)
	{
		// Create empty noise map with mapDepth and mapWidth coordinates
		float[, ] noiseMap = new float[mapDepth, mapWidth];

		for (int zIndex = 0; zIndex < mapDepth; zIndex++)
		{
			for (int xIndex = 0; xIndex < mapWidth; xIndex++)
			{
				// Calculate sample indices based on coordinates and scale
				float sampleX = (xIndex + offsetX) / scale;
				float sampleZ = (zIndex + offsetZ) / scale;

				// this.Print(sampleX, sampleZ);

				// Generae noise value with PerlinNoise
				float noise = 0f;
				float normalization = 0f;
				foreach (Waves wave in waves)
				{
					// Generate noise value using PerlinNoise for a given Wave
					noise += wave.amplitude * Mathf.PerlinNoise(sampleX * wave.frequency + wave.seed, sampleZ * wave.frequency + wave.seed) * (1 - EaseMethods.CubicEaseIn(Mathf.Abs(sampleX) * Mathf.Abs(sampleZ) / Snowstorm.walkDistance, 0, 1, 1));
					normalization += wave.amplitude;
				}
				// normalize the noise value so that it is within 0 and 1
				noise /= normalization;

				noiseMap[zIndex, xIndex] = noise;
			}
		}
		return noiseMap;
	}
}

[System.Serializable]
public struct Waves
{
	public float seed;
	public float frequency;
	public float amplitude;
}
