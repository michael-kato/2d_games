using UnityEditor;
using UnityEditor.Profiling;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace Editor
{
    public class ProfilerExportWizard : ScriptableWizard
    {
        [Tooltip("Frame index to export")]
        public int frameIndex = 0;

        [Tooltip("Skip samplers with total time below this threshold (ms). Set to 0 to show all.")]
        public float minTotalTimeMs = 0.01f;

        void OnWizardCreate()
        {
            ProfilerParse.ExportFrame(frameIndex, minTotalTimeMs);
        }

        void OnWizardUpdate()
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex - 1;
            isValid = frameIndex >= first && frameIndex <= last;
            errorString = isValid ? "" : $"Frame must be between {first} and {last}";
        }
    }

    public class ProfilerParse
    {
        [MenuItem("Tools/Export Profiler Frame")]
        public static void OpenExportDialog()
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex - 1;
            var wizard = ScriptableWizard.DisplayWizard<ProfilerExportWizard>(
                $"Export Profiler Frame  [{first} \u2013 {last}]", "Export");
            wizard.frameIndex = last;
        }

        public static void ExportFrame(int frameIndex, float minTotalTimeMs = 0.01f)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== PROFILER FRAME EXPORT ===");
            sb.AppendLine($"Frame: {frameIndex}");
            sb.AppendLine($"Time: {System.DateTime.Now}");
            sb.AppendLine($"Min threshold: {minTotalTimeMs}ms");
            sb.AppendLine();
            sb.AppendLine("--- CPU PROFILER DATA ---");

            using (var hierarchy = ProfilerDriver.GetHierarchyFrameDataView(
                       frameIndex,
                       0,
                       HierarchyFrameDataView.ViewModes.Default,
                       HierarchyFrameDataView.columnTotalTime,
                       false))
            {
                if (hierarchy == null || !hierarchy.valid)
                {
                    Debug.LogWarning($"No profiler data for frame {frameIndex}");
                    return;
                }

                sb.AppendLine($"Frame Time: {hierarchy.frameTimeMs:F2}ms  ({hierarchy.frameFps:F1} fps)");
                sb.AppendLine();
                ExportHierarchyItem(hierarchy, hierarchy.GetRootItemID(), sb, 0, minTotalTimeMs);
            }

            string path = Application.dataPath + "/../ProfilerExport_Frame_" + frameIndex + ".txt";
            System.IO.File.WriteAllText(path, sb.ToString());

            Debug.Log($"Profiler frame exported to: {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static void ExportHierarchyItem(HierarchyFrameDataView view, int itemId, StringBuilder sb, int depth, float minTotalTimeMs)
        {
            double totalTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime);
            string indent   = new string(' ', depth * 2);
            string name     = view.GetItemName(itemId);
            double selfTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
            string calls    = view.GetItemColumnData(itemId, HierarchyFrameDataView.columnCalls);

            if (selfTime < minTotalTimeMs)
                return;
            
            sb.AppendLine($"{indent}{name}");
            sb.AppendLine($"{indent}  Total: {totalTime:F2}ms");
            sb.AppendLine($"{indent}  Self:  {selfTime:F2}ms");
            sb.AppendLine($"{indent}  Calls: {calls}");

            if (view.HasItemChildren(itemId))
            {
                var children = new List<int>();
                view.GetItemChildren(itemId, children);
                foreach (int childId in children)
                    ExportHierarchyItem(view, childId, sb, depth + 1, minTotalTimeMs);
            }
        }
    }
}