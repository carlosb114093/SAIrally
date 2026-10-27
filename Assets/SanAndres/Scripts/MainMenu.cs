using System;
using System.Collections.Generic;
using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Menú principal, de pausa, de opciones y de controles. Todo procedural (IMGUI),
    /// sin prefabs ni Canvas. Se maneja con mouse, teclado o control.
    /// En el menú principal la cámara gira lentamente alrededor de la mulita en la salida.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        enum Pantalla { Oculto, Principal, Pausa, Opciones, Controles }

        public static MainMenu Instance { get; private set; }

        static bool abierto;
        static int cuadroCierre = -1;
        /// <summary>Verdadero si el menú está usando la entrada (RaceManager ignora sus teclas).</summary>
        public static bool Blocking => abierto || Time.frameCount == cuadroCierre;

        const string KeyVueltas = "SanAndres_vueltas";
        const string KeyCamara = "SanAndres_camara";
        const string KeyAyuda = "SanAndres_ayuda";

        class Item
        {
            public string texto;
            public Func<string> valor;
            public Func<float> barra;
            public Action aceptar;
            public Action<int> cambiar;
        }

        RaceManager race;
        CameraFollow follow;
        Camera cam;
        int vueltas;

        Pantalla pantalla = Pantalla.Oculto;
        Pantalla origen = Pantalla.Principal;   // a dónde vuelven Opciones / Controles
        readonly List<Item> items = new List<Item>();
        int sel, cuadroApertura = -1;
        float aparicion, tiempo, orbita;

        AudioSource sfx;
        AudioClip clipMover, clipAceptar;

        Texture2D texRedondo, texDegradado;
        GUIStyle stPanel, stTitulo, stSub, stEncabezado, stItem, stValor, stTexto, stTextoC, stPie;
        float escalaEstilos = -1f;

        static readonly Color Amarillo = new Color(1f, 0.82f, 0.16f);
        static readonly Color Turquesa = new Color(0.22f, 0.85f, 0.88f);
        static readonly Color Oscuro = new Color(0.06f, 0.1f, 0.14f);
        static readonly Color FondoItem = new Color(0.02f, 0.2f, 0.28f, 0.78f);
        // los siete colores del mar de San Andrés, de la orilla al mar profundo
        static readonly Color[] SieteColores =
        {
            new Color(0.93f, 0.98f, 0.95f), new Color(0.62f, 0.95f, 0.88f), new Color(0.31f, 0.88f, 0.84f),
            new Color(0.12f, 0.74f, 0.8f), new Color(0.07f, 0.55f, 0.73f), new Color(0.06f, 0.38f, 0.62f),
            new Color(0.05f, 0.2f, 0.45f)
        };

        // ------------------------------------------------------------ ciclo de vida

        public void Init(RaceManager r, CameraFollow f, int vueltasPorDefecto)
        {
            Instance = this;
            race = r;
            follow = f;
            cam = f != null ? f.GetComponent<Camera>() : Camera.main;
            vueltas = Mathf.Clamp(PlayerPrefs.GetInt(KeyVueltas, vueltasPorDefecto), 1, 5);
            if (follow != null) follow.mode = Mathf.Clamp(PlayerPrefs.GetInt(KeyCamara, 0), 0, 2);
            race.ShowHelp = PlayerPrefs.GetInt(KeyAyuda, 1) == 1;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            sfx.ignoreListenerPause = true;
            clipMover = Tono("menu_mover", new[] { 1046.5f }, 0.09f);
            clipAceptar = Tono("menu_aceptar", new[] { 784f, 1174.7f }, 0.2f);

            CrearTexturas();
            if (race.Car != null) orbita = race.Car.transform.eulerAngles.y + 150f;
            AbrirPrincipal();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            abierto = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            EngineAudio.Silenced = false;
            Cursor.visible = true;
        }

        // ------------------------------------------------------------ acciones

        void AbrirPrincipal()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            race.ParkAtStart();
            EngineAudio.Silenced = true;
            if (follow != null) follow.enabled = false;
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.Atenuar(false);
                MusicPlayer.Instance.Reproducir(MusicPlayer.Pista.Menu);
            }
            aparicion = 0f;
            Ir(Pantalla.Principal);
        }

        void Jugar()
        {
            PlayerPrefs.SetInt(KeyVueltas, vueltas);
            PlayerPrefs.Save();
            Cerrar();
            EngineAudio.Silenced = false;
            if (follow != null) follow.enabled = true;
            if (cam != null) cam.fieldOfView = 60f;
            race.StartRace(vueltas);
        }

        void Pausar()
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;   // el motor se pausa, la música no
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Atenuar(true);
            aparicion = 0f;
            Ir(Pantalla.Pausa);
        }

        void Continuar()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Atenuar(false);
            Cerrar();
        }

        void Salir()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Ir(Pantalla p, int seleccion = 0)
        {
            pantalla = p;
            abierto = p != Pantalla.Oculto;
            cuadroApertura = Time.frameCount;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ConstruirItems();
            sel = Mathf.Clamp(seleccion, 0, Mathf.Max(0, items.Count - 1));
        }

        void Cerrar()
        {
            pantalla = Pantalla.Oculto;
            abierto = false;
            cuadroCierre = Time.frameCount;
            items.Clear();
            Cursor.visible = false;
        }

        void Atras()
        {
            switch (pantalla)
            {
                case Pantalla.Pausa: Sonar(clipAceptar); Continuar(); break;
                case Pantalla.Opciones: Sonar(clipMover); PlayerPrefs.Save(); Ir(origen, 2); break;
                case Pantalla.Controles: Sonar(clipMover); Ir(origen, 3); break;
            }
        }

        void ConstruirItems()
        {
            items.Clear();
            switch (pantalla)
            {
                case Pantalla.Principal:
                    items.Add(new Item { texto = "Jugar", aceptar = Jugar });
                    items.Add(new Item
                    {
                        texto = "Vueltas",
                        valor = () => vueltas.ToString(),
                        cambiar = d => { vueltas = Mathf.Clamp(vueltas + d, 1, 5); PlayerPrefs.SetInt(KeyVueltas, vueltas); },
                        aceptar = () => { vueltas = vueltas % 5 + 1; PlayerPrefs.SetInt(KeyVueltas, vueltas); }
                    });
                    items.Add(new Item { texto = "Opciones", aceptar = () => { origen = Pantalla.Principal; Ir(Pantalla.Opciones); } });
                    items.Add(new Item { texto = "Controles", aceptar = () => { origen = Pantalla.Principal; Ir(Pantalla.Controles); } });
                    items.Add(new Item { texto = "Salir", aceptar = Salir });
                    break;

                case Pantalla.Pausa:
                    items.Add(new Item { texto = "Continuar", aceptar = Continuar });
                    items.Add(new Item { texto = "Reiniciar carrera", aceptar = () => { Continuar(); race.Restart(); } });
                    items.Add(new Item { texto = "Opciones", aceptar = () => { origen = Pantalla.Pausa; Ir(Pantalla.Opciones); } });
                    items.Add(new Item { texto = "Controles", aceptar = () => { origen = Pantalla.Pausa; Ir(Pantalla.Controles); } });
                    items.Add(new Item { texto = "Menú principal", aceptar = AbrirPrincipal });
                    items.Add(new Item { texto = "Salir del juego", aceptar = Salir });
                    break;

                case Pantalla.Opciones:
                    items.Add(new Item
                    {
                        texto = "Música",
                        barra = () => MusicPlayer.Volumen,
                        cambiar = d => MusicPlayer.Volumen = Mathf.Round(MusicPlayer.Volumen * 10f + d) / 10f
                    });
                    items.Add(new Item
                    {
                        texto = "Motor",
                        barra = () => EngineAudio.Volume,
                        cambiar = d => EngineAudio.Volume = Mathf.Round(EngineAudio.Volume * 10f + d) / 10f
                    });
                    items.Add(new Item
                    {
                        texto = "Cámara",
                        valor = () => follow != null ? follow.ModeName : "-",
                        cambiar = d =>
                        {
                            if (follow == null) return;
                            follow.mode = (follow.mode + d + 3) % 3;
                            if (race.Active) follow.Snap();
                            PlayerPrefs.SetInt(KeyCamara, follow.mode);
                        },
                        aceptar = () => items[sel].cambiar(1)
                    });
                    items.Add(new Item
                    {
                        texto = "Ayuda en pantalla",
                        valor = () => race.ShowHelp ? "Sí" : "No",
                        cambiar = d => { race.ShowHelp = !race.ShowHelp; PlayerPrefs.SetInt(KeyAyuda, race.ShowHelp ? 1 : 0); },
                        aceptar = () => items[sel].cambiar(1)
                    });
                    items.Add(new Item
                    {
                        texto = "Pantalla completa",
                        valor = () => Screen.fullScreen ? "Sí" : "No",
                        cambiar = d => Screen.fullScreen = !Screen.fullScreen,
                        aceptar = () => Screen.fullScreen = !Screen.fullScreen
                    });
                    items.Add(new Item { texto = "Volver", aceptar = () => { PlayerPrefs.Save(); Ir(origen, 2); } });
                    break;

                case Pantalla.Controles:
                    items.Add(new Item { texto = "Volver", aceptar = () => Ir(origen, 3) });
                    break;
            }
        }

        // ------------------------------------------------------------ entrada

        void Update()
        {
            tiempo += Time.unscaledDeltaTime;
            aparicion = Mathf.MoveTowards(aparicion, 1f, Time.unscaledDeltaTime * 2.5f);

            if (pantalla == Pantalla.Oculto)
            {
                if (race != null && race.Active && GameInput.PausePressed()) { Sonar(clipMover); Pausar(); }
                return;
            }
            if (Time.frameCount == cuadroApertura || items.Count == 0) return;

            var nav = GameInput.MenuNav();
            if (nav.y != 0)
            {
                sel = (sel - nav.y + items.Count) % items.Count;
                Sonar(clipMover);
            }
            if (nav.x != 0 && items[sel].cambiar != null)
            {
                items[sel].cambiar(nav.x);
                Sonar(clipMover);
            }
            if (GameInput.MenuConfirm()) Activar(sel);
            else if (GameInput.MenuBack()) Atras();
        }

        void Activar(int i)
        {
            if (i < 0 || i >= items.Count) return;
            var it = items[i];
            Sonar(clipAceptar);
            if (it.aceptar != null) it.aceptar();
            else if (it.cambiar != null) it.cambiar(1);
        }

        // ------------------------------------------------------------ cámara del menú

        void LateUpdate()
        {
            if (race == null || race.Active || cam == null || race.Car == null) return;
            orbita += Time.unscaledDeltaTime * 7f;
            Vector3 c = race.Car.transform.position + Vector3.up * 1.0f;
            Vector3 pos = c + Quaternion.Euler(0f, orbita, 0f) * new Vector3(0f, 0f, -12.5f) + Vector3.up * 3.6f;
            if (Physics.Linecast(c, pos, out RaycastHit hit, ~(1 << 2), QueryTriggerInteraction.Ignore))
                pos = hit.point + hit.normal * 0.4f;
            var t = cam.transform;
            t.position = pos;
            // el carro queda a la derecha de la pantalla y el menú a la izquierda
            Vector3 derecha = Quaternion.Euler(0f, orbita, 0f) * Vector3.right;
            t.rotation = Quaternion.LookRotation(c - derecha * 4.2f + Vector3.up * 0.8f - pos);
            cam.fieldOfView = 50f;
        }

        // ------------------------------------------------------------ dibujo

        void OnGUI()
        {
            if (pantalla == Pantalla.Oculto || items.Count == 0) return;
            GUI.depth = -100;
            float s = Screen.height / 1080f;
            float W = Screen.width, H = Screen.height;
            EstilosSiHaceFalta(s);

            var colorPrevio = GUI.color;
            float a = Mathf.SmoothStep(0f, 1f, aparicion);
            bool enCarrera = race.Active;   // pausa (o sus submenús) sobre la carrera

            // fondo
            if (enCarrera)
                Caja(new Rect(0, 0, W, H), new Color(0.0f, 0.06f, 0.1f, 0.6f * a));
            else if (Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect(0, 0, Mathf.Max(W * 0.62f, 760 * s), H), texDegradado);
                GUI.color = colorPrevio;
            }

            GUI.color = new Color(1f, 1f, 1f, a);

            float colX, colW = 540 * s, inicioY;
            if (enCarrera) colX = W * 0.5f - colW * 0.5f;
            else colX = 110 * s;

            // encabezado
            switch (pantalla)
            {
                case Pantalla.Principal:
                    DibujarTitulo(colX, 110 * s, s);
                    inicioY = 440 * s;
                    break;
                case Pantalla.Pausa:
                    Texto(new Rect(colX, 150 * s, colW, 110 * s), "PAUSA", stEncabezado, Color.white, true, TextAnchor.MiddleCenter);
                    Franja(new Rect(colX + colW * 0.5f - 160 * s, 262 * s, 320 * s, 8 * s));
                    inicioY = 310 * s;
                    break;
                case Pantalla.Opciones:
                    Texto(new Rect(colX, 150 * s, colW, 110 * s), "OPCIONES", stEncabezado, Color.white, true, enCarrera ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
                    Franja(new Rect(enCarrera ? colX + colW * 0.5f - 160 * s : colX, 262 * s, 320 * s, 8 * s));
                    inicioY = 310 * s;
                    break;
                default: // Controles
                    Texto(new Rect(colX, 110 * s, colW, 110 * s), "CONTROLES", stEncabezado, Color.white, true, enCarrera ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
                    Franja(new Rect(enCarrera ? colX + colW * 0.5f - 160 * s : colX, 222 * s, 320 * s, 8 * s));
                    float tablaW = 900 * s;
                    float tablaX = enCarrera ? W * 0.5f - tablaW * 0.5f : colX;
                    DibujarControles(new Rect(tablaX, 260 * s, tablaW, 560 * s), s);
                    inicioY = 850 * s;
                    break;
            }

            // opciones
            float h = 68 * s, gap = 12 * s;
            for (int i = 0; i < items.Count; i++)
                DibujarItem(i, new Rect(colX, inicioY + i * (h + gap), colW, h), s);

            // récords en el menú principal
            if (pantalla == Pantalla.Principal)
            {
                float y = inicioY + items.Count * (h + gap) + 14 * s;
                Texto(new Rect(colX + 6 * s, y, 900 * s, 32 * s),
                    $"Récord {vueltas} {(vueltas == 1 ? "vuelta" : "vueltas")}:  {Fmt(race.BestRaceFor(vueltas))}     Mejor vuelta:  {Fmt(race.BestLap)}",
                    stTexto, new Color(1f, 1f, 1f, 0.9f), true);
            }

            // pie con la ayuda de navegación
            string pie = pantalla == Pantalla.Principal
                ? "↑ ↓  elegir      ← →  cambiar      Enter  aceptar"
                : "↑ ↓  elegir      ← →  cambiar      Enter  aceptar      Esc  volver";
            Texto(new Rect(enCarrera ? 0 : colX, H - 64 * s, enCarrera ? W : 1200 * s, 40 * s), pie,
                enCarrera ? stTextoC : stPie, new Color(1f, 1f, 1f, 0.75f), true);
            if (!enCarrera)
                Texto(new Rect(W - 620 * s, H - 64 * s, 590 * s, 40 * s), "Isla de San Andrés · Caribe colombiano",
                    stPie, new Color(1f, 1f, 1f, 0.6f), true, TextAnchor.MiddleRight);

            GUI.color = colorPrevio;
        }

        void DibujarTitulo(float x, float y, float s)
        {
            Texto(new Rect(x - 4 * s, y, 1200 * s, 150 * s), "SAN ANDRÉS", stTitulo, Color.white, true);
            Texto(new Rect(x, y + 140 * s, 1000 * s, 50 * s), "RALLY DE LA CIRCUNVALAR", stSub, Amarillo, true);
            Franja(new Rect(x, y + 204 * s, 560 * s, 10 * s));
            Texto(new Rect(x, y + 226 * s, 1000 * s, 36 * s), "Una vuelta a la isla por el mar de siete colores", stTexto,
                new Color(1f, 1f, 1f, 0.85f), true);
        }

        void Franja(Rect r)
        {
            float w = r.width / SieteColores.Length;
            for (int i = 0; i < SieteColores.Length; i++)
            {
                var c = SieteColores[i];
                c.a = GUI.color.a;
                Caja(new Rect(r.x + i * w, r.y, w + 0.5f, r.height), c);
            }
        }

        void DibujarItem(int i, Rect r, float s)
        {
            var it = items[i];
            var ev = Event.current;

            // mouse: pasar por encima selecciona, clic activa
            bool dentro = r.Contains(ev.mousePosition);
            // zona del valor: mitad izquierda baja, mitad derecha sube
            Rect flechaIzq = new Rect(r.xMax - 290 * s, r.y, 145 * s, r.height);
            Rect flechaDer = new Rect(r.xMax - 145 * s, r.y, 145 * s, r.height);
            if (dentro && (ev.type == EventType.MouseMove || ev.type == EventType.MouseDrag) && sel != i)
            {
                sel = i;
                Sonar(clipMover);
            }
            if (dentro && ev.type == EventType.MouseDown && ev.button == 0)
            {
                sel = i;
                if (it.cambiar != null && flechaIzq.Contains(ev.mousePosition)) { it.cambiar(-1); Sonar(clipMover); }
                else if (it.cambiar != null && flechaDer.Contains(ev.mousePosition)) { it.cambiar(1); Sonar(clipMover); }
                else if (it.cambiar != null && it.aceptar == null) { it.cambiar(1); Sonar(clipMover); }
                else Activar(i);
                ev.Use();
                return;
            }

            bool activo = i == sel;
            float alfa = GUI.color.a;
            if (activo)
            {
                float pulso = 0.92f + 0.08f * Mathf.Sin(tiempo * 5f);
                r.x += 14 * s;
                Panel(r, new Color(Amarillo.r * pulso, Amarillo.g * pulso, Amarillo.b * pulso, 0.97f * alfa));
                Caja(new Rect(r.x - 14 * s, r.y + 10 * s, 6 * s, r.height - 20 * s), new Color(Turquesa.r, Turquesa.g, Turquesa.b, alfa));
            }
            else Panel(r, new Color(FondoItem.r, FondoItem.g, FondoItem.b, FondoItem.a * alfa));

            Color colTexto = activo ? Oscuro : Color.white;
            colTexto.a = alfa;
            Texto(new Rect(r.x + 30 * s, r.y, r.width - 60 * s, r.height), it.texto.ToUpperInvariant(), stItem, colTexto, !activo);

            if (it.barra != null)
            {
                float v = Mathf.Clamp01(it.barra());
                int llenos = Mathf.RoundToInt(v * 10f);
                float segW = 13 * s, segGap = 4 * s, bx = r.xMax - 70 * s - 10 * (segW + segGap), by = r.y + r.height * 0.5f - 11 * s;
                for (int k = 0; k < 10; k++)
                {
                    Color c = k < llenos ? (activo ? Oscuro : Turquesa) : (activo ? new Color(0f, 0f, 0f, 0.18f) : new Color(1f, 1f, 1f, 0.18f));
                    c.a *= alfa;
                    Caja(new Rect(bx + k * (segW + segGap), by, segW, 22 * s), c);
                }
                Flechas(r, activo, colTexto, s, bx - 22 * s, r.xMax - 40 * s);
            }
            else if (it.valor != null)
            {
                var vr = new Rect(r.xMax - 190 * s, r.y, 130 * s, r.height);
                Texto(vr, it.valor(), stValor, colTexto, !activo);
                if (it.cambiar != null) Flechas(r, activo, colTexto, s, r.xMax - 215 * s, r.xMax - 35 * s);
            }
        }

        void Flechas(Rect r, bool activo, Color c, float s, float centroIzq, float centroDer)
        {
            if (!activo) return;
            Texto(new Rect(centroIzq - 20 * s, r.y, 40 * s, r.height), "◄", stValor, c, false);
            Texto(new Rect(centroDer - 20 * s, r.y, 40 * s, r.height), "►", stValor, c, false);
        }

        static readonly string[,] Controles =
        {
            { "Acelerar / frenar", "W  S  /  ↑  ↓", "Gatillos RT / LT" },
            { "Girar", "A  D  /  ←  →", "Stick izquierdo" },
            { "Freno de mano", "Espacio", "A" },
            { "Turbo", "Shift", "RB" },
            { "Reaparecer en la vía", "R", "Y" },
            { "Reiniciar carrera", "T  /  Enter", "Desde la pausa" },
            { "Cambiar cámara", "C", "Select" },
            { "Pausa", "Esc  /  P", "Start" },
            { "Silenciar motor", "M", "—" },
            { "Mostrar / ocultar ayuda", "H  /  F1", "—" },
        };

        void DibujarControles(Rect r, float s)
        {
            Panel(r, new Color(FondoItem.r, FondoItem.g, FondoItem.b, 0.82f * GUI.color.a));
            float fila = 46 * s, y = r.y + 22 * s;
            float c0 = r.x + 34 * s, c1 = r.x + r.width * 0.45f, c2 = r.x + r.width * 0.71f;
            Texto(new Rect(c0, y, 400 * s, fila), "ACCIÓN", stTexto, Amarillo, true);
            Texto(new Rect(c1, y, 300 * s, fila), "TECLADO", stTexto, Amarillo, true);
            Texto(new Rect(c2, y, 300 * s, fila), "CONTROL", stTexto, Amarillo, true);
            y += fila + 4 * s;
            Caja(new Rect(r.x + 24 * s, y - 4 * s, r.width - 48 * s, 2 * s), new Color(1f, 1f, 1f, 0.25f * GUI.color.a));
            for (int i = 0; i < Controles.GetLength(0); i++)
            {
                Texto(new Rect(c0, y, 400 * s, fila), Controles[i, 0], stTexto, Color.white, true);
                Texto(new Rect(c1, y, 300 * s, fila), Controles[i, 1], stTexto, Turquesa, true);
                Texto(new Rect(c2, y, 300 * s, fila), Controles[i, 2], stTexto, Turquesa, true);
                y += fila;
            }
        }

        // ------------------------------------------------------------ utilidades de dibujo

        void Texto(Rect r, string t, GUIStyle st, Color c, bool sombra, TextAnchor? alinear = null)
        {
            var prevAnchor = st.alignment;
            if (alinear.HasValue) st.alignment = alinear.Value;
            var prevGui = GUI.color;
            float a = GUI.color.a;
            GUI.color = Color.white;
            if (sombra)
            {
                st.normal.textColor = new Color(0f, 0f, 0f, 0.6f * a * c.a);
                GUI.Label(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), t, st);
            }
            st.normal.textColor = new Color(c.r, c.g, c.b, c.a * a);
            GUI.Label(r, t, st);
            GUI.color = prevGui;
            st.alignment = prevAnchor;
        }

        static void Caja(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void Panel(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            var prev = GUI.color;
            var prevBg = GUI.backgroundColor;
            GUI.color = c;
            GUI.backgroundColor = Color.white;
            stPanel.Draw(r, false, false, false, false);
            GUI.color = prev;
            GUI.backgroundColor = prevBg;
        }

        void EstilosSiHaceFalta(float s)
        {
            if (Mathf.Approximately(s, escalaEstilos) && stItem != null) return;
            escalaEstilos = s;
            stPanel = new GUIStyle { border = new RectOffset(18, 18, 18, 18) };
            stPanel.normal.background = texRedondo;
            stTitulo = Estilo(128, s, TextAnchor.UpperLeft);
            stSub = Estilo(40, s, TextAnchor.UpperLeft);
            stEncabezado = Estilo(84, s, TextAnchor.MiddleCenter);
            stItem = Estilo(32, s, TextAnchor.MiddleLeft);
            stValor = Estilo(32, s, TextAnchor.MiddleCenter);
            stTexto = Estilo(26, s, TextAnchor.MiddleLeft);
            stTextoC = Estilo(24, s, TextAnchor.MiddleCenter);
            stPie = Estilo(24, s, TextAnchor.MiddleLeft);
        }

        static GUIStyle Estilo(int tam, float s, TextAnchor a)
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(10, Mathf.RoundToInt(tam * s)),
                fontStyle = FontStyle.Bold,
                alignment = a,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
            st.normal.textColor = Color.white;
            return st;
        }

        void CrearTexturas()
        {
            // rectángulo redondeado blanco (9-slice)
            const int n = 64; const float rad = 16f;
            texRedondo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(rad - (x + 0.5f), (x + 0.5f) - (n - rad)));
                    float dy = Mathf.Max(0f, Mathf.Max(rad - (y + 0.5f), (y + 0.5f) - (n - rad)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(rad - d + 0.5f));
                }
            texRedondo.SetPixels(px);
            texRedondo.Apply();

            // degradado lateral para que el menú se lea sobre la isla
            const int g = 256;
            texDegradado = new Texture2D(g, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var gp = new Color[g];
            for (int x = 0; x < g; x++)
            {
                float t = x / (g - 1f);
                float alfa = 0.88f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, t)));
                gp[x] = new Color(0.01f, 0.13f, 0.2f, alfa);
            }
            texDegradado.SetPixels(gp);
            texDegradado.Apply();
            escalaEstilos = -1f;
        }

        // ------------------------------------------------------------ sonidos de interfaz

        static AudioClip Tono(string nombre, float[] notas, float dur)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * dur);
            var data = new float[len];
            float tramo = dur / notas.Length;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)sr;
                int k = Mathf.Min(notas.Length - 1, (int)(t / tramo));
                float tl = t - k * tramo;
                float f = notas[k];
                // timbre de steel pan: fundamental + octava + duodécima
                float v = Mathf.Sin(2f * Mathf.PI * f * tl) + 0.45f * Mathf.Sin(4f * Mathf.PI * f * tl) + 0.2f * Mathf.Sin(6f * Mathf.PI * f * tl);
                float env = Mathf.Min(1f, tl / 0.003f) * Mathf.Exp(-tl * 22f);
                float fin = Mathf.Clamp01((dur - t) / 0.01f);
                data[i] = 0.22f * v * env * fin;
            }
            var clip = AudioClip.Create(nombre, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Sonar(AudioClip c)
        {
            if (sfx == null || c == null) return;
            sfx.PlayOneShot(c, 0.35f + 0.65f * MusicPlayer.Volumen);
        }

        static string Fmt(float t)
        {
            if (t <= 0f) return "--:--.--";
            int m = (int)(t / 60f);
            float sec = t - m * 60f;
            return $"{m:00}:{sec:00.00}";
        }
    }
}
