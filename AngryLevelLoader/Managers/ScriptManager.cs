using AngryLevelLoader.Containers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace AngryLevelLoader.Managers
{
    public static class ScriptManager
    {
        private static List<string> loadedScripts = new List<string>();
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

            string scriptPath = Path.Combine(Plugin.workingDir, "Scripts", scriptName);
            if (!File.Exists(scriptPath))
                return LoadScriptResult.NotFound;
            if (!File.Exists(scriptPath + ".cert"))
                return LoadScriptResult.NoCertificate;

            if (!CryptographyUtils.VerifyFileCertificate(scriptPath, scriptPath + ".cert"))
                return LoadScriptResult.InvalidCertificate;

            Assembly a = Assembly.Load(File.ReadAllBytes(scriptPath));
            loadedScripts.Add(scriptName);
            return LoadScriptResult.Loaded;
        }

        public static void ForceLoadScript(string scriptName)
        {
            string scriptPath = Path.Combine(Plugin.workingDir, "Scripts", scriptName);
            Assembly.Load(File.ReadAllBytes(scriptPath));
            loadedScripts.Add(scriptName);
        }

        public static bool ScriptLoaded(string scriptName)
        {
            return loadedScripts.Contains(scriptName);
        }

        public static bool ScriptExists(string scriptName)
        {
            return File.Exists(Path.Combine(Plugin.workingDir, "Scripts", scriptName));
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
