using BarcodeScanner.Mobile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UMAttendanceSystem_Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddBarcodeScannerHandler();
                });

            builder.Services.AddSingleton<DatabaseHelper>(sp =>
                new DatabaseHelper(App.ConnectionString));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}