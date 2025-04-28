//ZenithVal 2025
//Tool for Generating Exponential Smoothing Blendtrees
//UI Found under Tools/ZenithVal/Smoothing Blendtree Generator

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;
using UnityEngine.UIElements;
using System;

public class SmoothingBTGenerator : EditorWindow
{
	private string smoothFactorParam = "ParamSmoothFactor";
	private float smoothFactor = 0.9f;

	private string InputParam = "ParamInput";
	private float inputMin = 0.0f;
	private float inputMax = 1.0f;

	private string outputParam = "ParamOutput";

	private bool createMAPrefab = true;
	private string savePath = "Assets/ZZZ_SmoothBlendtrees";
	private string saveName = "SmoothBlendTree";

	[MenuItem("Tools/ZenithVal/Smoothing Blendtree Generator")]
	public static void ShowWindow()
	{
		GetWindow<SmoothingBTGenerator>("Smoothing Blendtree Gen");
	}

	#region GUI Stuff
	private Vector2 scrollPos;

	private class Section : GUI.Scope
	{
		public Section(string label)
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			GUILayoutUtility.GetRect(15, 15, GUILayout.Width(15));
			EditorGUILayout.BeginVertical();
		}
		protected override void CloseScope()
		{
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
		}
	}

	private float fieldFloat(GUIContent label, float value)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			EditorGUILayout.LabelField(label, GUILayout.Width(110));
			var val = EditorGUILayout.FloatField(value, GUILayout.Width(50));
			return val;
		}
	}

	private string fieldString (GUIContent label, string value)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			EditorGUILayout.LabelField(label, GUILayout.Width(110));
			var val = EditorGUILayout.TextField(value, GUILayout.Width(180));
			return val;
		}
	}

	private float fieldSlider(GUIContent label, float value, float min, float max)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			EditorGUILayout.LabelField(label, GUILayout.Width(110));
			var val = EditorGUILayout.Slider(value, min, max, GUILayout.Width(180));
			return val;
		}
	}
	private bool fieldBool(GUIContent label, bool value)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			EditorGUILayout.LabelField(label, GUILayout.Width(110));
			var val = EditorGUILayout.Toggle(value, GUILayout.Width(20));
			return val;
		}
	}
	#endregion

	private void OnGUI()
	{
		using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
		{
			scrollPos = scrollView.scrollPosition;

			using (new Section("Smoothing"))
			{
				smoothFactorParam = fieldString(new GUIContent("Smooth Parameter:"), smoothFactorParam);
				smoothFactor = fieldSlider(new GUIContent("      Smooth Factor:"), smoothFactor, 0.0f, 1.0f);
			}
				using (new Section("Input"))
			{
				InputParam = fieldString(new GUIContent("     Input Parameter:"), InputParam);
				inputMin = fieldFloat(new GUIContent("	           Min:"), inputMin);
				inputMax = fieldFloat(new GUIContent("	          Max:"), inputMax);
			}
			using (new Section("Output"))
			{
				outputParam = fieldString(new GUIContent(" Output Parameter: "), outputParam);
			}

			EditorGUILayout.Space();

			using (new Section("Generator"))
			{
				createMAPrefab = fieldBool(new GUIContent(" Create MA Prefab:"), createMAPrefab);
				saveName = fieldString(new GUIContent("    Blendtree Name:"), saveName);
				GUILayout.Label("  Save to: " + savePath + "/");
				if (GUILayout.Button("Change Folder", GUILayout.Width(100)))
				{
					string path = EditorUtility.OpenFolderPanel("Select folder", savePath, "");
					if (!string.IsNullOrEmpty(path))
					{
						path = path.Replace(Application.dataPath, "Assets");
						savePath = path;
					}
				}

				EditorGUILayout.Space();

				if (GUILayout.Button("Generate Smoothing Blendtree", GUILayout.Width(297), GUILayout.Height(30)))
				{
					CreateSmoothingBlendTree(
						smoothFactorParam, smoothFactor,
						InputParam, inputMin, inputMax, outputParam,
						savePath, saveName);
				}

				EditorGUILayout.Space();
			}
		}
	}


	//create smoothing Blendtree
	private void CreateSmoothingBlendTree(
		string _smoothFactorParam, float _smoothFactor,
		string _inputParam, float _inputMin, float _inputMax, string _outputParam,
		string _savePath, string _saveName)
	{
		string fullsavePath = _savePath + "/" + _saveName;

		if (!AssetDatabase.IsValidFolder(fullsavePath))
		{
			AssetDatabase.CreateFolder(_savePath, _saveName);
		}
		else
		{
			EditorUtility.DisplayDialog("Folder already exists", "The folder " + _savePath + "/" + _saveName + " already exists. Please choose a different name.", "OK");
			return;
		}

		//Animation Clips
		AnimationClip clipOutputMin = new AnimationClip();
		AnimationClip clipOutputMax = new AnimationClip();

		clipOutputMin.SetCurve("", typeof(Animator), _outputParam, new AnimationCurve(new Keyframe(0, 0)));
		clipOutputMax.SetCurve("", typeof(Animator), _outputParam, new AnimationCurve(new Keyframe(0, 1)));

		//Save Animations
		AssetDatabase.CreateAsset(clipOutputMin, fullsavePath + "/" + "ZZZ_Driver-" + _outputParam + "_0.anim");
		AssetDatabase.CreateAsset(clipOutputMax, fullsavePath + "/" + "ZZZ_Driver-" + _outputParam + "_1.anim");

		//Blendtrees
		BlendTree blendTreeInput = new BlendTree();
		blendTreeInput.name = _saveName + "_Input";
		blendTreeInput.blendType = BlendTreeType.Simple1D;
		blendTreeInput.blendParameter = _inputParam;
		blendTreeInput.useAutomaticThresholds = false;
		blendTreeInput.AddChild(clipOutputMin, _inputMin);
		blendTreeInput.AddChild(clipOutputMax, _inputMax);

		BlendTree blendtreeOutput = new BlendTree();
		blendtreeOutput.name = _saveName + "_Output";
		blendtreeOutput.blendType = BlendTreeType.Simple1D;
		blendtreeOutput.blendParameter = _inputParam;
		blendtreeOutput.useAutomaticThresholds = false;
		blendtreeOutput.blendParameter = _outputParam;
		blendtreeOutput.AddChild(clipOutputMin, 0);
		blendtreeOutput.AddChild(clipOutputMax, 1);

		BlendTree blendTreeSmoothing = new BlendTree();
		blendTreeSmoothing.name = "_" + _saveName;
		blendTreeSmoothing.blendType = BlendTreeType.Simple1D;
		blendTreeSmoothing.blendParameter = _smoothFactorParam;
		blendTreeSmoothing.useAutomaticThresholds = false;
		blendTreeSmoothing.AddChild(blendTreeInput, 0);
		blendTreeSmoothing.AddChild(blendtreeOutput, 1);

		//Save blendtrees
		AssetDatabase.CreateAsset(blendTreeInput, fullsavePath + "/" + blendTreeInput.name + ".asset");
		AssetDatabase.CreateAsset(blendtreeOutput, fullsavePath + "/" + blendtreeOutput.name + ".asset");
		AssetDatabase.CreateAsset(blendTreeSmoothing, fullsavePath + "/" + blendTreeSmoothing.name + ".asset");

		Debug.Log("Smoothing Blendtree System created at " + fullsavePath);

		AssetDatabase.Refresh();
		EditorGUIUtility.PingObject(blendTreeSmoothing);

		if (createMAPrefab)
		{
			GameObject blendtreePrefab = new GameObject(_saveName);
			ModularAvatarParameters MA_Params = blendtreePrefab.AddComponent<ModularAvatarParameters>();
			MA_Params.parameters.Add(new ParameterConfig() {
				nameOrPrefix = _smoothFactorParam,
				syncType = ParameterSyncType.NotSynced,
				localOnly = true,
				defaultValue = _smoothFactor,
				hasExplicitDefaultValue = true,
				saved = false
			});

			ModularAvatarMergeBlendTree MA_BlendTree = blendtreePrefab.AddComponent<ModularAvatarMergeBlendTree>();
			MA_BlendTree.Motion = blendTreeSmoothing;

			var finalPrefab = PrefabUtility.SaveAsPrefabAsset(blendtreePrefab, fullsavePath + "/" + _saveName + ".prefab");
			DestroyImmediate(blendtreePrefab);
			EditorGUIUtility.PingObject(finalPrefab);	
		}

	}
}

#endif
