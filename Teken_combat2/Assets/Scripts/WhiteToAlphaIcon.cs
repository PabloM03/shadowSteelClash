using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WhiteToAlphaHierarchy : MonoBehaviour
{
    [Range(0.8f, 1f)] public float whiteThreshold = 0.95f;
    public bool includeInactive = true;
    public bool applyOnStart = true;
    [Tooltip("Si está activo, se determinará el color de fondo tomando los píxeles de las esquinas y se hará transparente solo ese color (útil cuando el icono también contiene blanco).")]
    public bool detectBackgroundFromBorder = true;
    [Tooltip("Tolerancia (0-1) para comparar el color de fondo cuando 'detectBackgroundFromBorder' está activo.")]
    public float backgroundColorTolerance = 0.08f;

    private readonly Dictionary<Sprite, Sprite> _cacheBySprite = new();

    void Start()
    {
        if (applyOnStart) ApplyToHierarchy();
    }

    [ContextMenu("Apply To Hierarchy")]
    public void ApplyToHierarchy()
    {
        int changed = 0;

        // UI Images
        var images = GetComponentsInChildren<Image>(includeInactive);
        foreach (var img in images)
        {
            if (img.sprite == null) continue;
            var newSprite = ConvertSpriteSafe(img.sprite);
            if (newSprite != null)
            {
                // Guardar estado del RectTransform para restaurarlo después
                var rt = img.rectTransform;
                Vector2 anchorMin = rt.anchorMin;
                Vector2 anchorMax = rt.anchorMax;
                Vector2 anchoredPosition = rt.anchoredPosition;
                Vector2 sizeDelta = rt.sizeDelta;
                Vector2 pivot = rt.pivot;
                Vector3 localPosition = rt.localPosition;
                Quaternion localRotation = rt.localRotation;
                Vector3 localScale = rt.localScale;

                img.sprite = newSprite;
                img.preserveAspect = true;

                // Restaurar estado del RectTransform para que no cambie el transform original
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
                rt.pivot = pivot;
                rt.localPosition = localPosition;
                rt.localRotation = localRotation;
                rt.localScale = localScale;

                changed++;
            }
        }

        // 2D SpriteRenderers
        var srs = GetComponentsInChildren<SpriteRenderer>(includeInactive);
        foreach (var sr in srs)
        {
            if (sr.sprite == null) continue;
            var newSprite = ConvertSpriteSafe(sr.sprite);
            if (newSprite != null)
            {
                // Guardar transform y restaurar después de asignar el sprite
                var t = sr.transform;
                Vector3 localPos = t.localPosition;
                Quaternion localRot = t.localRotation;
                Vector3 localScale = t.localScale;

                sr.sprite = newSprite;

                t.localPosition = localPos;
                t.localRotation = localRot;
                t.localScale = localScale;

                changed++;
            }
        }

        Debug.Log($"[WhiteToAlpha] Procesados {changed} sprites en '{name}'.");
    }

    Sprite ConvertSpriteSafe(Sprite srcSprite)
    {
        if (_cacheBySprite.TryGetValue(srcSprite, out var cached))
            return cached;

        // 1) Sacamos los píxeles del rect del sprite sin exigir Read/Write usando la GPU
        var rect = srcSprite.rect;
        int w = Mathf.RoundToInt(rect.width);
        int h = Mathf.RoundToInt(rect.height);

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var prev = RenderTexture.active;

        try
        {
            // Dibujar el área del sprite (sub-rect) en un RT de w x h
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, w, 0, h);
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);

            // Material temporal para recortar el sub-rect
            // Usamos Graphics.DrawTexture con source rect normalizado
            var tex = srcSprite.texture;
            var src = new Rect(
                rect.x / tex.width,
                rect.y / tex.height,
                rect.width / tex.width,
                rect.height / tex.height
            );
            Graphics.DrawTexture(new Rect(0, 0, w, h), tex, src, 0, 0, 0, 0);
            GL.PopMatrix();

            // 2) Leemos del RT a una Texture2D legible
            var tmp = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tmp.Apply();

            // Nota: ReadPixels suele devolver la imagen invertida verticalmente respecto
            // a cómo estaban los píxeles dibujados en el RenderTexture. Corregimos eso
            // volteando las filas para evitar que las imágenes aparezcan rotadas/volteadas.
            var px = tmp.GetPixels32();
            var pxFlipped = new Color32[px.Length];
            for (int row = 0; row < h; row++)
            {
                int srcRowStart = row * w;
                int dstRowStart = (h - 1 - row) * w;
                System.Array.Copy(px, srcRowStart, pxFlipped, dstRowStart, w);
            }
            px = pxFlipped;
            float thr = whiteThreshold;

            if (detectBackgroundFromBorder && w > 2 && h > 2)
            {
                // Calcular color de fondo promedio a partir de las 4 esquinas
                Color avgBg = Color.black;
                Color32 c00 = px[0];
                Color32 c0w = px[w - 1];
                Color32 cw0 = px[(h - 1) * w];
                Color32 cww = px[(h - 1) * w + (w - 1)];
                avgBg += new Color(c00.r / 255f, c00.g / 255f, c00.b / 255f);
                avgBg += new Color(c0w.r / 255f, c0w.g / 255f, c0w.b / 255f);
                avgBg += new Color(cw0.r / 255f, cw0.g / 255f, cw0.b / 255f);
                avgBg += new Color(cww.r / 255f, cww.g / 255f, cww.b / 255f);
                avgBg /= 4f;

                float tol = Mathf.Clamp01(backgroundColorTolerance);
                float tolSq = tol * tol;

                for (int i = 0; i < px.Length; i++)
                {
                    var c = px[i];
                    if (c.a == 0) { px[i] = c; continue; }
                    Color col = new Color(c.r / 255f, c.g / 255f, c.b / 255f);
                    float dr = col.r - avgBg.r;
                    float dg = col.g - avgBg.g;
                    float db = col.b - avgBg.b;
                    float distSq = dr * dr + dg * dg + db * db;
                    if (distSq <= tolSq)
                        c.a = 0;
                    px[i] = c;
                }
            }
            else
            {
                for (int i = 0; i < px.Length; i++)
                {
                    var c = px[i];
                    float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
                    if (c.a > 0 && r >= thr && g >= thr && b >= thr)
                        c.a = 0;
                    px[i] = c;
                }
            }

            tmp.SetPixels32(px);
            tmp.Apply();

            // 4) Creamos un nuevo sprite manteniendo pivot y PPU del original
            var pivot = new Vector2(srcSprite.pivot.x / rect.width, srcSprite.pivot.y / rect.height);
            var newSprite = Sprite.Create(tmp, new Rect(0, 0, w, h), pivot, srcSprite.pixelsPerUnit);
            _cacheBySprite[srcSprite] = newSprite;
            return newSprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[WhiteToAlpha] No se pudo convertir '{srcSprite.name}': {ex.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
