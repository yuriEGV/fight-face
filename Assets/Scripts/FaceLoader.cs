using System;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Utilidad estática para:
    /// 1. Cargar archivos PNG/JPG desde disco y convertirlos en Sprites en tiempo real.
    /// 2. Guardar Texturas a archivos PNG.
    /// 3. Generar rostros cómicos procedurales de muestra para pruebas inmediatas.
    /// </summary>
    public static class FaceLoader
    {
        /// <summary>
        /// Carga un archivo de imagen (PNG/JPG) desde el disco y devuelve un Sprite de Unity.
        /// </summary>
        public static Sprite LoadSpriteFromFile(string filePath, float pixelsPerUnit = 100f)
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
                
                // LoadImage redimensiona la textura automáticamente al tamaño de la imagen
                if (texture.LoadImage(fileData))
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    
                    // Pivote en el centro inferior (cuello) o centro (0.5, 0.5)
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        pixelsPerUnit
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
        /// Convierte una Texture2D en un Sprite.
        /// </summary>
        public static Sprite CreateSpriteFromTexture(Texture2D texture, float pixelsPerUnit = 100f)
        {
            if (texture == null) return null;
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
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

                // Asegurar que la textura sea legible
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

        /// <summary>
        /// Crea una copia legible de una textura si no tiene habilitada la lectura directa.
        /// </summary>
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
        /// Genera un perfil con 4 caras cómicas procedurales para que el juego
        /// sea 100% jugable de inmediato sin fotos externas obligatorias.
        /// </summary>
        public static FaceProfile CreateDefaultProceduralProfile(int playerIndex)
        {
            FaceProfile profile = new FaceProfile();
            profile.fighterName = playerIndex == 1 ? "Panchito 'El Bravo'" : "Rocky 'El Furioso'";

            Color skinColor = playerIndex == 1 ? new Color(1f, 0.85f, 0.7f) : new Color(0.95f, 0.8f, 0.65f);
            Color accentColor = playerIndex == 1 ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 0.9f);

            profile.faceBase = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Base, skinColor, accentColor));
            profile.faceAngry = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Enojo, skinColor, accentColor));
            profile.faceHurt = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Dolor, skinColor, accentColor));
            profile.faceKO = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.KO, skinColor, accentColor));

            return profile;
        }

        /// <summary>
        /// Dibuja proceduralmente una cara chistosa en pixel-art/cartoon de 128x128.
        /// </summary>
        private static Texture2D GenerateCartoonFaceTexture(FaceType emotion, Color skinColor, Color accentColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            // Limpiar fondo transparente
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.44f;
            Color outlineColor = new Color(0.12f, 0.1f, 0.15f, 1f);

            // Si está furioso/enojado, la piel se torna rojiza de furia
            Color currentSkin = emotion == FaceType.Enojo 
                ? Color.Lerp(skinColor, new Color(1f, 0.35f, 0.35f), 0.5f) 
                : (emotion == FaceType.KO ? Color.Lerp(skinColor, new Color(0.6f, 0.65f, 0.75f), 0.5f) : skinColor);

            // Dibuja la cabeza redonda con contorno grueso estilo cómic
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        if (dist >= radius - 4f)
                        {
                            tex.SetPixel(x, y, outlineColor);
                        }
                        else
                        {
                            tex.SetPixel(x, y, currentSkin);
                        }
                    }
                }
            }

            // Dibuja bandana / vincha en la frente
            int bandanaY = (int)(size * 0.68f);
            for (int y = bandanaY; y < bandanaY + 12; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius - 3f)
                    {
                        tex.SetPixel(x, y, accentColor);
                    }
                }
            }

            // Ojos según la emoción
            int eyeY = (int)(size * 0.52f);
            int leftEyeX = (int)(size * 0.36f);
            int rightEyeX = (int)(size * 0.64f);

            switch (emotion)
            {
                case FaceType.Base:
                    // Ojos normales sonrientes
                    DrawCircle(tex, leftEyeX, eyeY, 6, Color.white);
                    DrawCircle(tex, rightEyeX, eyeY, 6, Color.white);
                    DrawCircle(tex, leftEyeX + 1, eyeY, 3, outlineColor);
                    DrawCircle(tex, rightEyeX - 1, eyeY, 3, outlineColor);
                    // Boca con sonrisa confiada
                    DrawMouth(tex, (int)(size * 0.5f), (int)(size * 0.28f), 14, 6, outlineColor, true);
                    break;

                case FaceType.Enojo:
                    // Ojos de furia / odio con cejas inclinadas hacia adentro y venas saltadas
                    DrawCircle(tex, leftEyeX, eyeY, 7, Color.white);
                    DrawCircle(tex, rightEyeX, eyeY, 7, Color.white);
                    DrawCircle(tex, leftEyeX, eyeY, 4, new Color(0.9f, 0.1f, 0.1f)); // Pupila roja
                    // Cejas enfurecidas en V
                    DrawLine(tex, leftEyeX - 10, eyeY + 10, leftEyeX + 10, eyeY + 3, outlineColor, 4);
                    DrawLine(tex, rightEyeX + 10, eyeY + 10, rightEyeX - 10, eyeY + 3, outlineColor, 4);
                    // Boca gritando de furia con colmillos
                    DrawOpenMouth(tex, (int)(size * 0.5f), (int)(size * 0.26f), 18, 12, outlineColor, new Color(0.7f, 0.1f, 0.1f));
                    break;

                case FaceType.Dolor:
                    // Ojos apretados de dolor / ojo morado
                    DrawX(tex, leftEyeX, eyeY, 7, outlineColor, 3);
                    DrawCircle(tex, rightEyeX, eyeY, 9, new Color(0.4f, 0.2f, 0.5f, 0.7f)); // Ojo morado
                    DrawCircle(tex, rightEyeX, eyeY, 5, outlineColor);
                    // Boca de mueca gritando dolor
                    DrawWobblyMouth(tex, (int)(size * 0.5f), (int)(size * 0.28f), 16, outlineColor);
                    // Gotas de sudor cómicas
                    DrawCircle(tex, leftEyeX - 12, eyeY + 14, 4, new Color(0.3f, 0.7f, 1f));
                    break;

                case FaceType.KO:
                    // Ojos en cruz (X X) típicos de noqueado
                    DrawX(tex, leftEyeX, eyeY, 8, outlineColor, 3);
                    DrawX(tex, rightEyeX, eyeY, 8, outlineColor, 3);
                    // Lengua afuera cómica
                    DrawCircle(tex, (int)(size * 0.5f) + 4, (int)(size * 0.22f), 6, new Color(1f, 0.4f, 0.5f));
                    DrawLine(tex, (int)(size * 0.38f), (int)(size * 0.26f), (int)(size * 0.62f), (int)(size * 0.26f), outlineColor, 3);
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
            // Dientes blancos arriba
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
