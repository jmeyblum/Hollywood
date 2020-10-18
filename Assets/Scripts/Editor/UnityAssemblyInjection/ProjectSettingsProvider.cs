using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	public class ProjectSettingsProvider : SettingsProvider
	{
		public static readonly string ProjectSettingPath = Path.GetFullPath(Path.Combine("ProjectSettings", $"{nameof(Hollywood)}.asset"));
		private static readonly FileInfo ProjectSettingFileInfo = new FileInfo(ProjectSettingPath);

		private static UnityEditor.Editor Editor;

		private static ProjectSettings _projectSettings;
		private static ProjectSettings ProjectSettings
		{
			get
			{
				if (!_projectSettings)
				{
					_projectSettings = TryLoadProjectSettings();

					if (!_projectSettings)
					{
						_projectSettings = ScriptableObject.CreateInstance<ProjectSettings>();
					}
				}

				return _projectSettings;
			}
		}

		public ProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
			: base(path, scopes, keywords)
		{
		}

		public static ProjectSettings TryLoadProjectSettings()
		{
			var projectSettingsObjects = InternalEditorUtility.LoadSerializedFileAndForget(ProjectSettingPath);
			if (projectSettingsObjects?.Length > 0 && projectSettingsObjects[0] is ProjectSettings projectSettings)
			{
				return projectSettings;
			}

			return null;
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new ProjectSettingsProvider(nameof(Hollywood), SettingsScope.Project)
			{
				guiHandler = (searchContext) =>
				{
					UnityEditor.Editor.CreateCachedEditor(ProjectSettings, null, ref Editor);

					bool changed = DrawProjectSettings();

					SaveProjectSettings(changed);
				},
			};

			return provider;
		}

		private static bool DrawProjectSettings()
		{
			bool changed = false;

			using (var _ = new EditorGUI.DisabledScope(ProjectSettingFileInfo.Exists && ProjectSettingFileInfo.IsReadOnly))
			{
				EditorGUI.BeginChangeCheck();
				Editor.OnInspectorGUI();

				if (EditorGUI.EndChangeCheck())
				{
					Editor.serializedObject.ApplyModifiedProperties();
					EditorUtility.SetDirty(Editor.target);
					changed = true;
				}
			}

			return changed;
		}

		private static void SaveProjectSettings(bool changed)
		{
			var projectSettingsAsset = new UnityEditor.VersionControl.Asset(ProjectSettingPath);
			var projectSettingsAssetList = new UnityEditor.VersionControl.AssetList() { projectSettingsAsset };

			if (changed && !ProjectSettingFileInfo.Exists && UnityEditor.VersionControl.Provider.isActive && UnityEditor.VersionControl.Provider.AddIsValid(projectSettingsAssetList))
			{
				UnityEditor.VersionControl.Provider.Add(projectSettingsAsset, false).Wait();
			}

			if (ProjectSettingFileInfo.Exists && ProjectSettingFileInfo.IsReadOnly && UnityEditor.VersionControl.Provider.isActive && UnityEditor.VersionControl.Provider.hasCheckoutSupport && GUILayout.Button("Checkout"))
			{
				UnityEditor.VersionControl.Provider.Checkout(projectSettingsAsset, UnityEditor.VersionControl.CheckoutMode.Exact);
			}

			if (changed && (!ProjectSettingFileInfo.Exists || !ProjectSettingFileInfo.IsReadOnly))
			{
				InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { Editor.target }, ProjectSettingPath, true);
			}
		}
	}
}