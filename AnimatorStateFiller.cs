using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimatorStateFiller
{
	// Base clip used. You'll probably wanna change this.
	private const string BaseClipGUID = "d7a23b6aeb510f44bb6cd8472387c239";

	[MenuItem("CONTEXT/AnimatorController/Fill Empty States", false, 2000)]
	private static void FillEmptyStates()
	{
		string assetPath = AssetDatabase.GUIDToAssetPath(BaseClipGUID);
		AnimationClip baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

		if (baseClip == null)
		{
			Debug.LogError($"[AnimatorStateFiller] Failed to load AnimationClip. Does '{BaseClipGUID}' exist?");
			return;
		}

		int totalModified = 0;

		foreach (Object obj in Selection.objects)
		{
			AnimatorController controller = obj as AnimatorController;
			if (controller != null)
			{
				int modifiedCount = 0;

				foreach (AnimatorControllerLayer layer in controller.layers)
				{
					modifiedCount += ProcessStateMachine(layer.stateMachine, baseClip);
				}

				if (modifiedCount > 0)
				{
					totalModified += modifiedCount;
					Debug.Log($"[AnimatorStateFiller] Filled {modifiedCount} empty states in '{controller.name}'");
				}
			}
		}

		if (totalModified > 0)
		{
			AssetDatabase.SaveAssets();
			Debug.Log($"[AnimatorStateFiller] Filled a total of {totalModified} empty states.");
		}
		else
		{
			Debug.Log("[AnimatorStateFiller] No empty states were found in the selected Animator(s).");
		}
	}

	private static int ProcessStateMachine(AnimatorStateMachine stateMachine, AnimationClip clipToAssign)
	{
		int modified = 0;

		foreach (ChildAnimatorState childState in stateMachine.states)
		{
			if (childState.state.motion == null)
			{
				childState.state.motion = clipToAssign;
				EditorUtility.SetDirty(childState.state);
				modified++;
			}
		}

		foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
		{
			modified += ProcessStateMachine(childStateMachine.stateMachine, clipToAssign);
		}

		return modified;
	}
}