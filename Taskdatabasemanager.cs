using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using MySqlConnector;

namespace CypherBotWPF
{
    // ═════════════════════════════════════════════════════════════════
    //  TASKDATABASEMANAGER  (Task 1: Database Integration / Task Storage)
    //
    //  Handles all MySQL CRUD operations for cybersecurity tasks.
    //
    //  ── SETUP ───────────────────────────────────────────────────────
    //  1. Install a local MySQL server (e.g. via XAMPP or MySQL
    //     Workbench / MySQL Community Server).
    //  2. Update the connection details below to match your setup.
    //  3. No manual SQL is required — EnsureDatabaseAndTable() creates
    //     the database and table automatically the first time the app
    //     runs.
    // ═════════════════════════════════════════════════════════════════
    public class TaskDatabaseManager
    {
        // ── TODO: update these to match your MySQL server ─────────────
        private const string Server = "localhost";
        private const string Port = "3306";
        private const string Database = "cypherbot_db";
        private const string UserId = "root";
        private const string Password = "";   // set your MySQL root/user password here

        private string ConnectionString =>
            $"Server={Server};Port={Port};Database={Database};Uid={UserId};Pwd={Password};";

        // Connection string WITHOUT the Database clause — used only to
        // create the database itself before it exists.
        private string ServerOnlyConnectionString =>
            $"Server={Server};Port={Port};Uid={UserId};Pwd={Password};";

        // Set to false if the database connection fails, so the chatbot
        // can gracefully fall back to an informative message instead of
        // crashing every time a task command is used.
        public bool IsAvailable { get; private set; } = true;
        public string LastError { get; private set; } = "";

        // ── Create the database/table if they don't already exist ─────
        public bool EnsureDatabaseAndTable()
        {
            try
            {
                using (var connection = new MySql.Data.MySqlClient.MySqlConnection(ServerOnlyConnectionString))
                {
                    connection.Open();
                    using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                        $"CREATE DATABASE IF NOT EXISTS {Database};", connection);
                    cmd.ExecuteNonQuery();
                }

                using (var connection = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString))
                {
                    connection.Open();
                    string createTable = @"
                CREATE TABLE IF NOT EXISTS tasks (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(255) NOT NULL,
                    Description TEXT,
                    ReminderText VARCHAR(255) NULL,
                    IsCompleted BOOLEAN NOT NULL DEFAULT 0,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                );";
                    using var cmd = new MySql.Data.MySqlClient.MySqlCommand(createTable, connection);
                    cmd.ExecuteNonQuery();
                }

                IsAvailable = true;
                return true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                LastError = ex.Message;
                return false;
            }
        }

        // ── Add a new task, returns the new row's Id (or -1 on failure) ─
        public int AddTask(string title, string description, string reminderText)
        {
            try
            {
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
                connection.Open();
                string insert = @"INSERT INTO tasks (Title, Description, ReminderText, IsCompleted)
                                   VALUES (@title, @description, @reminderText, 0);
                                   SELECT LAST_INSERT_ID();";
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(insert, connection);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@reminderText", string.IsNullOrWhiteSpace(reminderText) ? (object)DBNull.Value : reminderText);

                object result = cmd.ExecuteScalar();
                IsAvailable = true;
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                LastError = ex.Message;
                return -1;
            }
        }

        // ── Retrieve all tasks, most recent first ───────────────────────
        public List<TaskItem> GetAllTasks()
        {
            var results = new List<TaskItem>();
            try
            {
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
                connection.Open();
                string select = "SELECT Id, Title, Description, ReminderText, IsCompleted, CreatedAt FROM tasks ORDER BY CreatedAt DESC;";
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(select, connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new TaskItem
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                        ReminderText = reader.IsDBNull(reader.GetOrdinal("ReminderText")) ? null : reader.GetString("ReminderText"),
                        IsCompleted = reader.GetBoolean("IsCompleted"),
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    });
                }
                IsAvailable = true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                LastError = ex.Message;
            }
            return results;
        }

        // ── Find the most recent task whose title contains the given
        //    fragment (case-insensitive) — used so NLP commands can refer
        //    to a task by name instead of by numeric Id. ──────────────
        public TaskItem FindTaskByTitleFragment(string titleFragment)
        {
            try
            {
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
                connection.Open();
                string select = @"SELECT Id, Title, Description, ReminderText, IsCompleted, CreatedAt
                                   FROM tasks WHERE LOWER(Title) LIKE @fragment
                                   ORDER BY CreatedAt DESC LIMIT 1;";
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(select, connection);
                cmd.Parameters.AddWithValue("@fragment", $"%{titleFragment.ToLower()}%");
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    IsAvailable = true;
                    return new TaskItem
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                        ReminderText = reader.IsDBNull(reader.GetOrdinal("ReminderText")) ? null : reader.GetString("ReminderText"),
                        IsCompleted = reader.GetBoolean("IsCompleted"),
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    };
                }
                IsAvailable = true;
                return null;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                LastError = ex.Message;
                return null;
            }
        }

        // ── Set/update the reminder text for a task ─────────────────────
        public bool SetReminder(int id, string reminderText)
        {
            return ExecuteUpdate("UPDATE tasks SET ReminderText = @reminderText WHERE Id = @id;",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@reminderText", reminderText);
                    cmd.Parameters.AddWithValue("@id", id);
                });
        }

        // ── Mark a task as completed ─────────────────────────────────────
        public bool MarkCompleted(int id)
        {
            return ExecuteUpdate("UPDATE tasks SET IsCompleted = 1 WHERE Id = @id;",
                cmd => cmd.Parameters.AddWithValue("@id", id));
        }

        // ── Delete a task ─────────────────────────────────────────────────
        public bool DeleteTask(int id)
        {
            return ExecuteUpdate("DELETE FROM tasks WHERE Id = @id;",
                cmd => cmd.Parameters.AddWithValue("@id", id));
        }

        // ── Shared helper for simple parametrised UPDATE/DELETE queries ──
        private bool ExecuteUpdate(string sql, Action<MySql.Data.MySqlClient.MySqlCommand> addParameters)
        {
            try
            {
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, connection);
                addParameters(cmd);
                int rowsAffected = cmd.ExecuteNonQuery();
                IsAvailable = true;
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                LastError = ex.Message;
                return false;
            }
        }
    }
}