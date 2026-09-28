using System;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Utilidad para:
    /// 1. Cargar archivos PNG/JPG desde disco y convertirlos en Sprites escalados y proporcionados.
    /// 2. Recortar fotos de webcam en forma de cabeza ovalada (Sticker) con borde cómic.
    /// 3. Invertir / rotar fotos si la webcam entrega la imagen al revés.
    /// 4. Generar rostros cómicos procedurales de muestra para pruebas inmediatas.
    /// </summary>
    public static class FaceLoader
    {
        public const float TARGET_HEAD_WORLD_HEIGHT = 1.05f;

        /// <summary>
        /// Carga un archivo de imagen (PNG/JPG) desde el disco y devuelve un Sprite de Unity
        /// perfectamente calibrado para el tamaño del cuerpo del luchador.
        /// </summary>
        public static Sprite LoadSpriteFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FaceLoader] Archivo no encontrado: {filePath}");
                return null;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (texture.LoadImage(fileData))
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;

                    // Calibrar PixelsPerUnit para que la cabeza mida exactamente ~1.05 unidades de mundo
                    float ppu = texture.height / TARGET_HEAD_WORLD_HEIGHT;

                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.45f), // Pivote ligeramente hacia el mentón
                        ppu
                    );
                    sprite.name = Path.GetFileNameWithoutExtension(filePath);
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FaceLoader] Error al cargar imagen desde {filePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Convierte una Texture2D en un Sprite con tamaño normalizado.
        /// </summary>
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null) return null;
            float ppu = texture.height / TARGET_HEAD_WORLD_HEIGHT;
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.45f),
                ppu
            );
        }

        /// <summary>
        /// Aplica una máscara ovalada de sticker con contorno blanco y transparente
        /// para que la foto de la webcam parezca una cabeza recortada cómica (estilo Photo Dojo / Smash).
        /// </summary>
        public static Texture2D MaskAsOvalHead(Texture2D source, bool flipY = false, bool flipX = false)
        {
            int size = Mathf.Min(source.width, source.height);
            int xOffset = (source.width - size) / 2;
            int yOffset = (source.height - size) / 2;

            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color borderOutline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color borderGlow = new Color(1f, 1f, 1f, 0.95f);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radiusX = size * 0.45f;
            float radiusY = size * 0.48f; // Ligeramente más alto que ancho (forma de cabeza)

            for (int y = 0; y < size; y++)
            {
                int srcY = flipY ? (size - 1 - y) + yOffset : y + yOffset;

                for (int x = 0; x < size; x++)
                {
                    int srcX = flipX ? (size - 1 - x) + xOffset : x + xOffset;

                    // Distancia normalizada en elipse
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distSq = (dx * dx) + (dy * dy);

                    if (distSq > 1.0f)
                    {
                        result.SetPixel(x, y, transparent);
                    }
                    else if (distSq > 0.94f)
                    {
                        // Borde exterior negro fino
                        result.SetPixel(x, y, borderOutline);
                    }
                    else if (distSq > 0.88f)
                    {
                        // Borde blanco estilo sticker
                        result.SetPixel(x, y, borderGlow);
                    }
                    else
                    {
                        // Píxel de la foto
                        Color pixel = source.GetPixel(srcX, srcY);
                        result.SetPixel(x, y, pixel);
                    }
                }
            }

            result.Apply();
            return result;
        }

        /// <summary>
        /// Invierte verticalmente una textura.
        /// </summary>
        public static Texture2D FlipVertical(Texture2D source)
        {
            int w = source.width;
            int h = source.height;
            Texture2D flipped = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    flipped.SetPixel(x, y, source.GetPixel(x, h - 1 - y));
                }
            }

            flipped.Apply();
            return flipped;
        }

        /// <summary>
        /// Invierte horizontalmente una textura (efecto espejo).
        /// </summary>
        public static Texture2D FlipHorizontal(Texture2D source)
        {
            int w = source.width;
            int h = source.height;
            Texture2D flipped = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    flipped.SetPixel(x, y, source.GetPixel(w - 1 - x, y));
                }
            }

            flipped.Apply();
            return flipped;
        }

        /// <summary>
        /// Guarda una textura en formato PNG en el disco.
        /// </summary>
        public static bool SaveTextureToFile(Texture2D texture, string filePath)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Texture2D readableTexture = MakeTextureReadable(texture);
                byte[] bytes = readableTexture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
                Debug.Log($"[FaceLoader] Textura guardada en: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FaceLoader] Error al guardar textura en {filePath}: {ex.Message}");
                return false;
            }
        }

        public static Texture2D MakeTextureReadable(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB
            );

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            Texture2D readableText = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText;
        }

        /// <summary>
        /// Genera un perfil con 4 caras cómicas procedurales para cualquier luchador.
        /// </summary>
        public static FaceProfile CreateDefaultProceduralProfile(int fighterId)
        {
            FaceProfile profile = new FaceProfile();
            profile.fighterId = fighterId;

            Color[] skinPalette = {
                new Color(1f, 0.85f, 0.72f), // Claro
                new Color(0.92f, 0.76f, 0.62f), // Moreno
                new Color(0.85f, 0.68f, 0.52f), // Bronceado
                new Color(0.70f, 0.52f, 0.38f), // Oscuro
                new Color(0.98f, 0.82f, 0.65f), // Melocotón
                new Color(0.88f, 0.75f, 0.60f), // Oliva
                new Color(0.75f, 0.88f, 0.95f), // Fantasmal
                new Color(1f, 0.90f, 0.75f)     // Dorado
            };

            Color[] bandPalette = {
                new Color(0.9f, 0.2f, 0.2f),   // Rojo
                new Color(0.2f, 0.45f, 0.95f), // Azul
                new Color(0.95f, 0.8f, 0.1f),  // Amarillo
                new Color(0.2f, 0.85f, 0.35f), // Verde
                new Color(0.75f, 0.2f, 0.85f), // Morado
                new Color(0.95f, 0.5f, 0.1f),  // Naranja
                new Color(0.1f, 0.85f, 0.85f), // Cian
                new Color(0.15f, 0.15f, 0.15f) // Negro
            };

            int idx = Mathf.Clamp(fighterId - 1, 0, skinPalette.Length - 1);
            Color skin = skinPalette[idx];
            Color band = bandPalette[idx];

            profile.faceBase = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Base, skin, band));
            profile.faceAngry = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Enojo, skin, band));
            profile.faceHurt = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Dolor, skin, band));
            profile.faceKO = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.KO, skin, band));

            return profile;
        }

        private static Texture2D GenerateCartoonFaceTexture(FaceType emotion, Color skinColor, Color accentColor)
        {
            int size = 160;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radiusX = size * 0.42f;
            float radiusY = size * 0.46f;
            Color outlineColor = new Color(0.12f, 0.1f, 0.15f, 1f);

            Color currentSkin = emotion == FaceType.Enojo 
                ? Color.Lerp(skinColor, new Color(1f, 0.35f, 0.35f), 0.55f) 
                : (emotion == FaceType.KO ? Color.Lerp(skinColor, new Color(0.6f, 0.65f, 0.78f), 0.6f) : skinColor);

            // Cabeza elíptica
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distSq = (dx * dx) + (dy * dy);

                    if (distSq <= 1f)
                    {
                        if (distSq >= 0.88f)
                            tex.SetPixel(x, y, outlineColor);
                        else
                            tex.SetPixel(x, y, currentSkin);
                    }
                }
            }

            // Bandana
            int bandanaY = (int)(size * 0.66f);
            for (int y = bandanaY; y < bandanaY + 16; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    if ((dx * dx) + (dy * dy) < 0.88f)
                    {
                        tex.SetPixel(x, y, accentColor);
                    }
                }
            }

            // Ojos y Expresiones
            int eyeY = (int)(size * 0.50f);
            int leftEyeX = (int)(size * 0.35f);
            int rightEyeX = (int)(size * 0.65f);

            switch (emotion)
            {
                case FaceType.Base:
                    DrawCircle(tex, leftEyeX, eyeY, 7, Color.white);
                    DrawCircle(tex, rightEyeX, eyeY, 7, Color.white);
                    DrawCircle(tex, leftEyeX + 1, eyeY, 3, outlineColor);
                    DrawCircle(tex, rightEyeX - 1, eyeY, 3, outlineColor);
                    DrawMouth(tex, (int)(size * 0.5f), (int)(size * 0.28f), 18, 7, outlineColor, true);
                    break;

                case FaceType.Enojo:
                    DrawCircle(tex, leftEyeX, eyeY, 8, Color.white);
                    DrawCircle(tex, rightEyeX, eyeY, 8, Color.white);
                    DrawCircle(tex, leftEyeX, eyeY, 4, new Color(0.9f, 0.1f, 0.1f));
                    DrawCircle(tex, rightEyeX, eyeY, 4, new Color(0.9f, 0.1f, 0.1f));
                    // Cejas en V furiosa
                    DrawLine(tex, leftEyeX - 12, eyeY + 12, leftEyeX + 12, eyeY + 3, outlineColor, 4);
                    DrawLine(tex, rightEyeX + 12, eyeY + 12, rightEyeX - 12, eyeY + 3, outlineColor, 4);
                    DrawOpenMouth(tex, (int)(size * 0.5f), (int)(size * 0.26f), 22, 14, outlineColor, new Color(0.7f, 0.1f, 0.1f));
                    break;

                case FaceType.Dolor:
                    DrawX(tex, leftEyeX, eyeY, 8, outlineColor, 3);
                    DrawCircle(tex, rightEyeX, eyeY, 11, new Color(0.45f, 0.2f, 0.55f, 0.75f));
                    DrawCircle(tex, rightEyeX, eyeY, 5, outlineColor);
                    DrawWobblyMouth(tex, (int)(size * 0.5f), (int)(size * 0.28f), 20, outlineColor);
                    DrawCircle(tex, leftEyeX - 14, eyeY + 16, 5, new Color(0.3f, 0.7f, 1f));
                    break;

                case FaceType.KO:
                    DrawX(tex, leftEyeX, eyeY, 10, outlineColor, 4);
                    DrawX(tex, rightEyeX, eyeY, 10, outlineColor, 4);
                    DrawCircle(tex, (int)(size * 0.5f) + 4, (int)(size * 0.22f), 7, new Color(1f, 0.4f, 0.5f));
                    DrawLine(tex, (int)(size * 0.36f), (int)(size * 0.26f), (int)(size * 0.64f), (int)(size * 0.26f), outlineColor, 3);
                    break;
            }

            tex.Apply();
            return tex;
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= r)
                        {
                            tex.SetPixel(x, y, col);
                        }
                    }
                }
            }
        }

        private static void DrawX(Texture2D tex, int cx, int cy, int size, Color col, int thickness)
        {
            DrawLine(tex, cx - size, cy - size, cx + size, cy + size, col, thickness);
            DrawLine(tex, cx - size, cy + size, cx + size, cy - size, col, thickness);
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col, int thickness)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawCircle(tex, x0, y0, thickness / 2, col);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void DrawMouth(Texture2D tex, int cx, int cy, int width, int height, Color col, bool smile)
        {
            for (int x = -width / 2; x <= width / 2; x++)
            {
                float normalizedX = (float)x / (width / 2);
                int yOffset = (int)((1f - (normalizedX * normalizedX)) * (smile ? -height : height));
                DrawCircle(tex, cx + x, cy + yOffset, 2, col);
            }
        }

        private static void DrawOpenMouth(Texture2D tex, int cx, int cy, int width, int height, Color outline, Color inside)
        {
            for (int y = -height / 2; y <= height / 2; y++)
            {
                for (int x = -width / 2; x <= width / 2; x++)
                {
                    float nx = (float)x / (width / 2);
                    float ny = (float)y / (height / 2);
                    if (nx * nx + ny * ny <= 1f)
                    {
                        tex.SetPixel(cx + x, cy + y, inside);
                    }
                }
            }
            for (int x = -width / 3; x <= width / 3; x++)
            {
                DrawCircle(tex, cx + x, cy + height / 3, 2, Color.white);
            }
        }

        private static void DrawWobblyMouth(Texture2D tex, int cx, int cy, int width, Color col)
        {
            for (int x = -width / 2; x <= width / 2; x++)
            {
                int y = (int)(Mathf.Sin(x * 0.4f) * 3f);
                DrawCircle(tex, cx + x, cy + y, 2, col);
            }
        }
    }
}
