using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Helper utilities to apply a dungeon seed from command-line or via the Editor.
/// Usage (CLI):
///   -seed=123 or -dungeonSeed=123
///   Unity can be launched with -executeMethod TrainingSeedUtility.ApplySeedFromArgsToScene to run this in batch.
/// In the Editor menu: Tools/Training/Apply Seed From Command Line
/// </summary>
public static class TrainingSeedUtility
{
    // This method can be invoked via -executeMethod TrainingSeedUtility.ApplySeedFromArgsToScene
    // or from the Tools menu in the Editor.
#if UNITY_EDITOR
    [MenuItem("Tools/Training/Apply Seed From Command Line")]
    public static void ApplySeedFromCommandLineMenu()
    {
        ApplySeedFromArgsToScene();
    }
#endif

    public static void ApplySeedFromArgsToScene()
    {
        try
        {
            string[] args = Environment.GetCommandLineArgs();
            int seed = 0;
            bool found = false;

            foreach (var a in args)
            {
                if (a.StartsWith("-dungeonSeed=") || a.StartsWith("-seed="))
                {
                    var parts = a.Split('=');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int parsed))
                    {
                        seed = parsed;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                var env = Environment.GetEnvironmentVariable("DUNGEON_SEED");
                if (!string.IsNullOrEmpty(env) && int.TryParse(env, out int parsedEnv))
                {
                    seed = parsedEnv;
                    found = true;
                }
            }

            if (!found)
            {
#if UNITY_EDITOR
                Debug.Log("[TrainingSeedUtility] No dungeon seed found in command-line or environment.");
#endif
                return;
            }

            // Try to find an existing DungeonManager in the open scenes
#if UNITY_EDITOR
            var dm = UnityEngine.Object.FindObjectOfType<DungeonManager>();
            if (dm != null)
            {
                Undo.RecordObject(dm, "Apply Dungeon Seed");
                dm.useSeed = true;
                dm.seed = seed;
                EditorUtility.SetDirty(dm);
                EditorSceneManager.MarkSceneDirty(dm.gameObject.scene);
                Debug.LogFormat("[TrainingSeedUtility] Applied seed {0} to DungeonManager in scene.", seed);
            }
            else
            {
                // Store in EditorPrefs as fallback for runtime Pickup
                EditorPrefs.SetInt("TrainingDungeonSeed", seed);
                EditorPrefs.SetBool("TrainingDungeonUseSeed", true);
                Debug.LogFormat("[TrainingSeedUtility] DungeonManager not found; saved seed {0} to EditorPrefs.", seed);
            }
#else
            // Runtime: try to find and set the singleton at runtime (will affect current play session)
            var dmRuntime = UnityEngine.Object.FindObjectOfType<DungeonManager>();
            if (dmRuntime != null)
            {
                dmRuntime.useSeed = true;
                dmRuntime.seed = seed;
                Debug.LogFormat("[TrainingSeedUtility] Applied seed {0} to DungeonManager at runtime.", seed);
            }
#endif
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Debug.LogErrorFormat("[TrainingSeedUtility] Error applying seed: {0}", ex.Message);
#else
            Debug.LogErrorFormat("[TrainingSeedUtility] Error applying seed: {0}", ex.Message);
#endif
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Training/Clear Seed EditorPrefs")]
    public static void ClearSeedEditorPrefs()
    {
        if (EditorPrefs.HasKey("TrainingDungeonSeed"))
            EditorPrefs.DeleteKey("TrainingDungeonSeed");
        if (EditorPrefs.HasKey("TrainingDungeonUseSeed"))
            EditorPrefs.DeleteKey("TrainingDungeonUseSeed");

        Debug.Log("[TrainingSeedUtility] Cleared TrainingDungeonSeed and TrainingDungeonUseSeed from EditorPrefs.");
    }
#endif
}
