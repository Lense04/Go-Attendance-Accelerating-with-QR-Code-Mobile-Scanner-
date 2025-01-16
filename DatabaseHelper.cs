using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace UMAttendanceSystem_Mobile
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<List<AttendanceEvent>> GetEventsAsync()
        {
            var events = new List<AttendanceEvent>();
            const string query = "SELECT EventId, EventName FROM event.Event_list";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                events.Add(new AttendanceEvent
                                {
                                    EventId = reader["EventId"].ToString(),
                                    EventName = reader["EventName"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("An error occurred while retrieving events from the database.", ex);
            }

            return events;
        }

        public async Task AddAttendanceAsync(string studentNumber, string eventId, DateTime timeAttendance, string studentName, string department)
        {
            const string eventQuery = "SELECT StartDate, EndDate, StartTime, EndTime, Departments FROM event.Event_list WHERE EventId = @EventId";
            const string checkQuery = "SELECT COUNT(*) FROM event.Attendance_{0} WHERE Student_Number = @Student_Number";
            const string insertQuery = "INSERT INTO event.Attendance_{0} (Student_Number, Student_Name, Department, Attendance_Time) VALUES (@Student_Number, @Name, @Department, @Time_Attendance)";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    EventDetails eventDetails = await GetEventDetailsAsync(eventId, connection, eventQuery);
                    if (eventDetails == null)
                    {
                        throw new Exception($"Event with ID {eventId} does not exist.");
                    }

                    if (!IsAttendanceWithinEventTime(timeAttendance, eventDetails))
                    {
                        throw new Exception($"Current time {timeAttendance} is not within the event time range.");
                    }

                    if (!IsDepartmentAllowed(eventDetails.Departments, department))
                    {
                        throw new Exception($"Student's department {department} is not allowed for this event.");
                    }

                    if (await IsAttendanceAlreadyRecordedAsync(studentNumber, eventId, connection, checkQuery))
                    {
                        throw new Exception($"Attendance for student {studentNumber} already exists for event {eventId}. Skipping addition.");
                    }

                    await InsertAttendanceRecordAsync(studentNumber, studentName, department, timeAttendance, eventId, connection, insertQuery);
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"An error occurred while adding attendance to the database. Error: {ex.Message}", ex);
            }
        }

        private async Task<EventDetails> GetEventDetailsAsync(string eventId, SqlConnection connection, string query)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventId", eventId);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new EventDetails
                        {
                            StartDate = (DateTime)reader["StartDate"],
                            EndDate = (DateTime)reader["EndDate"],
                            StartTime = (TimeSpan)reader["StartTime"],
                            EndTime = (TimeSpan)reader["EndTime"],
                            Departments = reader["Departments"].ToString().Split(',')
                        };
                    }
                }
            }
            return null;
        }

        private bool IsAttendanceWithinEventTime(DateTime timeAttendance, EventDetails eventDetails)
        {
            DateTime eventStartDateTime = eventDetails.StartDate.Add(eventDetails.StartTime);
            DateTime eventEndDateTime = eventDetails.EndDate.Add(eventDetails.EndTime);
            return timeAttendance >= eventStartDateTime && timeAttendance <= eventEndDateTime;
        }

        private bool IsDepartmentAllowed(string[] allowedDepartments, string department)
        {
            return Array.Exists(allowedDepartments, d => d.Trim().Equals(department, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> IsAttendanceAlreadyRecordedAsync(string studentNumber, string eventId, SqlConnection connection, string query)
        {
            using (SqlCommand command = new SqlCommand(string.Format(query, eventId), connection))
            {
                command.Parameters.AddWithValue("@Student_Number", studentNumber);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        private async Task InsertAttendanceRecordAsync(string studentNumber, string studentName, string department, DateTime timeAttendance, string eventId, SqlConnection connection, string query)
        {
            using (SqlCommand command = new SqlCommand(string.Format(query, eventId), connection))
            {
                command.Parameters.AddWithValue("@Student_Number", studentNumber);
                command.Parameters.AddWithValue("@Name", studentName);
                command.Parameters.AddWithValue("@Department", department);
                command.Parameters.AddWithValue("@Time_Attendance", timeAttendance);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public class AttendanceEvent
    {
        public string EventId { get; set; }
        public string EventName { get; set; }

        public override string ToString() => EventName;
    }

    public class EventDetails
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string[] Departments { get; set; }
    }
}