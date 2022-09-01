using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Elevator89.BuildPresetter.Data;
using Elevator89.BuildPresetter.FolderHierarchy;

namespace Elevator89.BuildPresetter.UI
{
	public class PresetView
	{
		private Preset _preset = null;

		private Vector2 _scrollPosScenes;
		private Vector2 _scrollPosResources;
		private Vector2 _scrollPosStreamingAssets;

		private Texture2D _iconTexture = null;

		private string[] _existingScenePaths = null;
		private HashSet<string> _includedExistingScenesPaths = null;

		private string[] _existingResourcesFolderPaths = null;
		private HashSet<string> _includedExistingResourcesFolderPaths = null;

		[SerializeField]
		private readonly object _splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 33f, 33f, 33f }, new int[] { 50, 50, 50 }, null);

		private HierarchyAsset _selectedPresetStreamingAssetsHierarchy = null;

		public Preset GetPreset()
		{
			return _preset;
		}

		public void SetPreset(Preset preset)
		{
			_preset = preset;

			if (_preset != null)
			{
				_iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_preset.AppIconPath);
				_existingScenePaths = Util.FindAllScenesPaths().ToArray();
				_includedExistingScenesPaths = new HashSet<string>(_preset.IncludedScenes.Where(scenePath => _existingScenePaths.Contains(scenePath)));

				_existingResourcesFolderPaths = Util.FindResourcesFolders(searchIncluded: true, searchExcluded: true).ToArray();
				_includedExistingResourcesFolderPaths = new HashSet<string>(preset.IncludedResources.Where(path => _existingResourcesFolderPaths.Contains(path)));

				_selectedPresetStreamingAssetsHierarchy = StreamingAssetsUtil.GetStreamingAssetsHierarchyByLists(_preset.IncludedStreamingAssets);
			}
		}

		public void OnGUI()
		{
			if (_preset == null)
				return;

			{
				EditorGUI.BeginChangeCheck();
				string name = EditorGUILayout.TextField("Preset Name", _preset.Name);
				if (EditorGUI.EndChangeCheck())
					_preset.Name = name;
			}

			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
			{
				GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				{
					{
						EditorGUI.BeginChangeCheck();
						string appId = EditorGUILayout.TextField("App ID", _preset.AppId);
						if (EditorGUI.EndChangeCheck())
							_preset.AppId = appId;
					}

					{
						EditorGUI.BeginChangeCheck();
						string appName = EditorGUILayout.TextField("App name", _preset.AppName);
						if (EditorGUI.EndChangeCheck())
							_preset.AppName = appName;
					}

					{
						EditorGUI.BeginChangeCheck();
						string appIconPath = EditorGUILayout.TextField("App icon path", _preset.AppIconPath);
						if (EditorGUI.EndChangeCheck())
							_preset.AppIconPath = appIconPath;
					}
				}
				GUILayout.EndVertical();

				GUI.enabled = false;
				EditorGUILayout.ObjectField(_iconTexture, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64), GUILayout.ExpandWidth(false));
				GUI.enabled = true;
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);

			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
			{
				SplitterGUILayout.BeginHorizontalSplit(_splitterState);
				{
					GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
					{
						GUILayout.Label("Initial scene:");
						{
							EditorGUI.BeginChangeCheck();
							int initialSceneIndex = EditorGUILayout.Popup(_preset.InitialSceneIndex, _preset.IncludedScenes.Select(scenePath => scenePath.Replace('/', '\u2215')).ToArray());
							if (EditorGUI.EndChangeCheck())
								_preset.InitialSceneIndex = initialSceneIndex;
						}

						GUILayout.Label("Scenes:");
						_scrollPosScenes = EditorGUILayout.BeginScrollView(_scrollPosScenes, EditorStyles.helpBox);
						{
							bool includedExistingScenesPathsWereChanged = false;
							foreach (string existingScenePath in _existingScenePaths)
							{
								EditorGUI.BeginChangeCheck();
								bool isSceneIncluded = EditorGUILayout.ToggleLeft(existingScenePath, _includedExistingScenesPaths.Contains(existingScenePath));
								if (EditorGUI.EndChangeCheck())
								{
									if (isSceneIncluded)
										_includedExistingScenesPaths.Add(existingScenePath);
									else
										_includedExistingScenesPaths.Remove(existingScenePath);

									includedExistingScenesPathsWereChanged = true;
								}
							}

							if (includedExistingScenesPathsWereChanged)
								_preset.IncludedScenes = _includedExistingScenesPaths.ToList();
						}
						EditorGUILayout.EndScrollView();
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
					{
						GUILayout.Label("Resources:");

						_scrollPosResources = EditorGUILayout.BeginScrollView(_scrollPosResources, EditorStyles.helpBox);
						{
							bool includedExistingResourcesFolderPathsWereChanged = false;
							foreach (string existingResourcesFolderPath in _existingResourcesFolderPaths)
							{
								EditorGUI.BeginChangeCheck();
								bool areResourcesIncluded = EditorGUILayout.ToggleLeft(existingResourcesFolderPath, _includedExistingResourcesFolderPaths.Contains(existingResourcesFolderPath));
								if (EditorGUI.EndChangeCheck())
								{
									if (areResourcesIncluded)
										_includedExistingResourcesFolderPaths.Add(existingResourcesFolderPath);
									else
										_includedExistingResourcesFolderPaths.Remove(existingResourcesFolderPath);

									includedExistingResourcesFolderPathsWereChanged = true;
								}
							}

							if (includedExistingResourcesFolderPathsWereChanged)
								_preset.IncludedResources = _includedExistingResourcesFolderPaths.ToList();
						}
						EditorGUILayout.EndScrollView();
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
					{
						GUILayout.Label("Streaming assets:");

						// ToDo: Consider using TreeView: https://docs.unity3d.com/Manual/TreeViewAPI.html
						_scrollPosStreamingAssets = EditorGUILayout.BeginScrollView(_scrollPosStreamingAssets, EditorStyles.helpBox, GUILayout.ExpandWidth(true));
						{
							HierarchyAsset streamingAssetsHierarchy = StreamingAssetsUtil.GetStreamingAssetsHierarchyByLists(_preset.IncludedStreamingAssets);
							ShowStreamingAssetsFoldersAndFiles(0, parentIsIncluded: false, streamingAssetsHierarchy);
							_preset.IncludedStreamingAssets = StreamingAssetsUtil.GetAssetsListsByHierarchy(streamingAssetsHierarchy);
						}
						EditorGUILayout.EndScrollView();
					}
					GUILayout.EndVertical();
				}
				SplitterGUILayout.EndHorizontalSplit();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);

			{
				EditorGUI.BeginChangeCheck();
				BuildTargetGroup buildTargetGroup = (BuildTargetGroup)EditorGUILayout.EnumPopup("Target group", _preset.BuildTargetGroup);
				if (EditorGUI.EndChangeCheck())
					_preset.BuildTargetGroup = buildTargetGroup;
			}

			{
				EditorGUI.BeginChangeCheck();
				BuildTarget buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Target", _preset.BuildTarget);
				if (EditorGUI.EndChangeCheck())
					_preset.BuildTarget = buildTarget;
			}

			{
				EditorGUI.BeginChangeCheck();
				ScriptingImplementation scriptingImplementation = (ScriptingImplementation)EditorGUILayout.EnumPopup("ScriptingImplementation", _preset.ScriptingImplementation);
				if (EditorGUI.EndChangeCheck())
					_preset.ScriptingImplementation = scriptingImplementation;
			}

			{
				EditorGUI.BeginChangeCheck();
				string buildDirectory = EditorGUILayout.TextField("Build directory", _preset.BuildDirectory);
				if (EditorGUI.EndChangeCheck())
					_preset.BuildDirectory = buildDirectory;
			}

			{
				EditorGUI.BeginChangeCheck();
				string buildFileName = EditorGUILayout.TextField("Build file name", _preset.BuildFileName);
				if (EditorGUI.EndChangeCheck())
					_preset.BuildFileName = buildFileName;
			}

			{
				EditorGUI.BeginChangeCheck();
				string defineSymbols = EditorGUILayout.TextField("Define symbols", _preset.DefineSymbols);
				if (EditorGUI.EndChangeCheck())
					_preset.DefineSymbols = defineSymbols;
			}

			{
				EditorGUI.BeginChangeCheck();
				bool incrementalIl2CppBuild = EditorGUILayout.Toggle("Incremental IL2CPP Build", _preset.IncrementalIl2CppBuild);
				if (EditorGUI.EndChangeCheck())
					_preset.IncrementalIl2CppBuild = incrementalIl2CppBuild;
			}

			{
				EditorGUI.BeginChangeCheck();
				bool developmentBuild = EditorGUILayout.Toggle("Development Build", _preset.DevelopmentBuild);
				if (EditorGUI.EndChangeCheck())
					_preset.DevelopmentBuild = developmentBuild;
			}

			{
				EditorGUI.BeginChangeCheck();
				bool connectWithProfiler = EditorGUILayout.Toggle("Connect Profiler", _preset.ConnectWithProfiler);
				if (EditorGUI.EndChangeCheck())
					_preset.ConnectWithProfiler = connectWithProfiler;
			}

			{
				EditorGUI.BeginChangeCheck();
				bool useIncrementalGC = EditorGUILayout.Toggle("Use incremental GC", _preset.UseIncrementalGC);
				if (EditorGUI.EndChangeCheck())
					_preset.ConnectWithProfiler = useIncrementalGC;
			}
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
	}
}
