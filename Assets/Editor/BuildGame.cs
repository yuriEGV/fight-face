using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FightFace.Editor
{
    public static class BuildGame
    {
        [MenuItem("FightFace/Build Standalone Executable (Pendrive)")]
        public static void BuildWindowsExecutable()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildFolder = Path.Combine(projectRoot, "Build_Pendrive");

            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string exePath = Path.Combine(buildFolder, "FightFace.exe");
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

            Debug.Log($"[BuildGame] 🚀 Iniciando compilación de StandaloneWindows64...");
            Debug.Log($"[BuildGame] Destino: {exePath}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildGame] ✅ Compilación EXITOSA! Tamaño: {summary.totalSize / (1024 * 1024)} MB en {summary.totalTime.TotalSeconds:F1}s");

                // 1. Crear carpeta Luchadores limpia para que el usuario guarde sus propias fotos y voces
                string buildLuchadores = Path.Combine(buildFolder, "Luchadores");
                if (!Directory.Exists(buildLuchadores))
                {
                    Directory.CreateDirectory(buildLuchadores);
                }

                // 2. Crear archivo de instrucciones LEEME_PENDRIVE.txt
                string readmePath = Path.Combine(buildFolder, "COMO_JUGAR_PENDRIVE.txt");
                string readmeContent = 
@"========================================================================
   🥊 FIGHT FACE - EDICIÓN PORTABLE PARA PENDRIVE (PLUG & PLAY) 🥊
========================================================================

¡Este juego es 100% portable y no requiere instalar Unity ni programas extra!

--- CÓMO LLEVARLO EN TU PENDRIVE ---
1. Copia toda esta carpeta ('Build_Pendrive' o renómbrala a 'FightFace')
   directamente a tu pendrive o memoria USB.
2. Conecta el pendrive en cualquier computadora con Windows (64-bit).
3. Entra a la carpeta y haz doble clic en 'FightFace.exe' para jugar.

--- CONTROLES COMPATIBLES ---
🎮 JUGADOR 1 (Teclado o Joystick):
  - Moverse:          Teclas [W] [A] [S] [D]  o  Cruceta D-Pad / Stick Izquierdo
  - Salto:            Tecla [W]              o  Arriba / Stick Arriba
  - Golpe Ligero:     Tecla [F] o [Espacio]  o  Botón [X] (Xbox) / [Cuadrado] (PlayStation)
  - Patada Ligera:    Tecla [G] o [E]        o  Botón [A] (Xbox) / [Cruz] (PlayStation)
  - Golpe Fuerte:     Tecla [R] o [J]        o  Botón [Y] (Xbox) / [Triángulo] (PS) / [RB]
  - Patada Fuerte:    Tecla [T] o [U]        o  Botón [B] (Xbox) / [Círculo] (PS) / [RT]
  - HADOUKEN:         Tecla [Q] o [Y]        o  Gatillo [LT] / [L2]

🎮 JUGADOR 2 (Teclado o Segundo Joystick):
  - Moverse:          Flechas [Arriba] [Abajo] [Izq] [Der]
  - Golpes:           Teclas [L], [K], [P], [O], [Numpad 1, 2, 4, 5]
  - HADOUKEN:         Tecla [I] o [Numpad 3]

🎵 MÚSICA Y 🗣️ VOLUMEN DE VOCES:
  - [M] o [L3] (Click Stick Izq):  Activar / Silenciar Música de fondo por completo
  - [V] o [R3] (Click Stick Der):  Subir / Cambiar volumen de gritos y voces (100%, 150%, 200%, 250%)

📸 CARAS Y VOCES PERSONALIZADAS:
  - Presiona [C] en la pantalla de selección o durante el juego para
    personalizar las caras o grabar tus propios gritos con el micrófono.
  - Las fotos y grabaciones quedan guardadas en la carpeta 'Luchadores'
    junto al ejecutable para viajar contigo en el pendrive.

========================================================================
";
                File.WriteAllText(readmePath, readmeContent, System.Text.Encoding.UTF8);
                Debug.Log($"[BuildGame] 📝 Instrucciones creadas en: {readmePath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[BuildGame] ❌ Error en la compilación: {summary.totalErrors} errores reportados.");
            }
        }

        [MenuItem("FightFace/Build Android APK")]
        public static void BuildAndroidApk()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildFolder = Path.Combine(projectRoot, "Build_Android");

            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string apkPath = Path.Combine(buildFolder, "FightFace.apk");
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            EditorUserBuildSettings.buildAppBundle = false; // Generar archivo .apk instalable directamente

            Debug.Log($"[BuildGame] 📱 Iniciando compilación de Android APK...");
            Debug.Log($"[BuildGame] Destino: {apkPath}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildGame] ✅ APK generado con ÉXITO en: {apkPath} ({summary.totalSize / (1024 * 1024)} MB)");
            }
            else
            {
                Debug.LogError($"[BuildGame] ❌ Error al compilar APK: {summary.totalErrors} errores. (Recuerda instalar el módulo 'Android Build Support' desde Unity Hub).");
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, targetSubDir);
            }
        }
    }
}
