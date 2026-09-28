using System;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Perfil facial de un luchador con las 4 expresiones clave según el diseño completo:
    /// 1. Foto Normal (Foto_Base.png): Mirando a cámara, boca relajada.
    /// 2. Foto Dolor (Foto_Dolor.png): Expresión de dolor, rostro contraído.
    /// 3. Foto Rabia (Foto_Rabia.png o Foto_Enojo.png): Expresión agresiva / Rage.
    /// 4. Foto Ganador (Foto_Ganador.png): Sonrisa / celebración de victoria.
    /// </summary>
    [Serializable]
    public class FaceProfile
    {
        public int fighterId = 1;
        public string fighterName = "Luchador";

        public Sprite faceBase;     // 1. Normal
        public Sprite faceHurt;     // 2. Dolor
        public Sprite faceAngry;    // 3. Rabia / Enojo
        public Sprite faceWinner;   // 4. Ganador (¡Celebración de Victoria!)
        public Sprite faceKO;       // Derrota / K.O.

        public Sprite GetSprite(FaceType type)
        {
            switch (type)
            {
                case FaceType.Base:
                    return faceBase;
                case FaceType.Dolor:
                    return faceHurt != null ? faceHurt : faceBase;
                case FaceType.Enojo:
                    return faceAngry != null ? faceAngry : faceBase;
                case FaceType.Ganador:
                    return faceWinner != null ? faceWinner : (faceBase != null ? faceBase : faceAngry);
                case FaceType.KO:
                    return faceKO != null ? faceKO : (faceHurt != null ? faceHurt : faceBase);
                default:
                    return faceBase;
            }
        }

        public void SetSprite(FaceType type, Sprite sprite)
        {
            switch (type)
            {
                case FaceType.Base:
                    faceBase = sprite;
                    break;
                case FaceType.Dolor:
                    faceHurt = sprite;
                    break;
                case FaceType.Enojo:
                    faceAngry = sprite;
                    break;
                case FaceType.Ganador:
                    faceWinner = sprite;
                    break;
                case FaceType.KO:
                    faceKO = sprite;
                    break;
            }
        }

        /// <summary>
        /// Carga las 4 caras desde una carpeta específica del disco.
        /// </summary>
        public bool LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"[FaceProfile] El directorio no existe: {directoryPath}");
                return false;
            }

            string basePath = Path.Combine(directoryPath, "Foto_Base.png");
            string hurtPath = Path.Combine(directoryPath, "Foto_Dolor.png");

            string angryPath = Path.Combine(directoryPath, "Foto_Rabia.png");
            if (!File.Exists(angryPath)) angryPath = Path.Combine(directoryPath, "Foto_Enojo.png");

            string winnerPath = Path.Combine(directoryPath, "Foto_Ganador.png");
            string koPath = Path.Combine(directoryPath, "Foto_KO.png");

            bool anyLoaded = false;

            if (File.Exists(basePath))
            {
                faceBase = FaceLoader.LoadSpriteFromFile(basePath);
                anyLoaded = true;
            }
            if (File.Exists(hurtPath))
            {
                faceHurt = FaceLoader.LoadSpriteFromFile(hurtPath);
                anyLoaded = true;
            }
            if (File.Exists(angryPath))
            {
                faceAngry = FaceLoader.LoadSpriteFromFile(angryPath);
                anyLoaded = true;
            }
            if (File.Exists(winnerPath))
            {
                faceWinner = FaceLoader.LoadSpriteFromFile(winnerPath);
                anyLoaded = true;
            }
            if (File.Exists(koPath))
            {
                faceKO = FaceLoader.LoadSpriteFromFile(koPath);
                anyLoaded = true;
            }

            return anyLoaded;
        }

        /// <summary>
        /// Guarda las texturas de las caras en disco.
        /// </summary>
        public void SaveToDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            SaveSpriteToFile(faceBase, Path.Combine(directoryPath, "Foto_Base.png"));
            SaveSpriteToFile(faceHurt, Path.Combine(directoryPath, "Foto_Dolor.png"));
            SaveSpriteToFile(faceAngry, Path.Combine(directoryPath, "Foto_Rabia.png"));
            SaveSpriteToFile(faceAngry, Path.Combine(directoryPath, "Foto_Enojo.png"));
            SaveSpriteToFile(faceWinner, Path.Combine(directoryPath, "Foto_Ganador.png"));
            SaveSpriteToFile(faceKO, Path.Combine(directoryPath, "Foto_KO.png"));

            Debug.Log($"[FaceProfile] Caras guardadas exitosamente en: {directoryPath}");
        }

        private void SaveSpriteToFile(Sprite sprite, string filePath)
        {
            if (sprite == null || sprite.texture == null) return;
            FaceLoader.SaveTextureToFile(sprite.texture, filePath);
        }
    }
}
