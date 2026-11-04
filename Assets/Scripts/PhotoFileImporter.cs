using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FightFace
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileNameStruct
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    /// <summary>
    /// Permite subir y adjuntar imágenes directamente desde el explorador de Windows:
    /// - Abre el selector nativo de Windows (OpenFileDialog) filtrando por PNG, JPG, JPEG, BMP.
    /// - Recorta y adapta automáticamente el rostro al formato de óvalo erguido de Fight Face.
    /// - Guarda en la carpeta del luchador correspondiente y actualiza la UI en vivo.
    /// - Permite abrir la carpeta de archivos en el explorador de Windows.
    /// </summary>
    public static class PhotoFileImporter
    {
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileNameStruct ofn);

        /// <summary>
        /// Abre el diálogo nativo de Windows para que el usuario elija una foto de su PC.
        /// </summary>
        public static string PromptSelectImageFile(string dialogTitle = "Selecciona una foto para Fight Face")
        {
            try
            {
                OpenFileNameStruct ofn = new OpenFileNameStruct();
                ofn.structSize = Marshal.SizeOf(ofn);
                ofn.filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp)\0*.png;*.jpg;*.jpeg;*.bmp\0Todos los archivos (*.*)\0*.*\0";
                ofn.file = new string(new char[260]);
                ofn.maxFile = ofn.file.Length;
                ofn.fileTitle = new string(new char[100]);
                ofn.maxFileTitle = ofn.fileTitle.Length;
                ofn.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                ofn.title = dialogTitle;
                // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
                ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

                if (GetOpenFileName(ofn))
                {
                    string rawPath = ofn.file;
                    if (!string.IsNullOrEmpty(rawPath))
                    {
                        int nullIdx = rawPath.IndexOf('\0');
                        if (nullIdx >= 0)
                        {
                            rawPath = rawPath.Substring(0, nullIdx);
                        }
                        rawPath = rawPath.Trim();
                        if (File.Exists(rawPath))
                        {
                            Debug.Log($"[PhotoFileImporter] Archivo seleccionado por el usuario: {rawPath}");
                            return rawPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhotoFileImporter] Error en diálogo nativo de Windows: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Carga una imagen desde el disco, la procesa al óvalo centrado y la asigna al luchador y emoción indicados.
        /// </summary>
        public static Sprite ImportAndApplyPhoto(string filePath, int fighterId, FaceType emotion, int headIndex = 0, float zoom = 1.0f)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogWarning($"[PhotoFileImporter] Archivo no encontrado: {filePath}");
                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!rawTex.LoadImage(bytes))
                {
                    Debug.LogError("[PhotoFileImporter] No se pudo decodificar el formato de la imagen.");
                    return null;
                }

                string filename = GetFilenameForEmotion(emotion);
                string suffix = headIndex > 0 ? $"_Head{headIndex + 1}" : "";
                string folderJugador = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{fighterId}{suffix}");
                string folderLuchador = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{fighterId}{suffix}");
                if (!Directory.Exists(folderJugador)) Directory.CreateDirectory(folderJugador);
                if (!Directory.Exists(folderLuchador)) Directory.CreateDirectory(folderLuchador);

                // Guardar copia Raw original para permitir manipulación, zoom y encuadre posterior
                FaceLoader.SaveTextureToFile(rawTex, Path.Combine(folderJugador, $"Raw_{filename}"));
                FaceLoader.SaveTextureToFile(rawTex, Path.Combine(folderLuchador, $"Raw_{filename}"));

                // Aplicar máscara ovalada centrada de Fight Face
                Texture2D ovalTex = FaceLoader.MaskAsOvalHead(rawTex, false, false, zoom);

                Sprite faceSprite = FaceLoader.CreateSpriteFromTexture(ovalTex);

                FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderJugador, filename));
                FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderLuchador, filename));

                // Nombres alternativos de compatibilidad (Rabia/Enojo, KO/Perdedor)
                if (emotion == FaceType.Enojo)
                {
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderJugador, "Foto_Rabia.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderJugador, "Foto_Enojo.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderLuchador, "Foto_Rabia.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderLuchador, "Foto_Enojo.png"));
                }
                else if (emotion == FaceType.Ganador)
                {
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderJugador, "Foto_Ganador.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderLuchador, "Foto_Ganador.png"));
                }
                else if (emotion == FaceType.KO)
                {
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderJugador, "Foto_KO.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderJugador, "Foto_Perdedor.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderLuchador, "Foto_KO.png"));
                    FaceLoader.SaveTextureToFile(ovalTex, Path.Combine(folderLuchador, "Foto_Perdedor.png"));
                }

                Debug.Log($"[PhotoFileImporter] ✅ Foto adjuntada con éxito para Luchador {fighterId} ({emotion}) en Jugador_{fighterId} y Luchador_{fighterId}");

                // Si WebcamCaptureManager está activo, actualizar su perfil y miniaturas
                if (WebcamCaptureManager.Instance != null && WebcamCaptureManager.Instance.targetFighterId == fighterId)
                {
                    if (WebcamCaptureManager.Instance.CurrentProfile != null)
                    {
                        WebcamCaptureManager.Instance.CurrentProfile.SetSprite(emotion, faceSprite);
                    }
                    if (WebcamCaptureManager.Instance.cameraPreviewUI != null && faceSprite != null && faceSprite.texture != null)
                    {
                        WebcamCaptureManager.Instance.cameraPreviewUI.texture = faceSprite.texture;
                        WebcamCaptureManager.Instance.cameraPreviewUI.enabled = true;
                    }
                    WebcamCaptureManager.Instance.SetActiveRawTexture(rawTex, emotion, filePath);
                    WebcamCaptureManager.Instance.UpdateThumbnailPreviews();
                    WebcamCaptureManager.Instance.ApplyToActiveFighters();
                }
                else
                {
                    UnityEngine.Object.Destroy(rawTex);
                }

                // Si CharacterSelectUI está activo, refrescar
                if (CharacterSelectUI.Instance != null)
                {
                    CharacterSelectUI.Instance.RefreshP1UI();
                    CharacterSelectUI.Instance.RefreshP2UI();
                }

                // Refrescar luchadores activos en la arena
                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.ApplyFighterSetup(BattleManager.Instance.p1BodyType, BattleManager.Instance.p2BodyType, BattleManager.Instance.isP2ControlledByAI);
                }

                return faceSprite;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PhotoFileImporter] Error al procesar imagen: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// (Obsoleto) Ya no se utilizan fotos de cuerpo para preservar la animación retro y detalles de golpes.
        /// </summary>
        public static Sprite ImportAndApplyBodyPhoto(string filePath, int fighterId)
        {
            return null;
        }

        /// <summary>
        /// Abre la carpeta de fotos del luchador en el Explorador de Windows.
        /// </summary>
        public static void OpenFighterPhotosInExplorer(int fighterId, int headIndex = 0)
        {
            string suffix = (headIndex > 0) ? $"_Head{headIndex + 1}" : "";
            string folder = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{fighterId}{suffix}");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", folder.Replace('/', '\\'));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhotoFileImporter] No se pudo abrir el explorador: {ex.Message}");
            }
        }

        public static string GetFilenameForEmotion(FaceType emotion)
        {
            switch (emotion)
            {
                case FaceType.Base: return "Foto_Base.png";
                case FaceType.Dolor: return "Foto_Dolor.png";
                case FaceType.Enojo: return "Foto_Rabia.png";
                case FaceType.Ganador: return "Foto_Ganador.png";
                case FaceType.KO: return "Foto_Perdedor.png";
                default: return "Foto_Base.png";
            }
        }
    }
}
