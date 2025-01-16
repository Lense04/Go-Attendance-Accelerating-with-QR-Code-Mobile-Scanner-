using Microsoft.Maui.Controls;

namespace UMAttendanceSystem_Mobile
{
    public partial class App : Application
    {
        public static string ConnectionString { get; } = "Server = 34.126.135.52,1433;Database=finalproject;User Id = sqlserver; Password=admin;TrustServerCertificate=True;";

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage());
        }
    }
}