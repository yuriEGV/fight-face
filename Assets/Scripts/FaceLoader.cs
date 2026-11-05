using System;
using System.Collections.Generic;
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
        public const float TARGET_HEAD_WORLD_HEIGHT = 1.35f; // Cabeza sobredimensionada cómica (130%) para muecas claras y legibles

        /// <summary>
        /// Carga un archivo de imagen (PNG/JPG) desde el disco y devuelve un Sprite de Unity
        /// perfectamente calibrado para el tamaño del cuerpo del luchador.
        /// Aplica automáticamente máscara ovalada de sticker si la imagen es rectangular.
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

                    // Si la foto no tiene esquinas transparentes, recortar automáticamente en óvalo sticker
                    Color corner = texture.GetPixel(0, 0);
                    if (corner.a > 0.5f)
                    {
                        Texture2D masked = MaskAsOvalHead(texture, false, false, 2.70f);
                        texture = masked;
                    }

                    // Calibrar PixelsPerUnit para que la cabeza mida exactamente ~1.35 unidades de mundo
                    float ppu = texture.height / TARGET_HEAD_WORLD_HEIGHT;

                    // Pivote en (0.5, 0.18): justo en el mentón para descansar naturalmente sobre el cuello
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.18f),
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
        /// Convierte una Texture2D en un Sprite con tamaño normalizado y pivote en el mentón.
        /// </summary>
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null) return null;
            float ppu = texture.height / TARGET_HEAD_WORLD_HEIGHT;
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.18f),
                ppu
            );
        }

        /// <summary>
        /// Aplica una máscara redondeada estilizada con zoom cerrado al rostro y borde de cómic sutil:
        /// - Zoom cerrado enfocado en facciones (ojos, nariz, boca, mejillas) para eliminar fondo, paredes y pizarras.
        /// - Contorno redondeado que ocupa prácticamente el rango completo del círculo (97-98%).
        /// - Borde antialiased limpio sin cortes dentados.
        /// </summary>
        public static Texture2D MaskAsOvalHead(Texture2D source, bool flipY = false, bool flipX = false, float zoom = 2.70f, float panX = 0f, float panY = 0f)
        {
            if (source == null) return null;
            zoom = Mathf.Max(0.4f, zoom);

            int minDim = Mathf.Min(source.width, source.height);
            int size = Mathf.Min(minDim, 512);

            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;

            Color transparent = new Color(0, 0, 0, 0);
            Color borderOutline = new Color(0.10f, 0.10f, 0.13f, 1f);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            // Proporción redondeada que abarca el rango completo disponible de la imagen
            float radiusX = size * 0.485f;
            float radiusY = size * 0.490f;

            // Centro del rostro calibrado con desplazamiento horizontal (panX) y vertical (panY)
            float srcCenterX = source.width * (0.5f + panX);
            float srcCenterY = source.height * (0.535f + panY);
            float sampleRadius = (minDim * 0.5f) / zoom;

            for (int y = 0; y < size; y++)
            {
                // dy normalizado (-1 en mentón, +1 en frente)
                float ny = (y - center.y) / radiusY;
                if (Mathf.Abs(ny) > 1.0f)
                {
                    for (int x = 0; x < size; x++) result.SetPixel(x, y, transparent);
                    continue;
                }

                // Ahusamiento sutil y natural hacia el mentón
                float taper = 1.0f + (0.05f * ny);
                float currRx = radiusX * taper;

                float normYOffset = (y - center.y) / (size * 0.5f);
                float sampledSrcY = flipY
                    ? srcCenterY - (normYOffset * sampleRadius)
                    : srcCenterY + (normYOffset * sampleRadius);

                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center.x) / currRx;
                    float distSq = (nx * nx) + (ny * ny);

                    if (distSq > 1.02f)
                    {
                        result.SetPixel(x, y, transparent);
                    }
                    else if (distSq > 0.985f)
                    {
                        // Borde suave antialiased exterior
                        float alpha = Mathf.Clamp01((1.02f - distSq) / 0.035f);
                        Color c = borderOutline;
                        c.a = alpha;
                        result.SetPixel(x, y, c);
                    }
                    else if (distSq > 0.955f)
                    {
                        // Contorno fino estilo cómic
                        result.SetPixel(x, y, borderOutline);
                    }
                    else
                    {
                        // Coordenada horizontal en la imagen fuente
                        float normXOffset = (x - center.x) / (size * 0.5f);
                        float sampledSrcX = flipX
                            ? srcCenterX - (normXOffset * sampleRadius)
                            : srcCenterX + (normXOffset * sampleRadius);

                        if (sampledSrcX < 0 || sampledSrcX >= source.width || sampledSrcY < 0 || sampledSrcY >= source.height)
                        {
                            result.SetPixel(x, y, transparent);
                        }
                        else
                        {
                            float u = Mathf.Clamp01(sampledSrcX / source.width);
                            float v = Mathf.Clamp01(sampledSrcY / source.height);
                            Color pixel = source.GetPixelBilinear(u, v);
                            result.SetPixel(x, y, pixel);
                        }
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
            profile.faceWinner = CreateSpriteFromTexture(GenerateCartoonFaceTexture(FaceType.Ganador, skin, band));
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

                case FaceType.Ganador:
                    DrawCircle(tex, leftEyeX, eyeY, 7, Color.white);
                    DrawCircle(tex, rightEyeX, eyeY, 7, Color.white);
                    DrawCircle(tex, leftEyeX + 1, eyeY, 3, outlineColor);
                    DrawCircle(tex, rightEyeX - 1, eyeY, 3, outlineColor);
                    DrawOpenMouth(tex, (int)(size * 0.5f), (int)(size * 0.28f), 24, 12, outlineColor, new Color(0.95f, 0.25f, 0.35f));
                    DrawCircle(tex, leftEyeX + 14, eyeY + 12, 4, new Color(1f, 0.9f, 0.2f));
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

        // ----------------- CARGADORES DE LOS 5 CUERPOS CLÁSICOS -----------------

        public static string GetFighterDefaultName(FighterBodyType bodyType)
        {
            switch (bodyType)
            {
                case FighterBodyType.Gordo: return "El Gordo";
                case FighterBodyType.Flaco: return "El Flaco";
                case FighterBodyType.Musculoso: return "El Musculoso";
                case FighterBodyType.Mujer: return "La Mujer";
                case FighterBodyType.Mujer2: return "La Guerrera";
                case FighterBodyType.DosCabezas: return "Hombre-Máquina";
                case FighterBodyType.Playero: return "El Playero";
                case FighterBodyType.Oficinista: return "Oficinista Furioso";
                case FighterBodyType.Pijamero: return "El Pijamero";
                default: return "Luchador";
            }
        }

        public static string GetFighterDisplayName(FighterBodyType bodyType)
        {
            int fId = (int)bodyType + 1;
            return GetSavedFighterName(fId, GetFighterDefaultName(bodyType));
        }

        public static string GetSavedFighterName(int fighterId, string defaultName)
        {
            string key = $"FighterName_{fighterId}";
            if (PlayerPrefs.HasKey(key))
            {
                string saved = PlayerPrefs.GetString(key);
                if (!string.IsNullOrEmpty(saved)) return saved;
            }
            return defaultName;
        }

        public static void SaveFighterName(int fighterId, string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;
            PlayerPrefs.SetString($"FighterName_{fighterId}", newName);
            PlayerPrefs.Save();
        }

        public static string GetFighterDescription(FighterBodyType bodyType)
        {
            switch (bodyType)
            {
                case FighterBodyType.Gordo: return "Poderoso y pesado. Gran resistencia física y golpes demoledores.";
                case FighterBodyType.Flaco: return "Maestro de Kung-Fu ágil y rápido. Golpes veloces estilo Bruce Lee.";
                case FighterBodyType.Musculoso: return "Campeón invicto de boxeo y Muay Thai. Puños devastadores.";
                case FighterBodyType.Mujer: return "Experta en kickboxing. Patadas voladoras de gran alcance.";
                case FighterBodyType.Mujer2: return "Maestra de artes marciales y ninjutsu. Gran velocidad y técnicas letales.";
                case FighterBodyType.DosCabezas: return "Hombre-Máquina Graciosa: Cyborg cómico de combate con cañón de confeti, control NES, tetera humeante y botas de pato.";
                case FighterBodyType.Playero: return "¡El rey del verano! Pelea con su flotador de patito inflable amarillo y patadas con salpicaduras.";
                case FighterBodyType.Oficinista: return "¡Harto del trabajo! Ataca con su maletín de cuero ejecutivo y corbata desatada al viento.";
                case FighterBodyType.Pijamero: return "¡Recién despertado de la siesta! Golpea con pantuflas de conejo de peluche y pijama de estrellas.";
                default: return "";
            }
        }

        public static Vector3 GetNeckLocalOffset(FighterBodyType bodyType)
        {
            switch (bodyType)
            {
                case FighterBodyType.Gordo: return new Vector3(-0.02f, 1.86f, 0);
                case FighterBodyType.Flaco: return new Vector3(-0.02f, 1.69f, 0);
                case FighterBodyType.Musculoso: return new Vector3(0.19f, 2.12f, 0);
                case FighterBodyType.Mujer: return new Vector3(0.60f, 2.19f, 0);
                case FighterBodyType.Mujer2: return new Vector3(0.00f, 1.71f, 0);
                case FighterBodyType.DosCabezas: return new Vector3(0.00f, 1.90f, 0);
                case FighterBodyType.Playero: return new Vector3(0.05f, 1.95f, 0);
                case FighterBodyType.Oficinista: return new Vector3(0.00f, 1.85f, 0);
                case FighterBodyType.Pijamero: return new Vector3(-0.02f, 1.80f, 0);
                default: return new Vector3(0, 1.85f, 0);
            }
        }

        public static Vector3 GetRightNeckLocalOffset(FighterBodyType bodyType)
        {
            return GetNeckLocalOffset(bodyType);
        }

        private static readonly Dictionary<string, Sprite> modularCache = new Dictionary<string, Sprite>();

        public static Sprite LoadModularPart(string folder, string partName)
        {
            return LoadModularPart(folder, partName, new Vector2(0.5f, 0.5f), 320f);
        }

        public static Sprite LoadModularPart(string folder, string partName, Vector2 pivot, float pixelsPerUnit = 320f)
        {
            string key = $"{folder}/{partName}_{pivot.x:F2}_{pivot.y:F2}_{pixelsPerUnit:F0}";
            if (modularCache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            string path = Path.Combine(Application.dataPath, "Sprites", "Modular", folder, partName + ".png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit);
                    s.name = partName;
                    modularCache[key] = s;
                    return s;
                }
            }
            return null;
        }

        public static Sprite LoadFighterBodySprite(FighterBodyType bodyType)
        {
            string fname = $"Body_{bodyType}_headless.png";
            string path = Path.Combine(Application.dataPath, "Sprites", "Fighters", fname);
            if (!File.Exists(path))
            {
                fname = $"Sprite_{bodyType}.png";
                path = Path.Combine(Application.dataPath, "Sprites", "Fighters", fname);
            }

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 175f);
                }
            }
            return null;
        }

        /// <summary>
        /// Carga la tarjeta ilustrada oficial del arquetipo del luchador.
        /// </summary>
        public static Sprite LoadFighterBodySprite(int fighterId, FighterBodyType bodyType)
        {
            return LoadFighterCardSprite(bodyType);
        }

        public static Sprite LoadFighterCardSprite(FighterBodyType bodyType)
        {
            string fname = $"Fighter_{bodyType}_card.png";
            string path = Path.Combine(Application.dataPath, "Sprites", "Fighters", fname);
            if (!File.Exists(path))
            {
                fname = $"Fighter_{bodyType}_full.png";
                path = Path.Combine(Application.dataPath, "Sprites", "Fighters", fname);
            }

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            // Arquetipos extendidos comparten sprites de los luchadores base
            switch (bodyType)
            {
                case FighterBodyType.Playero: return LoadFighterCardSprite(FighterBodyType.Musculoso);
                case FighterBodyType.Oficinista: return LoadFighterCardSprite(FighterBodyType.Flaco);
                case FighterBodyType.Pijamero: return LoadFighterCardSprite(FighterBodyType.Gordo);
                case FighterBodyType.DosCabezas: return LoadFighterCardSprite(FighterBodyType.Gordo);
                default: return null;
            }
        }

        public static Sprite LoadFighterThumbSprite(FighterBodyType bodyType)
        {
            string fname = $"Fighter_{bodyType}_thumb.png";
            string path = Path.Combine(Application.dataPath, "Sprites", "Fighters", fname);
            if (!File.Exists(path))
            {
                fname = $"Fighter_{bodyType}_card.png";
                path = Path.Combine(Application.dataPath, "Sprites", "Fighters", fname);
            }

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            // Fallback de miniaturas para arquetipos cómicos
            switch (bodyType)
            {
                case FighterBodyType.Playero: return LoadFighterThumbSprite(FighterBodyType.Musculoso);
                case FighterBodyType.Oficinista: return LoadFighterThumbSprite(FighterBodyType.Flaco);
                case FighterBodyType.Pijamero: return LoadFighterThumbSprite(FighterBodyType.Gordo);
                case FighterBodyType.DosCabezas: return LoadFighterThumbSprite(FighterBodyType.Gordo);
                default: return null;
            }
        }

        public static StageType GetHomeStageForFighter(FighterBodyType body)
        {
            switch (body)
            {
                case FighterBodyType.Gordo: return StageType.GordoMarket;
                case FighterBodyType.Flaco: return StageType.FlacoDojo;
                case FighterBodyType.Musculoso: return StageType.MusculosoBeach;
                case FighterBodyType.Mujer: return StageType.MujerRooftop;
                case FighterBodyType.Mujer2: return StageType.GuerreraBamboo;
                case FighterBodyType.DosCabezas: return StageType.DosCabezasScrapyard;
                case FighterBodyType.Playero: return StageType.MusculosoBeach;
                case FighterBodyType.Oficinista: return StageType.MujerRooftop;
                case FighterBodyType.Pijamero: return StageType.FlacoDojo;
                default: return StageType.GordoMarket;
            }
        }

        public static string GetStageDisplayName(StageType stage, FighterBodyType fallbackOpponent = FighterBodyType.Gordo)
        {
            switch (stage)
            {
                case StageType.Auto: return $"🏠 AUTO (Ring de {GetFighterDisplayName(fallbackOpponent)})";
                case StageType.GordoMarket: return $"🐟 MERCADO CALLEJERO ({GetFighterDisplayName(FighterBodyType.Gordo)})";
                case StageType.FlacoDojo: return $"⛩️ TEMPLO DOJO DRAGÓN ({GetFighterDisplayName(FighterBodyType.Flaco)})";
                case StageType.MusculosoBeach: return $"🌴 PLAYA MUAY THAI ({GetFighterDisplayName(FighterBodyType.Musculoso)})";
                case StageType.MujerRooftop: return $"🏙️ AZOTEA CYBERPUNK ({GetFighterDisplayName(FighterBodyType.Mujer)})";
                case StageType.GuerreraBamboo: return $"🎋 BOSQUE DE BAMBÚ ({GetFighterDisplayName(FighterBodyType.Mujer2)})";
                case StageType.DosCabezasScrapyard: return $"⚙️ HANGAR ROBÓTICO MECHA ({GetFighterDisplayName(FighterBodyType.DosCabezas)})";
                case StageType.MGMGrand: return "🏟️ MGM GRAND ARENA (Las Vegas - Gran Final)";
                default: return "🏟️ MGM GRAND ARENA";
            }
        }

        public static string GetStageDescription(StageType stage, FighterBodyType fallbackOpponent = FighterBodyType.Gordo)
        {
            switch (stage)
            {
                case StageType.Auto: return $"Selecciona dinámicamente el ring local de {GetFighterDisplayName(fallbackOpponent)} según su origen.";
                case StageType.GordoMarket: return $"Lonja tradicional y mercado nocturno de comida callejera de {GetFighterDisplayName(FighterBodyType.Gordo)}.";
                case StageType.FlacoDojo: return $"Antiguo santuario Shaolin y dojo de artes marciales de {GetFighterDisplayName(FighterBodyType.Flaco)}.";
                case StageType.MusculosoBeach: return $"Campamento de Muay Thai al aire libre en la arena frente al mar de {GetFighterDisplayName(FighterBodyType.Musculoso)}.";
                case StageType.MujerRooftop: return $"Plataforma en lo alto de un rascacielos iluminada por neones de {GetFighterDisplayName(FighterBodyType.Mujer)}.";
                case StageType.GuerreraBamboo: return $"Claro místico en el bosque de bambú iluminado por luna llena de {GetFighterDisplayName(FighterBodyType.Mujer2)}.";
                case StageType.DosCabezasScrapyard: return $"Taller mecha de alta tecnología y robótica de {GetFighterDisplayName(FighterBodyType.DosCabezas)}.";
                case StageType.MGMGrand: return "El gran cuadrilátero de campeonato mundial bajo focos y multitud de Las Vegas.";
                default: return "";
            }
        }

        public static Sprite LoadStageSprite(StageType stage, FighterBodyType fallbackOpponent = FighterBodyType.Gordo)
        {
            StageType target = stage;
            if (target == StageType.Auto)
            {
                target = GetHomeStageForFighter(fallbackOpponent);
            }

            string filename;
            switch (target)
            {
                case StageType.GordoMarket: filename = "Stage_Gordo_Market.png"; break;
                case StageType.FlacoDojo: filename = "Stage_Flaco_Dojo.png"; break;
                case StageType.MusculosoBeach: filename = "Stage_Musculoso_Beach.png"; break;
                case StageType.MujerRooftop: filename = "Stage_Mujer_Rooftop.png"; break;
                case StageType.GuerreraBamboo: filename = "Stage_Guerrera_Bamboo.png"; break;
                case StageType.DosCabezasScrapyard: filename = "Stage_DosCabezas_Scrapyard.png"; break;
                case StageType.MGMGrand:
                default: filename = "Stage_MGM_Grand.png"; break;
            }

            string[] candidatePaths = {
                Path.Combine(Application.dataPath, "Sprites", "Stages", filename),
                Path.Combine(Application.dataPath, "Sprites", filename),
                Path.Combine(Application.dataPath, "Sprites", "Fighters", filename),
                Path.Combine(Application.dataPath, "Sprites", "Stages", "Stage_MGM_Grand.png"),
                Path.Combine(Application.dataPath, "Sprites", "Fighters", "Stage_MGM_Grand.png")
            };

            foreach (string path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes))
                        {
                            tex.filterMode = FilterMode.Bilinear;
                            tex.wrapMode = TextureWrapMode.Clamp;
                            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                            s.name = Path.GetFileNameWithoutExtension(filename);
                            return s;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FaceLoader] Error al cargar escenario desde {path}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        public static Sprite LoadStageMGMSprite()
        {
            return LoadStageSprite(StageType.MGMGrand);
        }

        public static Sprite LoadModularPartSprite(string characterName, string partName, Vector2 pivot, float pixelsPerUnit = 220f)
        {
            string path = Path.Combine(Application.dataPath, "Sprites", "Modular", characterName, $"{partName}.png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit);
                }
            }
            return null;
        }

        public static Sprite LoadModularPartSprite(string characterName, string partName)
        {
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (partName.Contains("Torso")) pivot = new Vector2(0.5f, 0.05f);
            else if (partName.Contains("Pelvis")) pivot = new Vector2(0.5f, 0.55f);
            else if (partName.Contains("Upper") || partName.Contains("Forearm") || partName.Contains("Thigh") || partName.Contains("Calf")) pivot = new Vector2(0.5f, 0.90f);
            else if (partName.Contains("Hand")) pivot = new Vector2(0.5f, 0.85f);
            else if (partName.Contains("Head")) pivot = new Vector2(0.5f, 0.15f);

            return LoadModularPartSprite(characterName, partName, pivot, 220f);
        }

        public static Sprite LoadStageStreetSprite()
        {
            return LoadStageMGMSprite();
        }

        public static FaceProfile LoadProfileForFighter(int id, string defaultName, int headIndex = 0)
        {
            FaceProfile profile = new FaceProfile();
            profile.fighterId = id;
            profile.fighterName = defaultName;

            string suffix = headIndex > 0 ? $"_Head{headIndex + 1}" : "";
            string dirLocal1 = Path.Combine(Application.dataPath, "..", "Luchadores", $"Jugador_{id}{suffix}");
            string dirLocal2 = Path.Combine(Application.dataPath, "..", "Luchadores", $"Luchador_{id}{suffix}");
            string dir1 = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{id}{suffix}");
            string dir2 = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{id}{suffix}");

            if (Directory.Exists(dirLocal1) && profile.LoadFromDirectory(dirLocal1))
            {
                return profile;
            }
            if (Directory.Exists(dirLocal2) && profile.LoadFromDirectory(dirLocal2))
            {
                return profile;
            }
            if (Directory.Exists(dir1) && profile.LoadFromDirectory(dir1))
            {
                return profile;
            }
            if (Directory.Exists(dir2) && profile.LoadFromDirectory(dir2))
            {
                return profile;
            }

            FaceProfile def = CreateDefaultProceduralProfile(id + (headIndex * 10));
            def.fighterName = defaultName;
            return def;
        }
    }
}
