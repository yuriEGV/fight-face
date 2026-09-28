# 🥊 Fight Face - 2D Brawler con Rostros Personalizados

¡Bienvenido a **Fight Face**! Un juego de combate 2D estilo "brawler" cósmico y chistoso inspirado en *Photo Dojo*, *MUGEN* y los modos party de lucha libre, donde el rostro de los personajes reacciona dinámicamente con fotos reales de los jugadores tomadas con la cámara web o cargadas desde el almacenamiento local.

---

## 🎭 Sistema de las 4 Caras Dinámicas

Cada luchador posee un perfil facial (`FaceProfile`) con 4 expresiones emocionales clave que cambian en tiempo real durante la pelea:

1. **`Foto_Base.png` (Rostro Normal / Neutral)**:
   - Expresión por defecto en reposo (Idle) y mientras camina por el ring.
2. **`Foto_Enojo.png` (¡Rostro de Enojo, Furia y Odio!)**:
   - **¡La 4ta cara del sistema!** Se activa instantáneamente al lanzar ataques (puñetazos, patadas). El personaje grita y desata su furia con rebote elástico en la cabeza.
3. **`Foto_Dolor.png` (Rostro de Dolor y Mueca)**:
   - Se activa cuando el personaje recibe un impacto o daño. La cabeza se sacude, tiembla cómicamente y se muestra una mueca de dolor o quejido antes de volver a su pose base.
4. **`Foto_KO.png` (Rostro Noqueado / Ojos Cerrados o en Cruz)**:
   - Se activa de forma permanente cuando los puntos de salud (HP) llegan a 0. El luchador cae de espaldas derrotado en la lona del ring.

---

## 🕹️ Controles del Juego

| Acción | Jugador 1 (P1) | Jugador 2 (P2) |
|---|---|---|
| **Moverse Izquierda / Derecha** | `A` / `D` | `←` / `→` |
| **Saltar** | `W` | `↑` |
| **Puñetazo (Cara de Enojo)** | `F` o `Barra Espaciadora` | `L` o `Keypad 1` |
| **Patada Fuerte** | `G` o `E` | `K` o `Keypad 2` |

### 🛠️ Controles Globales
- **`[C]`**: Abre / Cierra el panel de **Personalización de Caras (Webcam y Fotos)**.
- **`[R]`**: Reinicia el combate inmediatamente (**Revancha Instantánea**).
- *Nota:* El Jugador 2 cuenta con **Modo IA / CPU Bot** activado por defecto para poder pelear y probar el juego de inmediato en solitario.

---

## 📷 Flujo de Captura con Cámara Web (Webcam)

1. Presiona el botón **`[C] Caras / Webcam`** en la barra inferior o la tecla `C`.
2. Se abrirá el modal con el visor en vivo de tu webcam.
3. Selecciona a quién quieres personalizar: **Modificar Jugador 1** o **Modificar Jugador 2**.
4. Posa frente a la cámara y presiona cada uno de los 4 botones de captura:
   - 📸 **1. Foto BASE**: Haz una pose neutral o seria.
   - 📸 **2. Foto ENOJO**: Haz una cara de rabia, furia, odio o grito de guerra.
   - 📸 **3. Foto DOLOR**: Haz una mueca de quejido o dolor ("¡ouch!").
   - 📸 **4. Foto K.O.**: Cierra los ojos o quédate con la lengua afuera / mirada perdida.
5. Las fotos se guardan automáticamente en tu dispositivo en:
   `%USERPROFILE%/AppData/LocalLow/<Company>/fight face/Luchadores/Jugador_X/`
   como `Foto_Base.png`, `Foto_Enojo.png`, `Foto_Dolor.png` y `Foto_KO.png`.
6. Presiona **`⚔️ ¡LISTO! IR A PELEAR`** y tus expresiones faciales cobrarán vida en el ring.

> **💡 Modo sin cámara:** Si no dispones de cámara web en este momento, el juego genera automáticamente caras y cuerpos procedurales de cómic con ojos enfurecidos, moratones y cruces de KO para que sea 100% jugable de inmediato.

---

## 🏗️ Arquitectura de Scripts (`Assets/Scripts/`)

- [`FaceType.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FaceType.cs): Enumeración de las 4 expresiones (`Base`, `Enojo`, `Dolor`, `KO`).
- [`FaceProfile.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FaceProfile.cs): Almacén de sprites de las 4 caras, serialización y carga/guardado en disco.
- [`FaceLoader.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FaceLoader.cs): Utilidad para convertir bytes/PNGs en `Texture2D` y `Sprite`, y generador de avatares cómicos procedurales.
- [`DynamicFaceController.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/DynamicFaceController.cs): Controla el `SpriteRenderer` de la cabeza, cambios de estado temporal (golpes/daño) y micro-animaciones (squash & stretch, temblor).
- [`FighterBodyController.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FighterBodyController.cs): Gestiona y anima proceduralmente el torso, brazos, puños, piernas y pies del luchador.
- [`FighterController.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FighterController.cs): Física 2D, salud, detección de ataques, máquina de estados, lógica de CPU bot y transiciones entre las 4 caras.
- [`Hitbox.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/Hitbox.cs): Trigger 2D para puños y patadas con retroceso físico (knockback).
- [`CombatEffectsManager.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/CombatEffectsManager.cs): Textos flotantes estilo cómic ("¡POW!", "¡BAM!", "¡OUCH!"), Hit-Stop arcade y sacudida de pantalla.
- [`WebcamCaptureManager.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/WebcamCaptureManager.cs): Alimentación de video `WebCamTexture`, recorte cuadrado y persistencia en PNG.
- [`BattleUI.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/BattleUI.cs): Barras de salud dinámicas, retratos de miniatura reactivos en tiempo real, contador de tiempo 99s y banners.
- [`BattleManager.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/BattleManager.cs): Control de rondas, tiempo fuera, K.O. y reinicio de partidas.
- [`FightGameBootstrap.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FightGameBootstrap.cs): Constructor automático que autoensambla el ring, luces, personajes y UI al presionar **Play**.
- [`FightInput.cs`](file:///c:/Users/maritimo13/fight%20face/fight-face/Assets/Scripts/FightInput.cs): Adaptador compatible con Unity 6 y el nuevo Input System.

---

## 🚀 Cómo Ejecutar en Unity

1. Abre el proyecto en **Unity 6 (6000.3.0f1 LTS)**.
2. Abre la escena `Assets/Scenes/SampleScene.unity`.
3. Presiona el botón **Play (▶️)** en la parte superior del editor de Unity.
4. ¡El juego se inicializará automáticamente con el ring, los dos luchadores, el árbitro virtual y la interfaz!
