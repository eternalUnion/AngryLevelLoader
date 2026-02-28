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
    public static class ScriptManager
    {
        public static string ScriptsPath => Path.Combine(Plugin.workingDir, "Scripts");

        private static Dictionary<string, string> loadedScriptsDict = new Dictionary<string, string>();
        private static IEnumerable<string> loadedScripts => loadedScriptsDict.Keys;
        public enum LoadScriptResult
        {
            Loaded,
            NotFound,
            NoCertificate,
            InvalidCertificate,
        }

        public static LoadScriptResult AttemptLoadScriptWithCertificate(string scriptName)
        {
            if (loadedScripts.Contains(scriptName))
                return LoadScriptResult.Loaded;

            string scriptPath = Path.Combine(ScriptsPath, scriptName);
            if (!File.Exists(scriptPath))
                return LoadScriptResult.NotFound;
            if (!File.Exists(scriptPath + ".cert"))
                return LoadScriptResult.NoCertificate;

            if (!CryptographyUtils.VerifyFileCertificate(scriptPath, scriptPath + ".cert"))
                return LoadScriptResult.InvalidCertificate;

            byte[] script = File.ReadAllBytes(scriptPath);
			Assembly a = Assembly.Load(script);
            loadedScriptsDict[scriptName] = CryptographyUtils.GetMD5String(script);
            return LoadScriptResult.Loaded;
        }

        public static void ForceLoadScript(string scriptName)
        {
            string scriptPath = Path.Combine(ScriptsPath, scriptName);
			byte[] script = File.ReadAllBytes(scriptPath);
			Assembly a = Assembly.Load(script);
			loadedScriptsDict[scriptName] = CryptographyUtils.GetMD5String(script);
		}

        public static bool ScriptLoaded(string scriptName)
        {
            return loadedScripts.Contains(scriptName);
        }

        public static bool ScriptExists(string scriptName)
        {
            return File.Exists(Path.Combine(Plugin.workingDir, "Scripts", scriptName));
        }

        public static bool ScriptChanged(string scriptName)
        {
            if (!loadedScriptsDict.TryGetValue(scriptName, out string hash) || !File.Exists(Path.Combine(ScriptsPath, scriptName)))
                return false;

            return hash != CryptographyUtils.GetMD5String(File.ReadAllBytes(Path.Combine(ScriptsPath, scriptName)));
        }

        public static List<string> GetRequiredScriptsFromBundle(AngryBundleContainer bundleContainer)
        {
            List<string> requiredScripts = new List<string>();
            foreach (var data in bundleContainer.GetAllLevelData())
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
