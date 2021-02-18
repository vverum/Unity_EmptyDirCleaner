using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vverum.Tools.EmptyDirCleaner
{
	public class EmptyDirCleanerView : EditorWindow
	{
		private Label resultsText;
		private string projectPath;

		[MenuItem("Tools/EmptyDirCleaner")]
		public static void ShowWindow()
		{
			EmptyDirCleanerView window = GetWindow<EmptyDirCleanerView>();
			window.titleContent = new GUIContent("Empty Dir Cleaner");
			window.minSize = new Vector2(360, 130);
		}

		private void OnEnable()
		{
			var root = rootVisualElement;

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{EmptyDirCleanerConstants.PACKAGE_PATH}/EmptyDirCleanerView.uss");
			root.styleSheets.Add(styleSheet);

			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{EmptyDirCleanerConstants.PACKAGE_PATH}/EmptyDirCleanerView.uxml");
			visualTree.CloneTree(root);

			SetupEvents(root);

			resultsText = root.Q<Label>("resultsText");
			projectPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/') + 1);
		}

		private void SetupEvents(VisualElement root)
		{
			var findAllButton = root.Q<Button>("findAllButton");
			findAllButton.clicked += () => FindAllEmpty();

			var removeSelectedButton = root.Q<Button>("removeSelectedButton");
			removeSelectedButton.clicked += () => RemoveEmptyInSelected();

			var removeAllButton = root.Q<Button>("removeAllButton");
			removeAllButton.clicked += () => RemoveAllEmpty();
		}

		private void FindAllEmpty()
		{
			EditorUtility.DisplayProgressBar("Find all empty folders", $"Searching for empty folders", 0f);
			CheckForEmptyDirs(@"Assets", out List<string> emptyDirs);
			emptyDirs.Insert(0, "Empty dirs:");
			ShowResults(emptyDirs);
			EditorUtility.ClearProgressBar();
		}

		private void RemoveEmptyInSelected()
		{
			EditorUtility.DisplayProgressBar("Removing empty in selected", $"Searching for empty folders", 0f);
			List<string> result = new List<string>();
			if (Selection.objects.Length > 0)
			{
				foreach (var item in Selection.objects)
				{
					if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(item)))
					{
						CheckForEmptyDirs(AssetDatabase.GetAssetPath(item), out List<string> emptyDirs);
						result.AddRange(emptyDirs);
					}
				}
			}
			EditorUtility.DisplayProgressBar("Removing empty in selected", $"Removing empty folders", 0.5f);
			RemoveDirs(result, out result);
			ShowResults(result);
			EditorUtility.ClearProgressBar();
		}

		private void RemoveAllEmpty()
		{
			EditorUtility.DisplayProgressBar("Removing all empty folders", $"Searching for empty folders", 0f);
			CheckForEmptyDirs(@"Assets", out List<string> emptyDirs);
			EditorUtility.DisplayProgressBar("Removing all empty folders", $"Removing empty folders", 0.5f);
			RemoveDirs(emptyDirs, out List<string> result);
			ShowResults(result);
			EditorUtility.ClearProgressBar();
		}

		private bool CheckForEmptyDirs(string path, out List<string> emptyDirs)
		{
			if (!AssetDatabase.IsValidFolder(path))
				throw new Exception("Directory dos not exist: {path}");

			emptyDirs = new List<string>();
			bool hasFiles = false;
			foreach (var item in AssetDatabase.GetSubFolders(path))
			{
				if (!CheckForEmptyDirs(item, out List<string> innerDirs))
				{
					hasFiles = true;
				}
				emptyDirs.AddRange(innerDirs);
			}

			if (hasFiles || Directory.GetFiles($"{projectPath}/{path}").Any(x => !x.EndsWith(".meta")))
			{
				return false;
			}

			emptyDirs.Add(path);
			return true;
		}

		private void RemoveDirs(List<string> filesPath, out List<string> result)
		{
			AssetDatabase.StopAssetEditing(); 
			result = new List<string>();
			try
			{
				foreach (var item in filesPath)
				{
					if (AssetDatabase.IsValidFolder(item))
					{
						AssetDatabase.DeleteAsset(item);
						result.Add($"removed: {item}");
					}
				}
			}
			catch { }
			finally
			{
				AssetDatabase.StartAssetEditing();
			}
		}

		private void ShowResults(List<string> results)
		{
			var text = new StringBuilder();

			foreach (var item in results)
			{
				text.AppendLine(item);
			}

			resultsText.text = text.ToString();
		}
	}
}