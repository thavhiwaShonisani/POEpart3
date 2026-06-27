using System;
using System.Collections.Generic;
using System.Text;

namespace CypherBotWPF
{
    // ═════════════════════════════════════════════════════════════════
    //  ACTIVITYLOGMANAGER  (Task 4: Activity Log Feature)
    //
    //  Stores a running list of timestamped descriptions of every
    //  significant action CypherBot takes (tasks added, reminders set,
    //  quiz activity, NLP-recognised commands). The user can ask the
    //  bot to show this log at any time.
    // ═════════════════════════════════════════════════════════════════
    public class ActivityLogManager
    {
        // Each entry is a short, already-formatted description with timestamp.
        private readonly List<string> log = new List<string>();

        public int Count => log.Count;

        // Add a new entry to the log with the current timestamp.
        public void AddEntry(string description)
        {
            string timeStamped = $"{description} [{DateTime.Now:dd MMM, HH:mm}]";
            log.Add(timeStamped);
        }

        // Returns the most recent entries (defaults to last 10) formatted
        // for display in the chat window, matching the example in the brief.
        public string GetRecentEntriesFormatted(int maxEntries = 10)
        {
            if (log.Count == 0)
            {
                return "I haven't logged any actions yet. Try adding a task, setting a reminder, or starting the quiz!";
            }

            int startIndex = Math.Max(0, log.Count - maxEntries);
            var recent = log.GetRange(startIndex, log.Count - startIndex);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Here's a summary of recent actions:");
            for (int i = 0; i < recent.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {recent[i]}");
            }

            if (log.Count > maxEntries)
            {
                sb.Append($"(Showing the last {maxEntries} of {log.Count} actions. Say \"show full log\" to see everything.)");
            }

            return sb.ToString().TrimEnd();
        }

        // Returns the full history, used when the user asks to "show more" / "show full log".
        public string GetFullHistoryFormatted()
        {
            if (log.Count == 0)
            {
                return "I haven't logged any actions yet.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Here's your full activity history:");
            for (int i = 0; i < log.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {log[i]}");
            }
            return sb.ToString().TrimEnd();
        }
    }
}