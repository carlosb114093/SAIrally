using System.Collections.Generic;
using UnityEngine;

namespace SanAndres
{
    /// <summary>
    /// Carrera por la vía Circunvalar: cuenta regresiva, puntos de control, vueltas, récords
    /// (guardados con PlayerPrefs), reaparición y HUD con minimapa de la isla.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        public int totalLaps = 2;
        public int checkpointCount = 14;

        public ArcadeCar Car { get; private set; }
        /// <summary>Falso mientras se está en el menú principal (sin HUD ni controles).</summary>
        public bool Active { get; private set; }
        public bool ShowHelp { get => showHelp; set => showHelp = value; }
        public float BestRaceFor(int laps) => PlayerPrefs.GetFloat($"SanAndres_bestRace_{laps}", 0f);
        public float BestLap => bestLap;

        enum State { Countdown, Racing, Finished }

        IslandData island;
        CameraFollow cam;
        readonly List<Checkpoint> cps = new List<Checkpoint>();
        State state;
        float countdown, raceTime, lapStart, lastLap, bestLap, bestRace, flashUntil, upsideDownTime;
        int nextCp, lap, startIndex;
        string flashMsg = "";
        bool showHelp = true, newRecord;
        Vector3 startPos, respawnPos;
        Quaternion startRot, respawnRot;

        Texture2D whiteTex, arrowTex;
        GUIStyle big, mid, small, center, right;
        float styleScale = -1f;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Init(IslandData d, ArcadeCar car, CameraFollow camera, int startIdx, int laps, bool startNow = true)
        {
            island = d; Car = car; cam = camera; startIndex = startIdx; totalLaps = Mathf.Max(1, laps);
            bestLap = PlayerPrefs.GetFloat("SanAndres_bestLap", 0f);
            bestRace = PlayerPrefs.GetFloat($"SanAndres_bestRace_{totalLaps}", 0f);

            Vector3 sp = d.RoadPoint(startIdx - 4);
            startPos = sp + Vector3.up * 1.0f;
            startRot = Quaternion.LookRotation(d.RoadTangent(startIdx - 4), Vector3.up);
            BuildCheckpoints();
            whiteTex = Texture2D.whiteTexture;
            arrowTex = BuildArrow();
            if (startNow) StartRace(totalLaps);
            else ParkAtStart();
        }

        /// <summary>Empieza una carrera nueva con la cantidad de vueltas elegida en el menú.</summary>
        public void StartRace(int laps)
        {
            totalLaps = Mathf.Max(1, laps);
            bestRace = BestRaceFor(totalLaps);
            Active = true;
            Restart();
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Reproducir(MusicPlayer.Pista.Carrera);
        }

        /// <summary>Deja la mulita quieta en la salida, sin HUD (fondo del menú principal).</summary>
        public void ParkAtStart()
        {
            Active = false;
            state = State.Countdown;
            raceTime = 0f; lap = 0; nextCp = 1; flashUntil = 0f;
            Car.inputEnabled = false;
            Car.ResetTo(startPos, startRot);
            UpdateHighlights();
        }

        void BuildCheckpoints()
        {
            var parent = new GameObject("Puntos de control").transform;
            int n = island.road.Count;
            var checker = MatLib.Textured(MatLib.Checker(16, 2, 8), 0.2f);
            var post = MatLib.Solid(Color.white, 0.4f);
            var banner = MatLib.Solid(new Color(1f, 0.85f, 0.1f), 0.4f, 0f, true);
            var post2 = MatLib.Solid(new Color(0.1f, 0.75f, 0.85f), 0.4f);
            for (int k = 0; k < checkpointCount; k++)
            {
                int idx = (startIndex + k * n / checkpointCount) % n;
                Vector3 p = island.road[idx];
                var go = new GameObject(k == 0 ? "Salida / Meta" : $"Punto de control {k}");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(island.roadTangent[idx], Vector3.up));
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0f, 5f, 0f);
                box.size = new Vector3(island.roadWidth + 26f, 14f, 4f);
                var cp = go.AddComponent<Checkpoint>();
                cp.index = k; cp.manager = this;

                var vis = new GameObject("Arco");
                vis.transform.SetParent(go.transform, false);
                float hw = island.HalfRoad + 2f;
                var pm = k == 0 ? post : post2;
                Props.Part(vis.transform, PrimitiveType.Cylinder, new Vector3(-hw, 3.6f, 0f), Vector3.zero, new Vector3(0.4f, 3.6f, 0.4f), pm);
                Props.Part(vis.transform, PrimitiveType.Cylinder, new Vector3(hw, 3.6f, 0f), Vector3.zero, new Vector3(0.4f, 3.6f, 0.4f), pm);
                Props.Part(vis.transform, PrimitiveType.Cube, new Vector3(0f, 7.4f, 0f), Vector3.zero, new Vector3(hw * 2f + 0.6f, 1.3f, 0.3f), k == 0 ? checker : banner);
                cp.visual = vis;
                cps.Add(cp);
            }
        }

        Texture2D BuildArrow()
        {
            const int s = 32;
            var t = MatLib.NewTexture(s, s, false);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float halfW = (s - 1 - y) / (float)(s - 1) * (s * 0.5f) + 0.5f;
                    bool inside = Mathf.Abs(x - (s - 1) * 0.5f) <= halfW && y > 2;
                    px[y * s + x] = inside ? Color.white : new Color(1f, 1f, 1f, 0f);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        public void Restart()
        {
            state = State.Countdown;
            countdown = 3.0f;
            raceTime = 0f; lapStart = 0f; lastLap = 0f; lap = 0; nextCp = 1; newRecord = false;
            respawnPos = startPos; respawnRot = startRot;
            Car.inputEnabled = false;
            Car.ResetTo(startPos, startRot);
            if (cam != null) cam.Snap();
            UpdateHighlights();
        }

        public void Respawn(string msg)
        {
            Car.ResetTo(respawnPos, respawnRot);
            if (cam != null) cam.Snap();
            Flash(msg);
        }

        public void Flash(string msg, float duration = 2.5f)
        {
            flashMsg = msg;
            flashUntil = Time.time + duration;
        }

        public void OnCheckpoint(Checkpoint cp)
        {
            if (state != State.Racing || cp.index != nextCp) return;
            respawnPos = cp.transform.position + Vector3.up * 1.0f;
            respawnRot = cp.transform.rotation;
            if (cp.index == 0)
            {
                float lt = raceTime - lapStart;
                lastLap = lt;
                lapStart = raceTime;
                lap++;
                if (bestLap <= 0f || lt < bestLap)
                {
                    bestLap = lt;
                    PlayerPrefs.SetFloat("SanAndres_bestLap", bestLap);
                    Flash($"¡Mejor vuelta!  {Fmt(lt)}");
                }
                else Flash($"Vuelta {Fmt(lt)}");
                if (lap >= totalLaps) Finish();
                else nextCp = 1;
            }
            else
            {
                nextCp = (cp.index + 1) % cps.Count;
            }
            UpdateHighlights();
        }

        void Finish()
        {
            state = State.Finished;
            Car.inputEnabled = false;
            if (bestRace <= 0f || raceTime < bestRace)
            {
                bestRace = raceTime;
                newRecord = true;
                PlayerPrefs.SetFloat($"SanAndres_bestRace_{totalLaps}", bestRace);
            }
            PlayerPrefs.Save();
        }

        void UpdateHighlights()
        {
            for (int i = 0; i < cps.Count; i++)
                cps[i].SetHighlight(i == 0 || (state != State.Finished && i == nextCp));
        }

        void Update()
        {
            if (Car == null || !Active || MainMenu.Blocking) return;

            if (GameInput.RestartPressed()) Restart();
            if (GameInput.RespawnPressed() && state != State.Finished) Respawn("Reaparición");
            if (GameInput.CameraPressed() && cam != null) { cam.Cycle(); Flash("Cámara: " + cam.ModeName, 1.2f); }
            if (GameInput.HelpPressed()) showHelp = !showHelp;
            if (GameInput.MutePressed()) { EngineAudio.Muted = !EngineAudio.Muted; Flash(EngineAudio.Muted ? "Motor silenciado" : "Motor con sonido", 1.2f); }

            if (state == State.Countdown)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 0f)
                {
                    state = State.Racing;
                    Car.inputEnabled = true;
                    Flash("¡YA!", 1.2f);
                }
            }
            else if (state == State.Racing)
            {
                raceTime += Time.deltaTime;
            }

            // ¿se cayó al mar?
            if (Car.transform.position.y < -0.3f && state != State.Finished)
                Respawn("¡Al agua! El mar de siete colores es para nadar, no para manejar");

            // ¿quedó volteado?
            bool upside = Vector3.Dot(Car.transform.up, Vector3.up) < 0.25f && Car.Body.linearVelocity.magnitude < 2f;
            upsideDownTime = upside ? upsideDownTime + Time.deltaTime : 0f;
            if (upsideDownTime > 2.5f) { upsideDownTime = 0f; Respawn("Te volteaste: de vuelta a la vía"); }
        }

        // ------------------------------------------------------------ HUD

        static string Fmt(float t)
        {
            if (t <= 0f) return "--:--.--";
            int m = (int)(t / 60f);
            float s = t - m * 60f;
            return $"{m:00}:{s:00.00}";
        }

        void EnsureStyles(float s)
        {
            if (Mathf.Approximately(s, styleScale) && big != null) return;
            styleScale = s;
            big = MakeStyle(110, s, TextAnchor.MiddleCenter);
            mid = MakeStyle(34, s, TextAnchor.UpperLeft);
            small = MakeStyle(22, s, TextAnchor.UpperLeft);
            center = MakeStyle(40, s, TextAnchor.MiddleCenter);
            right = MakeStyle(72, s, TextAnchor.LowerRight);
        }

        static GUIStyle MakeStyle(int size, float s, TextAnchor anchor)
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(10, Mathf.RoundToInt(size * s)),
                fontStyle = FontStyle.Bold,
                alignment = anchor,
                wordWrap = false
            };
            st.normal.textColor = Color.white;
            return st;
        }

        static void Label(Rect r, string text, GUIStyle st, Color? color = null)
        {
            var prev = st.normal.textColor;
            st.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, st);
            st.normal.textColor = color ?? prev;
            GUI.Label(r, text, st);
            st.normal.textColor = prev;
        }

        void Box(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, whiteTex);
            GUI.color = prev;
        }

        string ZoneName()
        {
            string best = "Vía Circunvalar";
            float bd = 170f * 170f;
            Vector3 p = Car.transform.position;
            foreach (var l in island.landmarks)
            {
                float dx = l.position.x - p.x, dz = l.position.z - p.z;
                float d = dx * dx + dz * dz;
                if (d < bd) { bd = d; best = l.name; }
            }
            return best;
        }

        void OnGUI()
        {
            if (Car == null || island == null || !Active) return;
            float s = Screen.height / 1080f;
            EnsureStyles(s);
            float W = Screen.width, H = Screen.height;

            // panel de tiempos
            Box(new Rect(16 * s, 16 * s, 470 * s, 210 * s), new Color(0f, 0f, 0f, 0.35f));
            var r = new Rect(30 * s, 22 * s, 460 * s, 44 * s);
            Label(r, $"VUELTA {Mathf.Min(lap + 1, totalLaps)}/{totalLaps}", mid); r.y += 44 * s;
            Label(r, $"TIEMPO  {(raceTime > 0f ? Fmt(raceTime) : "00:00.00")}", mid); r.y += 46 * s;
            Label(r, $"ÚLTIMA  {Fmt(lastLap)}     MEJOR  {Fmt(bestLap)}", small); r.y += 30 * s;
            Label(r, $"RÉCORD {totalLaps} VUELTAS  {Fmt(bestRace)}", small); r.y += 30 * s;
            if (state == State.Racing && cps.Count > 0)
            {
                float dist = Vector3.Distance(Car.transform.position, cps[nextCp].transform.position);
                string cpName = nextCp == 0 ? "META" : $"PUNTO {nextCp}/{cps.Count - 1}";
                Label(r, $"{cpName}  ·  {dist:0} m", small, new Color(1f, 0.88f, 0.3f));
            }

            // zona actual
            Label(new Rect(0f, 20 * s, W, 50 * s), ZoneName(), center);

            // velocímetro y turbo
            Label(new Rect(W - 420 * s, H - 175 * s, 330 * s, 100 * s), $"{Car.SpeedKmh:0}", right);
            Label(new Rect(W - 90 * s, H - 118 * s, 80 * s, 40 * s), "km/h", small);
            var bar = new Rect(W - 360 * s, H - 64 * s, 330 * s, 18 * s);
            Box(bar, new Color(0f, 0f, 0f, 0.5f));
            Box(new Rect(bar.x, bar.y, bar.width * Car.Turbo, bar.height),
                Car.TurboActive ? new Color(1f, 0.45f, 0.1f, 0.95f) : new Color(0.2f, 0.85f, 0.95f, 0.9f));
            Label(new Rect(bar.x, bar.y - 30 * s, 200 * s, 30 * s), "TURBO", small);

            DrawMinimap(s);

            // mensajes centrales
            if (state == State.Countdown)
                Label(new Rect(0f, H * 0.3f, W, 160 * s), Mathf.CeilToInt(countdown).ToString(), big, new Color(1f, 0.9f, 0.2f));
            if (Time.time < flashUntil && !string.IsNullOrEmpty(flashMsg))
                Label(new Rect(0f, H * 0.42f, W, 70 * s), flashMsg, center);

            if (state == State.Finished)
            {
                var panel = new Rect(W * 0.5f - 330 * s, H * 0.5f - 150 * s, 660 * s, 340 * s);
                Box(panel, new Color(0.02f, 0.18f, 0.3f, 0.85f));
                var pr = new Rect(panel.x, panel.y + 20 * s, panel.width, 60 * s);
                Label(pr, "¡LLEGASTE A LA META!", center, new Color(1f, 0.88f, 0.25f)); pr.y += 70 * s;
                Label(pr, $"Tiempo total  {Fmt(raceTime)}", center); pr.y += 55 * s;
                Label(pr, newRecord ? "¡Nuevo récord!" : $"Récord  {Fmt(bestRace)}", center); pr.y += 60 * s;
                Label(pr, "Presiona T o Enter para correr otra vez", center); pr.y += 50 * s;
                Label(pr, "Esc: menú", center, new Color(1f, 1f, 1f, 0.75f));
            }

            if (showHelp)
            {
                var hr = new Rect(16 * s, H - 210 * s, 520 * s, 200 * s);
                Box(hr, new Color(0f, 0f, 0f, 0.35f));
                Label(new Rect(hr.x + 14 * s, hr.y + 8 * s, hr.width, hr.height),
                    "W/S o flechas: acelerar / frenar\nA/D: girar   Espacio: freno de mano\nShift: turbo   R: reaparecer   C: cámara\nT / Enter: reiniciar   M: motor   Esc: pausa   H: ocultar",
                    small);
            }
        }

        Vector2 ToMap(Vector3 wp, Rect mr)
        {
            Vector2 uv = island.WorldToMap01(wp);
            return new Vector2(mr.x + uv.x * mr.width, mr.y + (1f - uv.y) * mr.height);
        }

        void DrawMinimap(float s)
        {
            if (island.minimap == null) return;
            float w = 170 * s, h = w * island.SizeZ / island.SizeX;
            var mr = new Rect(Screen.width - w - 20 * s, 20 * s, w, h);
            Box(new Rect(mr.x - 4 * s, mr.y - 4 * s, mr.width + 8 * s, mr.height + 8 * s), new Color(1f, 1f, 1f, 0.5f));
            GUI.DrawTexture(mr, island.minimap);

            if (state != State.Finished && cps.Count > 0)
            {
                Vector2 c = ToMap(cps[nextCp].transform.position, mr);
                Box(new Rect(c.x - 5 * s, c.y - 5 * s, 10 * s, 10 * s), new Color(1f, 0.85f, 0.1f));
            }
            Vector2 p = ToMap(Car.transform.position, mr);
            float yaw = Car.transform.eulerAngles.y;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(yaw, p);
            var prev = GUI.color;
            GUI.color = new Color(0.95f, 0.15f, 0.15f);
            GUI.DrawTexture(new Rect(p.x - 9 * s, p.y - 9 * s, 18 * s, 18 * s), arrowTex);
            GUI.color = prev;
            GUI.matrix = m;
        }
    }
}
