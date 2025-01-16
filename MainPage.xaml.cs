using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using BarcodeScanner.Mobile;

namespace UMAttendanceSystem_Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseHelper _databaseHelper;
        private LinkedList<string> _scannedValues;
        private string _selectedEventId;

        public MainPage()
        {
            InitializeComponent();
            _databaseHelper = new DatabaseHelper(App.ConnectionString);
            _scannedValues = new LinkedList<string>();
            LoadEventsAsync(); // Call the async method to load events
        }

        private async void LoadEventsAsync()
        {
            var events = await _databaseHelper.GetEventsAsync();
            EventPicker.ItemsSource = events; // Set the ItemsSource for the Picker
            EventPicker.ItemDisplayBinding = new Binding("EventName"); // Display the EventName in the Picker
        }

        private void OnEventSelected(object sender, EventArgs e)
        {
            if (EventPicker.SelectedItem is AttendanceEvent selectedEvent)
            {
                _selectedEventId = selectedEvent.EventId; // Set the selected event ID
                AddAttendanceButton.IsEnabled = true; // Enable the Add Attendance button
                Console.WriteLine($"Selected Event: {selectedEvent.EventName}");
            }
        }

        private async void OnStartScanningClicked(object sender, EventArgs e)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                StartScanning();
            }
            else
            {
                await DisplayAlert("Permission Denied", "Camera permission is required to scan QR codes.", "OK");
            }
        }

        private void StartScanning()
        {
            if (Camera != null)
            {
                Camera.IsScanning = true;
            }
        }

        private async void Camera_OnDetected(object sender, OnDetectedEventArg e)
        {
            List<BarcodeResult> results = e.BarcodeResults;

            if (results != null && results.Count > 0)
            {
                string scannedInput = results[0].DisplayValue;
                _scannedValues.AddLast(scannedInput);
                BarcodeResult.Text = $"Latest: {scannedInput}";
                Camera.IsScanning = false; // Stop scanning after detecting

                var dataParts = scannedInput.Split('/');
                if (dataParts.Length == 4)
                {
                    string studentNumber = dataParts[0];
                    string studentName = dataParts[1];
                    string department = dataParts[2];
                    long timestamp = long.Parse(dataParts[3]);

                    if (IsLatestTimestamp(timestamp))
                    {
                        await DisplayAlert("Scanned Data", $"Student Number: {studentNumber}\nName: {studentName}\nDepartment: {department}", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "The scanned QR code is outdated.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Scanned data is not in the expected format.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Result", "No barcodes detected.", "OK");
            }
        }

        private bool IsLatestTimestamp(long scannedTimestamp)
        {
            long currentTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            long roundedCurrentTimestamp = GetRoundedTimestamp();
            return scannedTimestamp >= roundedCurrentTimestamp;
        }

        private long GetRoundedTimestamp()
        {
            DateTime now = DateTime.Now;
            int minutes = now.Minute;
            int roundedMinutes = (minutes / 5) * 5;
            DateTime roundedTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, roundedMinutes, 0, DateTimeKind.Local);
            return new DateTimeOffset(roundedTime).ToUnixTimeSeconds();
        }

        private async Task AddAttendanceRecordAsync(string studentNumber, string studentName, string department, long timestamp)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                await _databaseHelper.AddAttendanceAsync(studentNumber, _selectedEventId, currentTime, studentName, department);
                await DisplayAlert("Success", "Attendance recorded successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnAddAttendanceClicked(object sender, EventArgs e)
        {
            if (_scannedValues.Count > 0 && !string.IsNullOrEmpty(_selectedEventId))
            {
                foreach (var scannedValue in _scannedValues)
                {
                    var dataParts = scannedValue.Split('/');
                    if (dataParts.Length == 4)
                    {
                        string studentNumber = dataParts[0];
                        string studentName = dataParts[1];
                        string department = dataParts[2];
                        long timestamp = long.Parse(dataParts[3]);

                        if (IsLatestTimestamp(timestamp))
                        {
                            await AddAttendanceRecordAsync(studentNumber, studentName, department, timestamp);
                        }
                        else
                        {
                            await DisplayAlert("Error", $"The scanned QR code for {studentName} is outdated.", "OK");
                        }
                    }
                }
                _scannedValues.Clear();
            }
            else
            {
                await DisplayAlert("Error", "No scanned values available or event not selected.", "OK");
            }
        }
    }
}