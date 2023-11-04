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

            // add appsettings.json to the Config
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenAIRecommendationAppMaui.appsettings.json");
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);

            // Set up the OpenAI client
            var openAIKey = config["OpenAIKey"];
            var openAIEndpoint = config["OpenAIEndpoint"];
            OpenAIService svc = new OpenAIService();
            svc.Initialize(openAIKey, openAIEndpoint);
            builder.Services.AddSingleton<OpenAIService>(svc);
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}