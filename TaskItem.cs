using System;

namespace CypherBotWPF
{
    // ═════════════════════════════════════════════════════════════════
    //  TASKITEM  (Task 1: Task Assistant with Reminders)
    //
    //  Represents one task tracked by the chatbot.
    // ═════════════════════════════════════════════════════════════════
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ReminderText { get; set; }     // e.g. "3 days", "tomorrow" — null if no reminder
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool HasReminder => !string.IsNullOrWhiteSpace(ReminderText);

        // Display-friendly status for the DataGrid
        public string StatusDisplay => IsCompleted ? "Completed" : "Pending";

        public override string ToString()
        {
            string status = IsCompleted ? "[Completed]" : "[Pending]";
            string reminder = HasReminder ? $"Reminder: {ReminderText}" : "No reminder set";
            return $"{Title} — {Description} ({reminder}) {status}";
        }
    }
}
