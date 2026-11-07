using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SanAndres.EditorTools
{
    /// <summary>
    /// Exporta el juego para navegador (WebGL) con un clic.
    /// Menú: San Andrés → Exportar para Web (WebGL).
    /// El resultado queda en la carpeta Builds/WebGL del proyecto (ignorada por git).
    /// </summary>
    public static class ExportarWeb
    {
        const string CarpetaSalida = "Builds/WebGL";
        const string CarpetaMateriales = "Assets/SanAndres/Resources/Materiales";
        const string LitURP = "Universal Render Pipeline/Lit";

        [MenuItem("San Andrés/Exportar para Web (WebGL)", priority = 1)]
        public static void Exportar() => Construir(false);

        [MenuItem("San Andrés/Exportar y probar en el navegador", priority = 2)]
        public static void ExportarYProbar() => Construir(true);

        [MenuItem("San Andrés/Abrir carpeta del build web", priority = 20)]
        public static void AbrirCarpeta()
        {
            var ruta = Path.GetFullPath(CarpetaSalida);
            if (Directory.Exists(ruta)) EditorUtility.RevealInFinder(Path.Combine(ruta, "index.html"));
            else EditorUtility.DisplayDialog("Build web", "Todavía no hay build. Usa primero \"Exportar para Web\".", "OK");
        }

        /// <summary>Para línea de comandos: Unity.exe -batchmode -quit -projectPath ... -executeMethod SanAndres.EditorTools.ExportarWeb.BuildBatch</summary>
        public static void BuildBatch()
        {
            bool ok = Construir(false, interactivo: false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Construir(bool probar, bool interactivo = true)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                const string msg = "No está instalado el módulo \"WebGL Build Support\" para esta versión de Unity.\n\n" +
                                   "Unity Hub → Installs → (6000.0.40f1) → ⚙ → Add modules → WebGL Build Support.";
                Debug.LogError("[San Andrés] " + msg);
                if (interactivo) EditorUtility.DisplayDialog("Falta WebGL", msg, "OK");
                return false;
            }

            AsegurarVariantesDeShader();
            ConfigurarWebGL();

            var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (escenas.Length == 0) escenas = new[] { "Assets/Scenes/SanAndres.unity" };

            var opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = CarpetaSalida,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = probar ? BuildOptions.AutoRunPlayer : BuildOptions.None
            };

            Debug.Log("[San Andrés] Exportando para WebGL… (la primera vez puede tardar 10-20 minutos)");
            BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
            var r = reporte.summary;

            if (r.result == BuildResult.Succeeded)
            {
                string ruta = Path.GetFullPath(CarpetaSalida);
                Debug.Log($"[San Andrés] Build web listo en {ruta} · {r.totalSize / (1024f * 1024f):0.0} MB · {r.totalTime:mm\\:ss}");
                if (interactivo && !probar)
                {
                    if (EditorUtility.DisplayDialog("Build web listo",
                            $"El juego quedó exportado en:\n{ruta}\n\n" +
                            "Para jugarlo hay que abrirlo desde un servidor (itch.io, GitHub Pages, o la opción " +
                            "\"Exportar y probar en el navegador\"). Hacer doble clic en index.html no funciona.",
                            "Abrir carpeta", "Cerrar"))
                        EditorUtility.RevealInFinder(Path.Combine(ruta, "index.html"));
                }
                return true;
            }

            Debug.LogError($"[San Andrés] El build web falló ({r.result}) con {r.totalErrors} error(es). Revisa la consola.");
            if (interactivo) EditorUtility.DisplayDialog("Build web", $"El build falló ({r.result}). Revisa la consola de Unity.", "OK");
            return false;
        }

        /// <summary>
        /// Ajustes de WebGL pensados para subir el juego a cualquier hosting (itch.io, GitHub Pages, servidor propio).
        /// </summary>
        static void ConfigurarWebGL()
        {
            // Gzip + "Decompression Fallback": el navegador descomprime aunque el servidor no envíe
            // la cabecera Content-Encoding, así funciona en itch.io y GitHub Pages sin configurar nada.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            // Data Caching queda apagado: en Unity 6 combinado con Decompression Fallback el juego
            // se queda cargando con "Cannot read properties of undefined (reading 'match')" (bug de Unity).
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Los materiales del juego se crean por código, así que Unity no "ve" que se usen la emisión
        /// (faros) ni el GPU instancing, y podría quitar esas variantes del shader al compilar.
        /// Dos materiales en Resources las fuerzan a entrar en el build.
        /// </summary>
        static void AsegurarVariantesDeShader()
        {
            var shader = Shader.Find(LitURP);
            if (shader == null)
            {
                Debug.LogWarning("[San Andrés] No se encontró el shader URP/Lit; se omite la reserva de variantes.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                if (!AssetDatabase.IsValidFolder("Assets/SanAndres/Resources"))
                    AssetDatabase.CreateFolder("Assets/SanAndres", "Resources");
                AssetDatabase.CreateFolder("Assets/SanAndres/Resources", "Materiales");
            }

            CrearMaterial("VarianteLit", shader, false);
            CrearMaterial("VarianteLitEmisivo", shader, true);
            AssetDatabase.SaveAssets();
        }

        static void CrearMaterial(string nombre, Shader shader, bool emisivo)
        {
            string ruta = $"{CarpetaMateriales}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            bool nuevo = m == null;
            if (nuevo) m = new Material(shader);
            m.shader = shader;
            m.enableInstancing = true;
            if (emisivo)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (nuevo) AssetDatabase.CreateAsset(m, ruta);
            else EditorUtility.SetDirty(m);
        }
    }
}
