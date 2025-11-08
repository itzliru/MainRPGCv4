using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureScale
{
    public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
    {
        Texture2D newTex = new Texture2D(newWidth, newHeight, tex.format, false);
        float ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
        float ratioY = 1.0f / ((float)newHeight / (tex.height - 1));

        for (int y = 0; y < newHeight; y++)
        {
            int yy = Mathf.FloorToInt(y * ratioY);
            int y1 = Mathf.Min(yy + 1, tex.height - 1);
            float yLerp = y * ratioY - yy;

            for (int x = 0; x < newWidth; x++)
            {
                int xx = Mathf.FloorToInt(x * ratioX);
                int x1 = Mathf.Min(xx + 1, tex.width - 1);
                float xLerp = x * ratioX - xx;

                Color bl = tex.GetPixel(xx, yy);
                Color br = tex.GetPixel(x1, yy);
                Color tl = tex.GetPixel(xx, y1);
                Color tr = tex.GetPixel(x1, y1);

                Color b = Color.Lerp(bl, br, xLerp);
                Color t = Color.Lerp(tl, tr, xLerp);
                newTex.SetPixel(x, y, Color.Lerp(b, t, yLerp));
            }
        }

        newTex.Apply();
        tex.Reinitialize(newWidth, newHeight);
        tex.SetPixels(newTex.GetPixels());
        tex.Apply();
    }
}
