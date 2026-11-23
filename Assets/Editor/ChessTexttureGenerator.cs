using UnityEngine;
using UnityEditor;
using System.IO;

public class ChessTextureGenerator
{
    [MenuItem("Tools/Generate Chessboard Texture")]
    static void GenerateTexture()
    {
        int size = 512;
        int half = size / 2;
        Texture2D tex = new Texture2D(size, size);

        Color darkSquare = new Color(0.08f, 0.08f, 0.1f);   // Grim charcoal stone
        Color lightSquare = new Color(0.55f, 0.52f, 0.48f); // Worn bone/marble

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isDark = (x < half && y < half) || (x >= half && y >= half);
                tex.SetPixel(x, y, isDark ? darkSquare : lightSquare);
            }
        }
        tex.Apply();

        File.WriteAllBytes("Assets/Chessboard_512.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log("Generated Assets/Chessboard_512.png!");
    }
}