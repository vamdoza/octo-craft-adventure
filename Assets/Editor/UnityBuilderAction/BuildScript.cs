using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;

namespace UnityBuilderAction
{
    public static class BuildScript
    {
        private static readonly string Eol = Environment.NewLine;

        private static readonly string[] Secrets =
            {"androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass"};

        public static void Build()
        {
            Dictionary<string, string> options = GetValidatedOptions();
            ApplyPlayerSettings(options);

            var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            ApplyAndroidSettings(buildTarget, options);

            int buildSubtarget = GetStandaloneBuildSubtarget(options);
            BuildPlayer(buildTarget, buildSubtarget, options["customBuildPath"]);
        }

        public static void BuildWithAddressables()
        {
            Dictionary<string, string> options = GetValidatedOptions();
            ApplyPlayerSettings(options);

            var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            EnsureBuildTarget(buildTarget);
            ApplyAndroidSettings(buildTarget, options);

            BuildAddressablesContent();

            int buildSubtarget = GetStandaloneBuildSubtarget(options);
            BuildPlayer(buildTarget, buildSubtarget, options["customBuildPath"]);
        }

        public static void BuildAddressablesOnly()
        {
            Dictionary<string, string> options = GetValidatedOptions();

            var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            EnsureBuildTarget(buildTarget);

            BuildAddressablesContent();
            EditorApplication.Exit(0);
        }

        private static void BuildAddressablesContent()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Console.WriteLine("AddressableAssetSettings not found.");
                EditorApplication.Exit(110);
            }

            Console.WriteLine($"Active Addressables profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}");

            Console.WriteLine("Cleaning addressables player content...");
            AddressableAssetSettings.CleanPlayerContent();

            Console.WriteLine("Building addressables player content...");
            AddressableAssetSettings.BuildPlayerContent();
            Console.WriteLine("Addressables content build finished.");
        }

        private static void EnsureBuildTarget(BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.activeBuildTarget == buildTarget)
            {
                return;
            }

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget))
            {
                Console.WriteLine($"Failed to switch active build target to {buildTarget}.");
                EditorApplication.Exit(122);
            }
        }

        private static void ApplyPlayerSettings(Dictionary<string, string> options)
        {
            if (options.TryGetValue("buildVersion", out string buildVersion) && buildVersion != "none")
            {
                PlayerSettings.bundleVersion = buildVersion;
                PlayerSettings.macOS.buildNumber = buildVersion;
            }

            if (options.TryGetValue("androidVersionCode", out string versionCode) && versionCode != "0")
            {
                PlayerSettings.Android.bundleVersionCode = int.Parse(versionCode);
            }
        }

        private static void ApplyAndroidSettings(BuildTarget buildTarget, Dictionary<string, string> options)
        {
            if (buildTarget != BuildTarget.Android)
            {
                return;
            }

            EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
            if (options.TryGetValue("androidKeystoreName", out string keystoreName) &&
                !string.IsNullOrEmpty(keystoreName))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystoreName;
            }

            if (options.TryGetValue("androidKeystorePass", out string keystorePass) &&
                !string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
            }

            if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) &&
                !string.IsNullOrEmpty(keyaliasName))
            {
                PlayerSettings.Android.keyaliasName = keyaliasName;
            }

            if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) &&
                !string.IsNullOrEmpty(keyaliasPass))
            {
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }

            if (options.TryGetValue("androidTargetSdkVersion", out string androidTargetSdkVersion) &&
                !string.IsNullOrEmpty(androidTargetSdkVersion))
            {
                var targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                try
                {
                    targetSdkVersion =
                        (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), androidTargetSdkVersion);
                }
                catch
                {
                    Console.WriteLine("Failed to parse androidTargetSdkVersion! Fallback to AndroidApiLevelAuto");
                }

                PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
            }
        }

        private static int GetStandaloneBuildSubtarget(Dictionary<string, string> options)
        {
#if UNITY_2021_2_OR_NEWER
            if (!options.TryGetValue("standaloneBuildSubtarget", out var subtargetValue) ||
                !Enum.TryParse(subtargetValue, out StandaloneBuildSubtarget buildSubtargetValue))
            {
                buildSubtargetValue = default;
            }

            return (int)buildSubtargetValue;
#else
            return 0;
#endif
        }

        private static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out string _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (validatedOptions.TryGetValue("buildTarget", out var buildTarget))
            {
                if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
                {
                    Console.WriteLine($"{buildTarget} is not a defined {nameof(BuildTarget)}");
                    EditorApplication.Exit(121);
                }
            }
            else
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!validatedOptions.TryGetValue("customBuildPath", out string _))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }

            const string defaultCustomBuildName = "TestBuild";
            if (!validatedOptions.TryGetValue("customBuildName", out string customBuildName))
            {
                Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }
            else if (customBuildName == "")
            {
                Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }

            return validatedOptions;
        }

        private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();

            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"# Parsing settings #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}"
            );

            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                bool isFlag = args[current].StartsWith("-");
                if (!isFlag)
                {
                    continue;
                }

                string flag = args[current].TrimStart('-');
                bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
                string value = flagHasValue ? args[next].TrimStart('-') : "";
                bool secret = Secrets.Contains(flag);
                string displayValue = secret ? "*HIDDEN*" : "\"" + value + "\"";

                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
        }

        private static void BuildPlayer(BuildTarget buildTarget, int buildSubtarget, string filePath)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = buildTarget,
                locationPathName = filePath,
#if UNITY_2021_2_OR_NEWER
                subtarget = buildSubtarget
#endif
            };

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }

        private static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"# Build results #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}" +
                $"Duration: {summary.totalTime.ToString()}{Eol}" +
                $"Warnings: {summary.totalWarnings.ToString()}{Eol}" +
                $"Errors: {summary.totalErrors.ToString()}{Eol}" +
                $"Size: {summary.totalSize.ToString()} bytes{Eol}" +
                $"{Eol}"
            );
        }

        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }
    }
}
