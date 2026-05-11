using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateWeaponPrefabs
{
    private const string PrefabFolder = "Assets/Guns/Prefabs";

    // Positions matched from existing placeholder prefabs in Assets/Prefabs/
    private static readonly Dictionary<string, Vector3> Offsets = new()
    {
        { "Sniper",     new Vector3(0.22f, -0.19f,  0.55f) },
        { "Rifle",      new Vector3(0.25f, -0.22f,  0.50f) },
        { "Machinegun", new Vector3(0.25f, -0.22f,  0.50f) },
        { "Shotgun",    new Vector3(0.22f, -0.20f,  0.42f) },
    };

    [MenuItem("Tools/Create Weapon Prefabs")]
    public static void CreatePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            string[] parts = PrefabFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            AssetDatabase.Refresh();
        }

        foreach (var kvp in Offsets)
        {
            string weaponName = kvp.Key;
            Vector3 offset    = kvp.Value;

            GameObject sceneObj = GameObject.Find(weaponName);
            if (sceneObj == null)
            {
                Debug.LogWarning($"[CreateWeaponPrefabs] '{weaponName}' not found in scene. Skipping.");
                continue;
            }

            // Temporarily apply camera-relative offset before saving so the prefab
            // spawns in the correct position under Main Camera. Scene object is
            // restored to (0,0,0) afterward.
            Vector3 originalPos = sceneObj.transform.localPosition;
            sceneObj.transform.localPosition = offset;

            string prefabPath = $"{PrefabFolder}/{weaponName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(sceneObj, prefabPath, out bool success);

            sceneObj.transform.localPosition = originalPos;

            if (success)
                Debug.Log($"[CreateWeaponPrefabs] Created: {prefabPath}  pos={offset}  scale={sceneObj.transform.localScale}");
            else
                Debug.LogError($"[CreateWeaponPrefabs] Failed to create prefab for '{weaponName}'.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateWeaponPrefabs] Done.");
    }
}
