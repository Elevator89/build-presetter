using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Elevator89.BuildPresetter.Data;
using Elevator89.BuildPresetter.FolderHierarchy;

namespace Elevator89.BuildPresetter.UI
{
	public class PresetListWindow : EditorWindow
	{
		private PresetList _presets;
		private string[] _presetNames;

		private int _selectedPresetIndex = -1;

		private Vector2 _scrollPos;

		private PresetView _presetView = null;

		[MenuItem("Build/Presets...", isValidateFunction: false, 200)]
		private static void OpenSettings()
		{
			PresetListWindow window = GetWindow<PresetListWindow>(false, "Build Presets", true);
			window.minSize = new Vector2(400.0f, 380.0f);
			window.Show();
		}

		private void OnEnable()
		{
			_presets = PresetList.Load();
			_presetNames = _presets.AvailablePresets.Select(prst => prst.Name).ToArray();
			_selectedPresetIndex = Array.IndexOf(_presetNames, _presets.ActivePresetName);

			_presetView = new PresetView();

			Preset selectedPreset = _presets.GetPreset(_selectedPresetIndex);
			_presetView.SetPreset(selectedPreset);
		}

		private void OnGUI()
		{
			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

			GUILayout.Space(10);

			Preset selectedPreset = _presets.GetPreset(_selectedPresetIndex);

			EditorGUI.BeginChangeCheck();
			_selectedPresetIndex = EditorGUILayout.Popup("Preset", _selectedPresetIndex, _presetNames);
			if (EditorGUI.EndChangeCheck())
			{
				selectedPreset = _presets.GetPreset(_selectedPresetIndex);
				_presets.ActivePresetName = _presetNames[_selectedPresetIndex];
				_presetView.SetPreset(selectedPreset);
			}

			BuildMode buildMode = BuildMode.DoNotBuild;
			if (selectedPreset != null)
			{
				buildMode = ShowBuildPresetsGuiAndReturnBuildPress(selectedPreset);
			}

			GUILayout.Space(5);

			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
			{
				if (GUILayout.Button("Reload All", GUILayout.ExpandWidth(false)))
				{
					_presets = PresetList.Load();
				}
				if (GUILayout.Button("Save All", GUILayout.ExpandWidth(false)))
				{
					PresetList.Save(_presets);
				}
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();

			switch (buildMode)
			{
				case BuildMode.Build:
					Builder.Build(selectedPreset, false, null);
					break;
				case BuildMode.BuildAndRun:
					Builder.Build(selectedPreset, true, null);
					break;
				case BuildMode.DoNotBuild:
				default:
					break;
			}
		}

		private BuildMode ShowBuildPresetsGuiAndReturnBuildPress(Preset preset)
		{
			BuildMode buildMode = BuildMode.DoNotBuild;

			EditorGUILayout.BeginVertical("box");
			{
				GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
				{
					EditorGUILayout.LabelField(preset.AppName);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Clone", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(15)))
					{
						Preset clone = new Preset
						{
							Name = GenerateName(preset.Name, _presetNames),
							AppId = preset.AppId,
							AppName = preset.AppName,
							AppIconPath = preset.AppIconPath,
							BuildDirectory = preset.BuildDirectory,
							BuildFileName = preset.BuildFileName,
							BuildTarget = preset.BuildTarget,
							BuildTargetGroup = preset.BuildTargetGroup,
							ConnectWithProfiler = preset.ConnectWithProfiler,
							DefineSymbols = preset.DefineSymbols,
							DevelopmentBuild = preset.DevelopmentBuild,
							IncludedResources = new List<string>(preset.IncludedResources),
							IncludedStreamingAssets = new AssetsLists()
							{
								Files = preset.IncludedStreamingAssets.Files,
								Folders = preset.IncludedStreamingAssets.Folders
							},
							IncludedScenes = new List<string>(preset.IncludedScenes),
							InitialSceneIndex = preset.InitialSceneIndex,
							UseIncrementalGC = preset.UseIncrementalGC,
						};

						_presets.AddPreset(clone);
						_presets.ActivePresetName = clone.Name;

						_presetNames = _presets.AvailablePresets.Select(prst => prst.Name).ToArray();
					}
					if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(15)))
					{
						_presets.RemovePreset(preset.Name);
						_presets.ActivePresetName = _presets.AvailablePresets[0].Name;
					}

				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				bool presetNameWasSynced = preset.Name == _presets.ActivePresetName;

				_presetView.OnGUI();

				bool presetNameIsSynced = preset.Name == _presets.ActivePresetName;
				if (presetNameWasSynced && !presetNameIsSynced)
				{
					_presetNames[_selectedPresetIndex] = preset.Name;
					_presets.ActivePresetName = preset.Name;
				}

				GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
				{
					if (GUILayout.Button("Fill from Settings", GUILayout.ExpandWidth(false)))
					{
						Presetter.FillFromCurrent(preset);
					}

					if (GUILayout.Button("Apply to Settings", GUILayout.ExpandWidth(false)))
					{
						Presetter.SetCurrent(preset);
					}

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Build", GUILayout.ExpandWidth(false)))
					{
						buildMode = BuildMode.Build;
					}

					if (GUILayout.Button("Build and Run", GUILayout.ExpandWidth(false)))
					{
						buildMode = BuildMode.BuildAndRun;
					}
				}
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();

			return buildMode;
		}

		private void ShowStreamingAssetsFoldersAndFiles(int nestingLevel, bool parentIsIncluded, HierarchyAsset hierarchyAsset)
		{
			GUIStyle style = new GUIStyle(EditorStyles.label);
			style.contentOffset = new Vector2(nestingLevel * 20f, 0f);

			GUI.enabled = !parentIsIncluded;
			EditorGUI.BeginChangeCheck();

			bool isIncluded = EditorGUILayout.ToggleLeft(hierarchyAsset.Name, hierarchyAsset.IsIncluded, style);

			if (EditorGUI.EndChangeCheck())
				hierarchyAsset.IsIncluded = isIncluded;

			GUI.enabled = true;

			if (hierarchyAsset.Children.Count > 0)
			{
				foreach (HierarchyAsset child in hierarchyAsset.Children)
					ShowStreamingAssetsFoldersAndFiles(nestingLevel + 1, hierarchyAsset.IsIncluded, child);
			}
		}

		private static string GenerateName(string desiredName, string[] otherNames)
		{
			if (otherNames.Contains(desiredName))
			{
				int i = 1;
				string generatedName = desiredName + i;
				while (otherNames.Contains(generatedName))
				{
					i++;
					generatedName = desiredName + i;
				}
				return generatedName;
			}
			return desiredName;
		}
	}
}
