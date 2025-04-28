//Tool for replacing all instances of an asset with another
//UI Found under Tools/ZenithVal/Texture Consolidator

//HOWTO:
//Assign a master asset (Texture you want to be the source)
//Add duplicate assets (Textures you want replaced by the master texture)
//Click "Find Duplicates" to find all references to the duplicate asset
//Click "Replace Duplicates with Master" to replace all found duplicate assets with the master asset
//Click "Delete Duplicate Assets" to delete all duplicate assets

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

public class AssetConsolidator : EditorWindow
{
	private Object masterObject;
	private List<Object> replacedObjects = new List<Object>();
	private Dictionary<Object, List<Object>> objectReplaceMap = new Dictionary<Object, List<Object>>();

	[MenuItem("Tools/ZenithVal/Asset Consolidator")]
	public static void ShowWindow()
	{
		GetWindow<AssetConsolidator>("Asset Consolidator");
	}

	private void OnGUI()
	{
		GUILayout.Label("Master Asset", EditorStyles.boldLabel);
		masterObject = EditorGUILayout.ObjectField(masterObject, typeof(Object), false);

		GUILayout.Space(10);
		GUILayout.Label("Duplicate Assets", EditorStyles.boldLabel);

		if (GUILayout.Button("Add Duplicate Asset"))
		{
			replacedObjects.Add(null);
		}

		for (int i = 0; i < replacedObjects.Count; i++)
		{
			GUILayout.BeginHorizontal();
			replacedObjects[i] = EditorGUILayout.ObjectField(replacedObjects[i], typeof(Object), false);

			if (GUILayout.Button("Remove", GUILayout.Width(60)))
			{
				replacedObjects.RemoveAt(i);
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.Space(10);
		if (GUILayout.Button("Find Duplicate Usages"))
		{
			FindDuplicateUsages();
		}

		if (objectReplaceMap.Count > 0)
		{
			GUILayout.Space(10);
			GUILayout.Label("Found Duplicate Usages", EditorStyles.boldLabel);
			foreach (var entry in objectReplaceMap)
			{
				GUILayout.Label("Object: " + entry.Key.name);
				foreach (var obj in entry.Value)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label($"    {obj.GetType()}: " + obj.name);
					if (GUILayout.Button("Select", GUILayout.Width(60)))
					{
						Selection.activeObject = obj;
						EditorGUIUtility.PingObject(obj);
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
		if (objectReplaceMap.Count == 0 && objectReplaceMap.Count > 0)
		{
			GUILayout.Space(5);
			GUILayout.Label("No materials found using duplicate textures.", EditorStyles.boldLabel);
		}

		GUILayout.Space(5);
		if (GUILayout.Button("Delete Duplicate Assets"))
		{
			if (objectReplaceMap.Count == 0)
			{
				GUI.enabled = false;
			}
			if (EditorUtility.DisplayDialog("Confirm Deletion", "Are you sure you want to delete duplicate assets?", "Yes", "No"))
			{
				DeleteDuplicates();
			}
		}
	}

	private void FindDuplicateUsages()
	{
		if (!MasterCheck()) return;

		objectReplaceMap.Clear();

		string[] allObjects = AssetDatabase.FindAssets("a:assets t:Scene t:Prefab");

		foreach (string guid in allObjects)
		{
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
			if (obj == null) continue;	
			if (obj == masterObject) continue;

			foreach (Object replacedAsset in replacedObjects)
			{
				if (replacedAsset == null) continue;

				string replacedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(replacedAsset));
				string[] dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(obj));

				foreach (string dependency in dependencies)
				{
					string dependencyGUID = AssetDatabase.AssetPathToGUID(dependency);
					if (dependencyGUID == replacedGUID)
					{
						Debug.Log("Found duplicate usage: " + obj.name + " using " + replacedAsset.name);
						if (!objectReplaceMap.ContainsKey(replacedAsset))
						{
							objectReplaceMap.Add(replacedAsset, new List<Object>());
						}

						objectReplaceMap[replacedAsset].Add(obj);
					}
				}

			}

        }
	}

	private void ReplaceDuplicatesWithMaster()
	{
		if (!MasterCheck()) return;

		string masterGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(masterObject));

		foreach (Object replacedAsset in replacedObjects)
		{
			string replacedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(replacedAsset));

			foreach (var entry in objectReplaceMap)
			{
				foreach (Object obj in entry.Value)
				{
					string path = AssetDatabase.GetAssetPath(obj);
					string[] dependencies = AssetDatabase.GetDependencies(path);

					foreach (string dependency in dependencies)
					{
						string dependencyGUID = AssetDatabase.AssetPathToGUID(dependency);
						if (dependencyGUID == replacedGUID)
						{
							string text = File.ReadAllText(path);
							text = text.Replace(replacedGUID, masterGUID);
							File.WriteAllText(path, text);
						}
					}
				}
			}
		}

		objectReplaceMap.Clear();
		AssetDatabase.Refresh();
	}

	private void DeleteDuplicates()
	{
		if (!MasterCheck()) return;

		foreach (Object replacedAsset in replacedObjects)
		{
			if (replacedAsset != null)
			{
				string path = AssetDatabase.GetAssetPath(replacedAsset);
				AssetDatabase.MoveAssetToTrash(path);
			}
		}

		replacedObjects.Clear();
		AssetDatabase.Refresh();
	}

	private bool MasterCheck()
	{
		if (masterObject == null)
		{
			EditorUtility.DisplayDialog("Error", "Master texture is not assigned.", "OK");
			return false;
		}

		if (replacedObjects.Contains(masterObject))
		{
			EditorUtility.DisplayDialog("Error", "Master texture cannot be in the list of duplicates.", "OK");
			return false;
		}

		return true;
	}
}
#endif