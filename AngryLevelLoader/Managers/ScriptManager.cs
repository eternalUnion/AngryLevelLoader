using AngryLevelLoader.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AngryLevelLoader.Managers
{
    /// <summary>
    /// Used to load signed custom scripts for angry levels.
    /// </summary>
    public static class ScriptManager
    {
        private static Dictionary<string, string> loadedScriptsDict = new Dictionary<string, string>();
        private static IEnumerable<string> loadedScripts => loadedScriptsDict.Keys;
        public enum LoadScriptResult
        {
            Loaded,
            NotFound,
            NoCertificate,
            InvalidCertificate,
        }

        /// <summary>
        /// Attempt to load a local custom script if it has a valid certificate.
        /// </summary>
        /// <param name="scriptName">Full name of the script, including the .dll extension.</param>
        public static LoadScriptResult AttemptLoadScriptWithCertificate(string scriptName)
        {
            if (loadedScripts.Contains(scriptName))
                return LoadScriptResult.Loaded;

            string scriptPath = Path.Combine(AngryPaths.ScriptsPath, scriptName);
            if (!File.Exists(scriptPath))
                return LoadScriptResult.NotFound;
            if (!File.Exists(scriptPath + ".cert"))
                return LoadScriptResult.NoCertificate;

            if (!AngryCryptographyUtils.VerifyFileCertificate(scriptPath, scriptPath + ".cert"))
                return LoadScriptResult.InvalidCertificate;

            byte[] script = File.ReadAllBytes(scriptPath);
			Assembly a = Assembly.Load(script);
            loadedScriptsDict[scriptName] = AngryCryptographyUtils.GetMD5String(script);
            return LoadScriptResult.Loaded;
        }

		/// <summary>
		/// This method should NOT be used without user consent.
		/// </summary>
		/// <param name="scriptName">Full name of the script, including the .dll extension.</param>
		internal static void ForceLoadScript(string scriptName)
        {
            string scriptPath = Path.Combine(AngryPaths.ScriptsPath, scriptName);
			byte[] script = File.ReadAllBytes(scriptPath);
			Assembly a = Assembly.Load(script);
			loadedScriptsDict[scriptName] = AngryCryptographyUtils.GetMD5String(script);
		}

        /// <summary>
        /// Returns true if the given script is currently loaded in the application domain.
        /// </summary>
        /// <param name="scriptName">Full name of the script, including the .dll extension.</param>
        public static bool ScriptLoaded(string scriptName)
        {
            return loadedScripts.Contains(scriptName);
        }

		/// <summary>
		/// Returns true if the given script is present in the scripts folder.
		/// </summary>
		/// <param name="scriptName">Full name of the script, including the .dll extension.</param>
		public static bool ScriptExists(string scriptName)
        {
            return File.Exists(Path.Combine(Plugin.workingDir, "Scripts", scriptName));
        }

        /// <summary>
        /// Returns true if an outdated version of the script is loaded into the application domain.
        /// This situation can occur if the script is updated while it was already loaded.
        /// In such situations, game restart is required to reload the newer script.
        /// </summary>
        /// <param name="scriptName"></param>
        public static bool ScriptChanged(string scriptName)
        {
            if (!loadedScriptsDict.TryGetValue(scriptName, out string hash) || !File.Exists(Path.Combine(AngryPaths.ScriptsPath, scriptName)))
                return false;

            return hash != AngryCryptographyUtils.GetMD5String(File.ReadAllBytes(Path.Combine(AngryPaths.ScriptsPath, scriptName)));
        }

        /// <summary>
        /// Returns a list of scripts required to load levels from the given bundle.
        /// </summary>
        public static List<string> GetRequiredScriptsFromBundle(BundleContainer bundleContainer)
        {
            List<string> requiredScripts = new List<string>();
            foreach (var data in bundleContainer.GetAllRudeLevelData())
            {
                if (data.requiredDllNames == null)
                    continue;

                foreach (string script in data.requiredDllNames)
                    if (!requiredScripts.Contains(script))
                        requiredScripts.Add(script);
            }

            return requiredScripts;
        }
    }
}
