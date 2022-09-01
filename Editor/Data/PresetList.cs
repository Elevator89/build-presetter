using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Elevator89.BuildPresetter.Data
{
	[Serializable]
	public class PresetList
	{
		private const string PresetsFilePath = "../ProjectSettings/BuildPresets.json";

		[SerializeField]
		public string ActivePresetName;

		[SerializeField]
		public List<Preset> AvailablePresets;

		public Preset GetPreset(int presetIndex)
		{
			return AvailablePresets[presetIndex];
		}

		public Preset GetPreset(string presetName)
		{
			return AvailablePresets.Find(preset => preset.Name == presetName);
		}

		public void SetPreset(Preset configuration)
		{
			int index = AvailablePresets.FindIndex(s => s.Name == configuration.Name);
			if (index == -1)
			{
				AvailablePresets.Add(configuration);
			}
			else
			{
				AvailablePresets[index] = configuration;
			}
		}

		public void AddPreset(Preset configuration)
		{
			AvailablePresets.Add(configuration);
		}

		public void RemovePreset(string configurationName)
		{
			AvailablePresets.RemoveAll(conf => conf.Name == configurationName);
		}

		public static PresetList Load()
		{
			string presetListFullPath = Path.Combine(Application.dataPath, PresetsFilePath);

			if (!File.Exists(presetListFullPath))
			{
				Debug.LogError("No preset list file found at path: " + PresetsFilePath);
				return new PresetList();
			}

			string presetListJson = File.ReadAllText(presetListFullPath);

			try
			{
				return JsonUtility.FromJson<PresetList>(presetListJson);
			}
			catch (Exception)
			{
				Debug.LogError("Preset list asset " + presetListFullPath + " has bad format");
				return new PresetList();
			}
		}

		public static void Save(PresetList presetList)
		{
			string presetListJson = JsonUtility.ToJson(presetList, true);
			string presetListFullPath = Path.Combine(Application.dataPath, PresetsFilePath);
			File.WriteAllText(presetListFullPath, presetListJson);
		}
	}
}