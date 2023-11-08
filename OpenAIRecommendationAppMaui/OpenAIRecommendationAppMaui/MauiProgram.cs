using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAIRecommendationAppMaui.Services;
using System.Reflection;

namespace OpenAIRecommendationAppMaui
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
                });

            builder.Services.AddTransient<MainPage>();

            OpenAIService svc = new OpenAIService();
            svc.Initialize(Constants.OpenAIKey, Constants.OpenAIEndpoint);
            builder.Services.AddSingleton<OpenAIService>(svc);
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}