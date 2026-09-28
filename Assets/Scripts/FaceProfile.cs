using System;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Perfil facial de un luchador con las 4 expresiones clave.
    /// Soporta carga desde archivos locales, guardado en disco y asignación en tiempo de ejecución.
    /// </summary>
    [Serializable]
    public class FaceProfile
    {
        public string fighterName = "Luchador";

        public Sprite faceBase;
        public Sprite faceAngry;
        public Sprite faceHurt;
        public Sprite faceKO;

        public Sprite GetSprite(FaceType type)
        {
            switch (type)
            {
                case FaceType.Base:
                    return faceBase;
                case FaceType.Enojo:
                    return faceAngry != null ? faceAngry : faceBase;
                case FaceType.Dolor:
                    return faceHurt != null ? faceHurt : faceBase;
                case FaceType.KO:
                    return faceKO != null ? faceKO : faceBase;
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
                case FaceType.Enojo:
                    faceAngry = sprite;
                    break;
                case FaceType.Dolor:
                    faceHurt = sprite;
                    break;
                case FaceType.KO:
                    faceKO = sprite;
                    break;
            }
        }

        /// <summary>
        /// Carga las 4 caras desde una carpeta específica del disco.
        /// Busca Foto_Base.png, Foto_Enojo.png, Foto_Dolor.png, Foto_KO.png.
        /// </summary>
        public bool LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"[FaceProfile] El directorio no existe: {directoryPath}");
                return false;
            }

            string basePath = Path.Combine(directoryPath, "Foto_Base.png");
            string angryPath = Path.Combine(directoryPath, "Foto_Enojo.png");
            string hurtPath = Path.Combine(directoryPath, "Foto_Dolor.png");
            string koPath = Path.Combine(directoryPath, "Foto_KO.png");

            bool anyLoaded = false;

            if (File.Exists(basePath))
            {
                faceBase = FaceLoader.LoadSpriteFromFile(basePath);
                anyLoaded = true;
            }
            if (File.Exists(angryPath))
            {
                faceAngry = FaceLoader.LoadSpriteFromFile(angryPath);
                anyLoaded = true;
            }
            if (File.Exists(hurtPath))
            {
                faceHurt = FaceLoader.LoadSpriteFromFile(hurtPath);
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
        /// Guarda las 4 texturas de las caras en una carpeta del disco.
        /// </summary>
        public void SaveToDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            SaveSpriteToFile(faceBase, Path.Combine(directoryPath, "Foto_Base.png"));
            SaveSpriteToFile(faceAngry, Path.Combine(directoryPath, "Foto_Enojo.png"));
            SaveSpriteToFile(faceHurt, Path.Combine(directoryPath, "Foto_Dolor.png"));
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
