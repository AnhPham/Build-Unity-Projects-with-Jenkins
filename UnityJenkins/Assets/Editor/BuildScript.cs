using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class BuildScript
{
    static string[] GetScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    [MenuItem("Jenkins/Run All Tests")]
    public static void RunAllTests()
    {
        Debug.Log("🧪 Running all tests...");

        var testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();

        // Subscribe to test events
        testRunner.RegisterCallbacks(new TestCallbacks());

        var filter = new Filter()
        {
            testMode = TestMode.PlayMode  // or TestMode.EditMode
        };

        testRunner.Execute(new ExecutionSettings
        {
            filters = new[] { filter }
        });
    }

    private class TestCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"▶️ Test Run Started: {testsToRun.TestCaseCount} tests detected");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"✅ Test Run Finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped");
            if (result.FailCount > 0)
            {
                Debug.LogError("❌ Some tests failed!");
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
            Debug.Log($"⏳ Running Test: {test.Name}");
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return; // only log leaf tests
            if (result.ResultState == "Passed")
                Debug.Log($"✅ {result.Name} Passed");
            else if (result.ResultState == "Failed")
                Debug.LogError($"❌ {result.Name} Failed: {result.Message}");
            else
                Debug.LogWarning($"⚠️ {result.Name} {result.ResultState}");
        }
    }

    [MenuItem("Jenkins/Build Addressables")]
    public static void BuildAddressables()
    {
        if (IsAddressablesAvailable())
        {
            Debug.Log("🔧 Building Addressables...");
            var type = System.Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettings");
            var method = type?.GetMethod("BuildPlayerContent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
            Debug.Log("✅ Addressables build completed.");
        }
        else
        {
            Debug.LogWarning("⚠️ Addressables package not found. Skipping Addressables build.");
        }
    }

    private static bool IsAddressablesAvailable()
    {
        return System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Any(t => t.FullName == "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings");
    }

    private static BuildOptions GetBuildOptions()
    {
        string val = (System.Environment.GetEnvironmentVariable("DEVELOPMENT_BUILD") ?? "false").ToLowerInvariant();
        bool isDev = val == "on" || val == "true" || val == "yes" || val == "1";

        BuildOptions opts = BuildOptions.None;
        if (isDev)
        {
            opts |= BuildOptions.Development;
            opts |= BuildOptions.ConnectWithProfiler;
            Debug.Log("🛠️ DEVELOPMENT_BUILD = ON → Development, Deep Profiling, Autoconnect Profiler enabled.");
        }
        else
        {
            Debug.Log("🧱 DEVELOPMENT_BUILD = OFF → Building in Release mode.");
        }

        return opts;
    }

    [MenuItem("Jenkins/Build Android")]
    public static void BuildAndroid()
    {
        BuildAddressables();

        string buildFormat = System.Environment.GetEnvironmentVariable("BUILD_ANDROID_FORMAT") ?? "APK";

        PlayerSettings.Android.keystorePass = System.Environment.GetEnvironmentVariable("KEYSTORE_PASS");
        PlayerSettings.Android.keyaliasPass = System.Environment.GetEnvironmentVariable("KEY_ALIAS_PASS");

        var productName = GetSanitizedProductName();
        var commonOptions = GetBuildOptions();

        if (buildFormat == "APK" || buildFormat == "Both")
        {
            string buildPath = string.Format("Builds/Android/{0}_{1}_{2}_GMT+7.apk", productName, Application.version, System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss"));

            Debug.Log("🔨 Starting Android APK build to " + buildPath);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = commonOptions
            };

            EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("✅ Android APK build succeeded: " + summary.totalSize + " bytes");
            }
            else
            {
                Debug.LogError("❌ Android APK build failed");
                EditorApplication.Exit(1);
            }
        }

        if (buildFormat == "AAB" || buildFormat == "Both")
        {
            string buildPath = string.Format("Builds/Android/{0}_{1}_{2}_GMT+7.aab", productName, Application.version, System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss"));

            Debug.Log("🔨 Starting Android AAB build to " + buildPath);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = commonOptions
            };

            EditorUserBuildSettings.buildAppBundle = true;
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("✅ Android AAB build succeeded: " + summary.totalSize + " bytes");
            }
            else
            {
                Debug.LogError("❌ Android AAB build failed");
                EditorApplication.Exit(1);
            }
        }
    }

    [MenuItem("Jenkins/Build iOS")]
    public static void BuildiOS()
    {
        BuildAddressables();

        string buildPath = "Builds/iOS";
        var commonOptions = GetBuildOptions();

        Debug.Log("🔨 Starting iOS build to " + buildPath);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            options = commonOptions
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("✅ iOS build succeeded");
        }
        else
        {
            Debug.LogError("❌ iOS build failed");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Jenkins/Build MacOS")]
    public static void BuildMacOS()
    {
        BuildAddressables();

        string buildPath = "Builds/MacOS/" + Application.productName;
        var commonOptions = GetBuildOptions();

        Debug.Log("🔨 Starting MacOS build to " + buildPath);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = commonOptions
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("✅ macOS build succeeded");
        }
        else
        {
            Debug.LogError("❌ macOS build failed");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Jenkins/Build Windows")]
    public static void BuildWindows()
    {
        BuildAddressables();

        string buildPath = "Builds/Windows/" + Application.productName + ".exe";
        var commonOptions = GetBuildOptions();

        Debug.Log("🔨 Starting Windows build to " + buildPath);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows,
            options = commonOptions
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("✅ Windows build succeeded");
        }
        else
        {
            Debug.LogError("❌ Windows build failed");
            EditorApplication.Exit(1);
        }
    }

    public static string GetSanitizedProductName()
    {
        string rawName = Application.productName;

        string sanitized = Regex.Replace(rawName, @"[^a-zA-Z0-9]", "");

        return sanitized;
    }
}