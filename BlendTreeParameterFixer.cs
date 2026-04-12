// ZenithVal 2026
// Scans params used by BlendTrees in an animator and adds any missing ones to it's param list.
// Found under the context menu for animator assets (Select it then see 3 dots on inspector top right)

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class BlendTreeParameterFixer
{
	[MenuItem("CONTEXT/AnimatorController/Add BlendTree Parameters to Animator", false, 2000)]
	private static void FixParameters()
	{
		int totalAdded = 0;

		foreach (Object obj in Selection.objects)
		{
			AnimatorController controller = obj as AnimatorController;
			if (controller != null)
			{
				totalAdded += ProcessController(controller);
			}
		}

		if (totalAdded > 0)
		{
			AssetDatabase.SaveAssets();
			Debug.Log($"[BlendTreeParameterFixer] Added {totalAdded} missing parameters.");
		}
		else
		{
			Debug.Log("[BlendTreeParameterFixer] No missing float parameters.");
		}
	}

	private static int ProcessController(AnimatorController controller)
	{
		Dictionary<string, AnimatorControllerParameterType> existingParams = new Dictionary<string, AnimatorControllerParameterType>();
		foreach (var param in controller.parameters)
		{
			if (!existingParams.ContainsKey(param.name))
			{
				existingParams.Add(param.name, param.type);
			}
		}

		HashSet<string> requiredFloatParams = new HashSet<string>();

		foreach (var layer in controller.layers)
		{
			FindParametersInStateMachine(layer.stateMachine, requiredFloatParams);
		}

		int addedCount = 0;

		foreach (string paramName in requiredFloatParams)
		{
			if (string.IsNullOrEmpty(paramName)) continue;

			if (existingParams.ContainsKey(paramName))
			{
				// exists but not as a float
				if (existingParams[paramName] != AnimatorControllerParameterType.Float)
				{
					Debug.Log($"[BlendTreeParameterFixer] '{controller.name}' already has '{paramName}' but it is a {existingParams[paramName]} instead of a Float.");
				}
			}
			else
			{
				// completely missing
				controller.AddParameter(paramName, AnimatorControllerParameterType.Float);
				existingParams.Add(paramName, AnimatorControllerParameterType.Float); 

				addedCount++;
				Debug.Log($"[BlendTreeParameterFixer] Added '{paramName}' to '{controller.name}'.");
			}
		}

		if (addedCount > 0)
		{
			EditorUtility.SetDirty(controller);
		}

		return addedCount;
	}

	private static void FindParametersInStateMachine(AnimatorStateMachine stateMachine, HashSet<string> requiredParams)
	{
		foreach (var childState in stateMachine.states)
		{
			if (childState.state.motion is BlendTree blendTree)
			{
				ExtractParametersFromBlendTree(blendTree, requiredParams);
			}
		}

		foreach (var childStateMachine in stateMachine.stateMachines)
		{
			FindParametersInStateMachine(childStateMachine.stateMachine, requiredParams);
		}
	}

	private static void ExtractParametersFromBlendTree(BlendTree tree, HashSet<string> requiredParams)
	{
		if (tree == null) return;

		// 1D / 2D blendtrees
		if (tree.blendType != BlendTreeType.Direct)
		{
			requiredParams.Add(tree.blendParameter);

			// 2D BlendTrees have a 2nd param
			if (tree.blendType != BlendTreeType.Simple1D)
			{
				requiredParams.Add(tree.blendParameterY);
			}
		}

		// Direct and nested
		foreach (var child in tree.children)
		{
			// Direct have a parameter per child
			if (tree.blendType == BlendTreeType.Direct)
			{
				requiredParams.Add(child.directBlendParameter);
			}

			// If another BlendTree recursively extract from it
			if (child.motion is BlendTree nestedTree)
			{
				ExtractParametersFromBlendTree(nestedTree, requiredParams);
			}
		}
	}
}

#endif