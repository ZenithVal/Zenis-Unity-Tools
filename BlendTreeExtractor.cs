using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if UNITY_EDITOR
public class BlendTreeExtractor
{
	private static Dictionary<BlendTree, BlendTree> extractedTrees;

	[MenuItem("CONTEXT/AnimatorController/Extract BlendTrees (Asset Bundled)", false, 2010)]
	private static void ExtractBundled() => PrepareExtraction(false);

	[MenuItem("CONTEXT/AnimatorController/Extract BlendTrees (Individually)", false, 2011)]
	private static void ExtractIndividually() => PrepareExtraction(true);

	private static void PrepareExtraction(bool individual)
	{
		AnimatorController controller = Selection.activeObject as AnimatorController;
		if (controller == null) return;

		string controllerPath = AssetDatabase.GetAssetPath(controller);

		if (individual)
		{
			int totalToExtract = 0;
			foreach (var layer in controller.layers)
			{
				totalToExtract += CountEmbeddedTrees(layer.stateMachine, controllerPath);
			}

			if (totalToExtract > 10)
			{
				if (!EditorUtility.DisplayDialog("Warning", $"That's {totalToExtract} blendtrees. Are you sure?", "Yes", "Cancel"))
					return;
			}
		}

		RunExtraction(controller, controllerPath, individual);
	}

	private static void RunExtraction(AnimatorController controller, string controllerPath, bool individual)
	{
		string controllerDir = Path.GetDirectoryName(controllerPath).Replace("\\", "/");
		string rootFolder = $"{controllerDir}/{controller.name} - Blendtrees";

		extractedTrees = new Dictionary<BlendTree, BlendTree>();

		try
		{
			AssetDatabase.StartAssetEditing();
			CreateFolderRecursively(rootFolder);

			foreach (var layer in controller.layers)
			{
				TraverseStateMachine(layer.stateMachine, controllerPath, rootFolder, individual);
			}

			foreach (var originalTree in extractedTrees.Keys)
			{
				if (originalTree != null) Object.DestroyImmediate(originalTree, true);
			}

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();
			Debug.Log($"<b>[BlendTree Extractor]</b> Extracted to: {rootFolder}");
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.Refresh();
			extractedTrees.Clear();
		}
	}

	private static void TraverseStateMachine(AnimatorStateMachine sm, string controllerPath, string rootFolder, bool individual)
	{
		foreach (var childState in sm.states)
		{
			if (childState.state.motion is BlendTree tree && AssetDatabase.GetAssetPath(tree) == controllerPath)
			{
				int maxDepth = GetMaxBlendTreeDepth(tree, controllerPath);
				childState.state.motion = ProcessBlendTree(tree, controllerPath, rootFolder, null, maxDepth, 0, individual);
			}
		}

		foreach (var childSm in sm.stateMachines)
		{
			TraverseStateMachine(childSm.stateMachine, controllerPath, rootFolder, individual);
		}
	}

	private static BlendTree ProcessBlendTree(BlendTree originalTree, string controllerPath, string rootFolder, BlendTree mainAsset, int maxDepth, int currentDepth, bool individual)
	{
		if (extractedTrees.ContainsKey(originalTree)) return extractedTrees[originalTree];

		int underscoreCount = Mathf.Max(0, maxDepth - currentDepth);
		string underscores = new string('_', underscoreCount);
		string baseName = string.IsNullOrEmpty(originalTree.name) ? "BlendTree" : originalTree.name;
		
		BlendTree newTree = Object.Instantiate(originalTree);
		newTree.name = underscores + baseName;
		extractedTrees[originalTree] = newTree;

		if (mainAsset == null)
		{
			string targetFolder = rootFolder;
			if (individual)
			{
				targetFolder = $"{rootFolder}/{MakeSafeForPath(baseName)}";
				CreateFolderRecursively(targetFolder);
			}

			string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{MakeSafeForPath(baseName)}.asset");
			AssetDatabase.CreateAsset(newTree, assetPath);
			if (!individual) mainAsset = newTree;
		}
		else
		{
			AssetDatabase.AddObjectToAsset(newTree, mainAsset);
			EditorUtility.SetDirty(mainAsset);
		}

		ChildMotion[] children = newTree.children;
		for (int i = 0; i < children.Length; i++)
		{
			if (children[i].motion is BlendTree childTree && AssetDatabase.GetAssetPath(childTree) == controllerPath)
			{
				children[i].motion = ProcessBlendTree(childTree, controllerPath, rootFolder, mainAsset, maxDepth, currentDepth + 1, individual);
			}
		}

		newTree.children = children;
		EditorUtility.SetDirty(newTree);
		return newTree;
	}

	private static int CountEmbeddedTrees(AnimatorStateMachine sm, string path)
	{
		int count = 0;
		foreach (var state in sm.states)
		{
			if (state.state.motion is BlendTree tree && AssetDatabase.GetAssetPath(tree) == path)
				count += CountAllChildren(tree, path);
		}
		foreach (var subSm in sm.stateMachines)
			count += CountEmbeddedTrees(subSm.stateMachine, path);
		return count;
	}

	private static int CountAllChildren(BlendTree tree, string path)
	{
		int count = 1;
		foreach (var child in tree.children)
		{
			if (child.motion is BlendTree childTree && AssetDatabase.GetAssetPath(childTree) == path)
				count += CountAllChildren(childTree, path);
		}
		return count;
	}

	private static int GetMaxBlendTreeDepth(BlendTree tree, string path)
	{
		int max = 0;
		foreach (var child in tree.children)
		{
			if (child.motion is BlendTree childTree && AssetDatabase.GetAssetPath(childTree) == path)
				max = Mathf.Max(max, GetMaxBlendTreeDepth(childTree, path) + 1);
		}
		return max;
	}

	private static void CreateFolderRecursively(string folderPath)
	{
		if (AssetDatabase.IsValidFolder(folderPath)) return;
		string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
		CreateFolderRecursively(parent);
		AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
	}

	private static string MakeSafeForPath(string name)
	{
		foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
		return name.Replace("/", "_").Replace("\\", "_");
	}
}
#endif