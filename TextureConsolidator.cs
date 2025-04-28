//Tool for merging duplicate textures.
//UI Found under Tools/ZenithVal/Texture Consolidator

//HOWTO:
//Assign a master texture (Texture you want to be the source)
//Add duplicate textures (Textures you want replaced by the master texture)
//Click "Find Duplicates" to find all materials using the duplicate textures
//Click "Replace Duplicates with Master" to replace all found duplicate textures with the master texture
//Click "Delete Duplicate Textures" to delete all duplicate textures in the project

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class TextureConsolidator : EditorWindow
{
    private Texture2D masterTexture;
    private List<Texture2D> duplicateTextures = new List<Texture2D>();
    private Dictionary<Texture2D, List<Material>> textureMaterialsMap = new Dictionary<Texture2D, List<Material>>();

    [MenuItem("Tools/ZenithVal/Texture Consolidator")]
    public static void ShowWindow()
    {
        GetWindow<TextureConsolidator>("Texture Consolidator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Master Texture", EditorStyles.boldLabel);
        masterTexture = (Texture2D)EditorGUILayout.ObjectField(masterTexture, typeof(Texture2D), false);

        GUILayout.Space(10);
        GUILayout.Label("Duplicate Textures", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Duplicate Texture"))
        {
            duplicateTextures.Add(null);
        }

        for (int i = 0; i < duplicateTextures.Count; i++)
        {
            GUILayout.BeginHorizontal();
            duplicateTextures[i] = (Texture2D)EditorGUILayout.ObjectField(duplicateTextures[i], typeof(Texture2D), false);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                duplicateTextures.RemoveAt(i);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Find Duplicates"))
        {
            FindDuplicateMaterials();
        }

        if (textureMaterialsMap.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Found Materials Using Duplicate Textures", EditorStyles.boldLabel);
            foreach (var entry in textureMaterialsMap)
            {
                GUILayout.Label("Texture: " + entry.Key.name);
                foreach (var material in entry.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("    Material: " + material.name);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = material;
                        EditorGUIUtility.PingObject(material);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Replace Duplicates with Master"))
            {
                ReplaceDuplicatesWithMaster();
            }
        }
        if (textureMaterialsMap.Count == 0 && duplicateTextures.Count > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label("No materials found using duplicate textures.", EditorStyles.boldLabel);
        }

        GUILayout.Space(5);
        if (GUILayout.Button("Delete Duplicate Textures"))
        {
            if (EditorUtility.DisplayDialog("Confirm Deletion", "Are you sure you want to delete duplicate textures?", "Yes", "No"))
            {
                DeleteDuplicates();
            }
        }
    }

    private void FindDuplicateMaterials()
    {
        if (!MasterCheck()) return;

        textureMaterialsMap.Clear();
        foreach (Texture2D duplicateTexture in duplicateTextures)
        {
            if (duplicateTexture != null)
            {
                List<Material> foundMaterials = new List<Material>();
                string duplicateGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(duplicateTexture));
                Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();

                foreach (Material material in allMaterials)
                {
                    foreach (string propertyName in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(propertyName);
                        if (texture != null)
                        {
                            string textureGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
                            if (textureGUID == duplicateGUID)
                            {
                                foundMaterials.Add(material);
                            }
                        }
                    }
                }

                if (foundMaterials.Count > 0)
                {
                    textureMaterialsMap.Add(duplicateTexture, foundMaterials);
                }
            }
        }
    }

    private void ReplaceDuplicatesWithMaster()
    {
        if (!MasterCheck()) return;

        string masterGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(masterTexture));

        foreach (var entry in textureMaterialsMap)
        {
            foreach (Material material in entry.Value)
            {
                foreach (string propertyName in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(propertyName);
                    string textureGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
                    if (textureGUID == AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.Key)))
                    {
                        material.SetTexture(propertyName, masterTexture);
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    private void DeleteDuplicates()
    {
        if (!MasterCheck()) return;

        foreach (Texture2D duplicateTexture in duplicateTextures)
        {
            if (duplicateTexture != null)
            {
                string path = AssetDatabase.GetAssetPath(duplicateTexture);
                AssetDatabase.MoveAssetToTrash(path);
            }
        }

        AssetDatabase.Refresh();
    }

    private bool MasterCheck()
    {
        if (masterTexture == null)
        {
            EditorUtility.DisplayDialog("Error", "Master texture is not assigned.", "OK");
            return false;
        }

        if (duplicateTextures.Contains(masterTexture))
        {
            EditorUtility.DisplayDialog("Error", "Master texture cannot be in the list of duplicates.", "OK");
            return false;
        }

        return true;
    }
}
#endif