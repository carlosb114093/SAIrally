# San Andrés — Rally de la Circunvalar

Prototipo de juego de carreras 3D en Unity ambientado en la isla de San Andrés (Caribe colombiano). El jugador conduce una "mulita" (el carrito de golf típico de la isla) por la vía Circunvalar, pasando por puntos de control y completando vueltas contra el reloj.

- **Autor:** Carlos Darío Botina Mora
- **Programa:** Desarrollo de Videojuegos y Entornos Interactivos — SENA, ficha 3466174
- **Motor:** Unity 6000.0.40f1 · Universal Render Pipeline (URP) · Input System

## Cómo abrirlo

1. En Unity Hub: **Add → Add project from disk** y seleccionar esta carpeta.
2. Abrir con Unity **6000.0.40f1**.
3. Abrir la escena `Assets/Scenes/SanAndres.unity` y presionar **Play**.

La isla, la vegetación, los sitios, el vehículo, el HUD y los menús se generan por código al iniciar la escena (`SanAndresGame.cs`).

## Contenido del prototipo

- Menú principal, pausa, opciones (música, motor, cámara, ayuda, pantalla completa) y pantalla de controles.
- Carrera por vueltas con cuenta regresiva, puntos de control, récords guardados y minimapa.
- Sitios de la isla: North End / Spratt Bight, aeropuerto, Johnny Cay, Rocky Cay, San Luis, Haynes Cay y El Acuario, La Loma, Hoyo Soplador, West View, Cueva de Morgan y El Cove.
- Música de fondo (menú y carrera) y sonido de motor procedural.

## Controles

| Acción | Teclado | Control |
|---|---|---|
| Acelerar / frenar | W S / ↑ ↓ | Gatillos RT / LT |
| Girar | A D / ← → | Stick izquierdo |
| Freno de mano | Espacio | A |
| Turbo | Shift | RB |
| Reaparecer en la vía | R | Y |
| Reiniciar carrera | T / Enter | Desde la pausa |
| Cambiar cámara | C | Select |
| Pausa | Esc / P | Start |
| Silenciar motor | M | — |
| Mostrar / ocultar ayuda | H / F1 | — |

## Estructura

| Ruta | Contenido |
|---|---|
| `Assets/Scenes/SanAndres.unity` | Escena principal |
| `Assets/SanAndres/Scripts/` | Código del juego (generación de la isla, vehículo, carrera, menús, audio) |
| `Assets/SanAndres/Resources/Musica/` | Pistas de música del menú y de la carrera |
| `Assets/Settings/` | Configuración de URP |
