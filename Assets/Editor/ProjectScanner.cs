using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ProjectHealthScanner : EditorWindow
{
    private List<string> _namingIssues = new List<string>();
    private List<string> _structureIssues = new List<string>();
    private Vector2 _scrollPos;

    [MenuItem("Window/CustomTools/Project Health Scanner")]
    public static void ShowWindow()
    {
        GetWindow<ProjectHealthScanner>("Project Health");
    }

    private void OnGUI()
    {
        GUILayout.Label("Project Structure & Naming Scanner", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Scan Project Assets", GUILayout.Height(30)))
        {
            RunScan();
        }

        EditorGUILayout.Space();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        DisplaySection("Structure Issues (Folder Organization)", _structureIssues, Color.yellow);
        DisplaySection("Naming Issues (Conventions)", _namingIssues, new Color(1f, 0.5f, 0.5f));

        EditorGUILayout.EndScrollView();
        
        
        
        // Inside your OnGUI
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // Calculate how many items can actually fit on screen to save draw calls
        int totalIssues = _namingIssues.Count;
        float itemHeight = 20f; // Approx height of one label
        int firstVisible = Mathf.Max(0, (int)(_scrollPos.y / itemHeight));
        int lastVisible = Mathf.Min(totalIssues, firstVisible + (int)(position.height / itemHeight) + 5);

        // Add "spacer" so the scrollbar reflects the actual total size
        GUILayout.Space(firstVisible * itemHeight);

        for (int i = firstVisible; i < lastVisible; i++)
        {
            // Draw only what we can see
            EditorGUILayout.LabelField(_namingIssues[i]);
        }

        // Add spacer for the bottom
        GUILayout.Space((totalIssues - lastVisible) * itemHeight);

        EditorGUILayout.EndScrollView();
    }

    private void DisplaySection(string title, List<string> list, Color color)
    {
        GUI.color = Color.white;
        GUILayout.Label($"{title} ({list.Count})", EditorStyles.boldLabel);
        GUI.color = color;
        foreach (var item in list)
        {
            GUILayout.Label("- " + item, EditorStyles.wordWrappedLabel);
        }
        if (list.Count == 0) GUILayout.Label("No issues found! Clean as a whistle.");
        EditorGUILayout.Space();
    }

    private void RunScan()
    {
        _namingIssues.Clear();
        _structureIssues.Clear();

        string[] allPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string path in allPaths)
        {
            // Only scan the Assets folder, skip internal/package files
            if (!path.StartsWith("Assets")) continue;

            string fileName = Path.GetFileName(path);
            string extension = Path.GetExtension(path).ToLower();

            // 1. Check for Spaces (Universal "No" in Unity)
            if (fileName.Contains(" "))
            {
                _namingIssues.Add($"[Space Found]: {path}");
            }

            // 2. Folder Conventions (PascalCase)
            if (AssetDatabase.IsValidFolder(path))
            {
                if (!char.IsUpper(fileName[0]))
                    _structureIssues.Add($"[Folder Case]: {path} (Should be PascalCase)");
                continue;
            }

            // 3. Script Conventions (PascalCase, matches class)
            if (extension == ".cs")
            {
                if (!char.IsUpper(fileName[0]))
                    _namingIssues.Add($"[Script Case]: {path} (Scripts must be PascalCase)");
            }

            // 4. Asset Conventions (Textures/Audio: snake_case or kebab-case)
            // Modern studios prefer lowercase-with-separators for assets used in web/marketing
            if (extension == ".png" || extension == ".jpg" || extension == ".wav" || extension == ".mp3")
            {
                if (Regex.IsMatch(fileName, @"[A-Z]"))
                    _namingIssues.Add($"[Asset Case]: {path} (Use lowercase_snake_case for textures/audio)");
            }
            
            // 5. Structure Check: Avoid root-level loose files
            string directory = Path.GetDirectoryName(path);
            if (directory == "Assets" && !fileName.EndsWith(".unity"))
            {
                _structureIssues.Add($"[Loose File]: {fileName} is in the root Assets folder. Move to a subfolder.");
            }
        }
    }
}