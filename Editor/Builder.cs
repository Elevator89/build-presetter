using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Elevator89.BuildPresetter.Data;

namespace Elevator89.BuildPresetter
{
	public static class Builder
	{
		[MenuItem("Build/Build with Acive Preset", isValidateFunction: false, 100)]
		private static void BuildWithAcivePreset()
		{
			PresetList presets = PresetList.Load();
			Builder.Build(presets.GetPreset(presets.ActivePresetName), false, null);
		}

		[MenuItem("Build/Build and Run with Acive Preset", isValidateFunction: false, 100)]
		private static void BuildAndRunWithAciveConfiguration()
		{
			PresetList presets = PresetList.Load();
			Builder.Build(presets.GetPreset(presets.ActivePresetName), true, null);
		}

		/// <summary>
		/// This method is called from CI system
		/// </summary>
		public static void BuildFromCommandLine()
		{
			string[] args = null;
			try
			{
				args = Environment.GetCommandLineArgs();
			}
			catch
			{
				throw new UnityException("Command line args not found");
			}

			if (args == null)
			{
				throw new UnityException("Commandline args unaccessable");
			}

			string buildPresetName = GetNamedArgument(args, "-buildPresetName");
			string buildDirectory = GetNamedArgument(args, "-buildDirectory");
			string version = GetNamedArgument(args, "-appVersion");

			PresetList presets = PresetList.Load();
			Preset preset = presets.GetPreset(buildPresetName);

			if (preset == null)
			{
				throw new UnityException("No build preset with name " + buildPresetName + " was found");
			}

			if (buildDirectory != null)
			{
				preset.BuildDirectory = buildDirectory;
			}

			Build(preset, false, version);
		}

		public static void Build(Preset preset, bool run, string version)
		{
			if (preset.BuildDirectory == null)
			{
				throw new UnityException("Build directory is not set");
			}

			AddressableAssetSettings.CleanPlayerContent();
			AddressableAssetSettings.BuildPlayerContent();

			Preset previousPreset = Presetter.GetCurrent();

			Presetter.SetCurrent(preset);

			PlayerSettings.bundleVersion = string.IsNullOrWhiteSpace(version) ? "" : version;

			try
			{
				BuildInternal(preset, run);
			}
			finally
			{
				Presetter.SetCurrent(previousPreset);
			}
		}

		private static string GetNamedArgument(string[] args, string name)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == name && args.Length > i + 1)
				{
					return args[i + 1];
				}
			}
			return null;
		}

		private static void BuildInternal(Preset preset, bool run)
		{
			EditorUserBuildSettings.SwitchActiveBuildTarget(preset.BuildTargetGroup, preset.BuildTarget);

			if (preset.BuildDirectory != null && !Directory.Exists(preset.BuildDirectory))
			{
				Directory.CreateDirectory(preset.BuildDirectory);
			}

			BuildOptions buildOptions = BuildOptions.None;
			if (preset.DevelopmentBuild)
				buildOptions = buildOptions | BuildOptions.Development;
			if (preset.ConnectWithProfiler)
				buildOptions = buildOptions | BuildOptions.ConnectWithProfiler;
			if (run)
				buildOptions = buildOptions | BuildOptions.AutoRunPlayer;

			List<string> enabledScenes = preset.IncludedScenes.ToList();
			string initialScene = enabledScenes[preset.InitialSceneIndex];
			enabledScenes.RemoveAt(preset.InitialSceneIndex);
			enabledScenes.Insert(0, initialScene);
			string buildPath = Path.Combine(preset.BuildDirectory, preset.BuildFileName);

			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
			{
				target = preset.BuildTarget,
				targetGroup = preset.BuildTargetGroup,
				options = buildOptions,
				scenes = enabledScenes.ToArray(),
				locationPathName = buildPath
			};

			BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

			if (buildReport.summary.result != BuildResult.Succeeded)
			{
				string[] errorLog = buildReport.steps.SelectMany(
					step =>
						step.messages.Where(
							m => m.type == LogType.Assert || m.type == LogType.Error || m.type == LogType.Exception)
							.Select(m => m.content)).ToArray();

				throw new Exception("BuildPlayer failure: " + string.Join(Environment.NewLine, errorLog));
			}
		}
	}
}
