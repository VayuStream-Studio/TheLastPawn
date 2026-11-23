using UnityEngine;
using UnityEditor;
using System.IO;

public class NormalMapGenerator
{
    [MenuItem("Tools/Generate Sandpaper Normal Map")]
    static void GenerateNormalMap()
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // High-frequency noise for tiny sandpaper grit bumps
                float n1 = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                float n2 = Mathf.PerlinNoise((x + 50) * 0.3f, (y + 50) * 0.3f);

                Vector3 normal = new Vector3((n1 - 0.5f) * 2f, (n2 - 0.5f) * 2f, 1.0f).normalized;
                Color col = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f);
                
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();

        File.WriteAllBytes("Assets/Sandpaper_Normal.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log("Generated Assets/Sandpaper_Normal.png!");
    }
}