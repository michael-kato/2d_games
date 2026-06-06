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

        private const int MaxProfilerThreadCount = 128;
        private const int MaxConsecutiveInvalidThreadViews = 8;

        private static readonly string[] MainThreadWaitKeywords = new[]
        {
            "wait",
            "lock",
            "join",
            "mutex",
            "semaphore",
            "barrier",
            "signal",
            "spinlock",
            "sleep",
            "event",
            "await",
            "yield",
            "task.wait",
            "waitforjob",
            "waitforothers",
            "waitforcompletion"
        };

        private static readonly string[] PotentialBlockerKeywords = new[]
        {
            "job",
            "worker",
            "background",
            "thread pool",
            "async",
            "compute",
            "dispatch",
            "task",
            "pool",
            "system thread"
        };

        private static readonly string[] IdleWaitNoiseKeywords = new[]
        {
            "idle",
            "semaphore.waitforsignal",
            "workerthread.sleep"
        };

        private static readonly string[] GpuVsyncBoundKeywords = new[]
        {
            "waitforlastpresentationandupdatetime",
            "gfx.presentframe"
        };

        private class ThreadSummary
        {
            public int ThreadIndex;
            public string RootName = string.Empty;
            public int RootItemId;
            public double TotalTimeMs;
            public double SelfTimeMs;
            public bool IsMainThread;
            public bool HasMainThreadWait;
            public bool HasPotentialBlockerSamples;
            public bool IsIdleOrWaitNoise;
            public bool HasGpuVsyncBound;
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

            var threadSummaries = EnumerateProfilerThreads(frameIndex, minTotalTimeMs);
            if (threadSummaries.Count == 0)
            {
                Debug.LogWarning($"No profiler thread data for frame {frameIndex}");
                return;
            }

            sb.AppendLine($"Frame Threads: {threadSummaries.Count}");
            sb.AppendLine();

            bool mainGpuBound = threadSummaries.Exists(t => t.IsMainThread && t.HasGpuVsyncBound);
            bool anyMainThreadWait = threadSummaries.Exists(t => t.IsMainThread && t.HasMainThreadWait) && !mainGpuBound;
            bool anyPotentialBlocker = threadSummaries.Exists(t => !t.IsMainThread && t.HasPotentialBlockerSamples);

            if (mainGpuBound)
            {
                sb.AppendLine("INFO: Target frame rate or VSync limit reached. Main thread throttled by hardware presentation loop.");
                sb.AppendLine();
            }
            else if (anyMainThreadWait && anyPotentialBlocker)
            {
                sb.AppendLine("WARNING: Main thread appears to be waiting on work from other threads.");
                sb.AppendLine("Marked threads may contain potential main-thread blockers.");
                sb.AppendLine();
            }

            foreach (var summary in threadSummaries)
            {
                using (var threadView = ProfilerDriver.GetHierarchyFrameDataView(
                           frameIndex,
                           summary.ThreadIndex,
                           HierarchyFrameDataView.ViewModes.Default,
                           HierarchyFrameDataView.columnTotalTime,
                           false))
                {
                    if (threadView == null || !threadView.valid)
                        continue;

                    sb.AppendLine($"--- THREAD {summary.ThreadIndex}: {summary.RootName} ---");
                    sb.AppendLine($"  Thread Total: {summary.TotalTimeMs:F2}ms  Self: {summary.SelfTimeMs:F2}ms");
                    if (summary.IsMainThread)
                        sb.AppendLine("  NOTE: Main thread section.");
                    if (summary.IsIdleOrWaitNoise)
                        sb.AppendLine("  NOTE: Idle/wait noise detected; this thread is not flagged as a blocker.");
                    if (summary.HasGpuVsyncBound)
                        sb.AppendLine("  NOTE: GPU/VSync bound detected on main thread.");
                    if (summary.HasMainThreadWait)
                        sb.AppendLine("  NOTE: Main thread wait/synchronization candidate detected.");
                    if (summary.HasPotentialBlockerSamples)
                        sb.AppendLine("  NOTE: Potential blocker samples detected on this thread.");
                    sb.AppendLine();

                    ExportHierarchyItem(threadView, summary.RootItemId, sb, 0, minTotalTimeMs, true);
                    sb.AppendLine();
                }
            }

            string path = Application.dataPath + "/../ProfilerExport_Frame_" + frameIndex + ".txt";
            System.IO.File.WriteAllText(path, sb.ToString());

            Debug.Log($"Profiler frame exported to: {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static List<ThreadSummary> EnumerateProfilerThreads(int frameIndex, float minTotalTimeMs)
        {
            var threads = new List<ThreadSummary>();
            int consecutiveInvalid = 0;

            for (int threadIndex = 0; threadIndex < MaxProfilerThreadCount; threadIndex++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                           frameIndex,
                           threadIndex,
                           HierarchyFrameDataView.ViewModes.Default,
                           HierarchyFrameDataView.columnTotalTime,
                           false))
                {
                    if (view == null || !view.valid)
                    {
                        consecutiveInvalid++;
                        if (consecutiveInvalid >= MaxConsecutiveInvalidThreadViews)
                            break;
                        continue;
                    }

                    int rootItemId = view.GetRootItemID();
                    if (rootItemId == HierarchyFrameDataView.invalidSampleId)
                    {
                        consecutiveInvalid++;
                        if (consecutiveInvalid >= MaxConsecutiveInvalidThreadViews)
                            break;
                        continue;
                    }

                    if (!HierarchyContainsAnySample(view, rootItemId, minTotalTimeMs))
                    {
                        consecutiveInvalid++;
                        if (consecutiveInvalid >= MaxConsecutiveInvalidThreadViews)
                            break;
                        continue;
                    }

                    string rootName = view.GetItemName(rootItemId) ?? $"Thread {threadIndex}";
                    double totalTime = view.GetItemColumnDataAsDouble(rootItemId, HierarchyFrameDataView.columnTotalTime);
                    double selfTime = view.GetItemColumnDataAsDouble(rootItemId, HierarchyFrameDataView.columnSelfTime);

                    threads.Add(new ThreadSummary
                    {
                        ThreadIndex = threadIndex,
                        RootName = rootName,
                        RootItemId = rootItemId,
                        TotalTimeMs = totalTime,
                        SelfTimeMs = selfTime
                    });

                    consecutiveInvalid = 0;
                }
            }

            int mainThreadIndex = DetermineMainThreadIndex(threads);
            foreach (var summary in threads)
                summary.IsMainThread = summary.ThreadIndex == mainThreadIndex;

            foreach (var summary in threads)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                           frameIndex,
                           summary.ThreadIndex,
                           HierarchyFrameDataView.ViewModes.Default,
                           HierarchyFrameDataView.columnTotalTime,
                           false))
                {
                    if (view == null || !view.valid)
                        continue;

                    summary.IsIdleOrWaitNoise = IsIdleNoise(view, summary.RootItemId);
                    summary.HasGpuVsyncBound = summary.IsMainThread && IsGpuVsyncBound(view, summary.RootItemId, summary.TotalTimeMs);

                    if (summary.IsIdleOrWaitNoise)
                    {
                        summary.HasMainThreadWait = false;
                        summary.HasPotentialBlockerSamples = false;
                        continue;
                    }

                    if (summary.IsMainThread)
                    {
                        summary.HasMainThreadWait = !summary.HasGpuVsyncBound && HierarchyContainsAnyKeyword(view, summary.RootItemId, minTotalTimeMs, MainThreadWaitKeywords);
                    }
                    else
                    {
                        summary.HasPotentialBlockerSamples = HierarchyContainsAnyKeyword(view, summary.RootItemId, minTotalTimeMs, PotentialBlockerKeywords);
                    }
                }
            }

            return threads;
        }

        private static int DetermineMainThreadIndex(List<ThreadSummary> threads)
        {
            for (int i = 0; i < threads.Count; i++)
            {
                var name = threads[i].RootName;
                if (name != null && name.IndexOf("main", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return threads[i].ThreadIndex;
            }

            if (threads.Count > 0)
                return threads[0].ThreadIndex;

            return 0;
        }

        private static bool IsIdleNoise(HierarchyFrameDataView view, int itemId)
        {
            string rootName = view.GetItemName(itemId);
            if (MatchesAnyKeyword(rootName, IdleWaitNoiseKeywords))
                return true;

            string highestTotalName = GetHighestTotalSampleName(view, itemId);
            return MatchesAnyKeyword(highestTotalName, IdleWaitNoiseKeywords);
        }

        private static bool IsGpuVsyncBound(HierarchyFrameDataView view, int itemId, double threadTotalTime)
        {
            string rootName = view.GetItemName(itemId);
            if (MatchesAnyKeyword(rootName, GpuVsyncBoundKeywords))
                return true;

            double gpuSelfTime = GetTotalSelfTimeForKeywords(view, itemId, GpuVsyncBoundKeywords);
            return threadTotalTime > 0.0 && gpuSelfTime >= threadTotalTime * 0.5;
        }

        private static string GetHighestTotalSampleName(HierarchyFrameDataView view, int itemId)
        {
            var best = GetHighestTotalSample(view, itemId);
            return best.Name;
        }

        private static (string Name, double Total) GetHighestTotalSample(HierarchyFrameDataView view, int itemId)
        {
            string name = view.GetItemName(itemId);
            double total = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime);

            if (!view.HasItemChildren(itemId))
                return (name, total);

            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (int childId in children)
            {
                var childBest = GetHighestTotalSample(view, childId);
                if (childBest.Total > total)
                {
                    name = childBest.Name;
                    total = childBest.Total;
                }
            }

            return (name, total);
        }

        private static double GetTotalSelfTimeForKeywords(HierarchyFrameDataView view, int itemId, string[] keywords)
        {
            double selfTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
            string name = view.GetItemName(itemId);
            double total = MatchesAnyKeyword(name, keywords) ? selfTime : 0.0;

            if (!view.HasItemChildren(itemId))
                return total;

            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (int childId in children)
                total += GetTotalSelfTimeForKeywords(view, childId, keywords);

            return total;
        }

        private static bool MatchesAnyKeyword(string sampleName, string[] keywords)
        {
            if (string.IsNullOrEmpty(sampleName))
                return false;

            string lower = sampleName.ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (lower.Contains(keyword.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private static bool HierarchyContainsAnySample(HierarchyFrameDataView view, int itemId, float minTotalTimeMs)
        {
            double selfTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
            if (selfTime >= minTotalTimeMs)
                return true;

            if (!view.HasItemChildren(itemId))
                return false;

            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (int childId in children)
            {
                if (HierarchyContainsAnySample(view, childId, minTotalTimeMs))
                    return true;
            }

            return false;
        }

        private static bool HierarchyContainsAnyKeyword(
            HierarchyFrameDataView view,
            int itemId,
            float minTotalTimeMs,
            string[] keywords)
        {
            string name = view.GetItemName(itemId);
            if (name != null && ContainsKeyword(name, keywords))
            {
                double selfTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
                double totalTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime);
                if (selfTime >= minTotalTimeMs || totalTime >= minTotalTimeMs)
                    return true;
            }

            if (!view.HasItemChildren(itemId))
                return false;

            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (int childId in children)
            {
                if (HierarchyContainsAnyKeyword(view, childId, minTotalTimeMs, keywords))
                    return true;
            }

            return false;
        }

        private static bool ContainsKeyword(string sampleName, string[] keywords)
        {
            if (string.IsNullOrEmpty(sampleName))
                return false;

            string lower = sampleName.ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (lower.Contains(keyword.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private static void ExportHierarchyItem(
            HierarchyFrameDataView view,
            int itemId,
            StringBuilder sb,
            int depth,
            float minTotalTimeMs,
            bool alwaysExportRoot = false)
        {
            double totalTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime);
            string indent = new string(' ', depth * 2);
            string name = view.GetItemName(itemId);
            double selfTime = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
            string calls = view.GetItemColumnData(itemId, HierarchyFrameDataView.columnCalls);

            if (!alwaysExportRoot && selfTime < minTotalTimeMs)
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