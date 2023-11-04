Calling OpenAI from .NET MAUI
=============================

.NET MAUI is a framework to create cross platform .NET applications for Windows, Mac, iOS, and Android among others. It is the successor of Xamarin Forms.

This sample is based on Alvin Ashcraft's sample here [Tutorial--Create a recommendation app with .NET MAUI and ChatGPT - Windows apps | Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-dotnet-maui/tutorial-maui-ai), but I swapped out the OpenAI library he used with the official Microsoft Azure OpenAI package. Despite its name, it can hit OpenAI.com, doesn't have to hit Azure. So all you need is an OpenAI account and an API key.

If you want to recreate this sample yourself, follow the following steps:

Create a new MAUI App project. Name it `OpenAIRecommendationAppMaui`. 

Add an `appsettings.json`file to the root of the project, set the build action to Embedded Resource. The content should look like this. A null endpoint will hit openai.com, if you have an Azure OpenAI instance put the endpoint here.

```
{
  "OpenAIKey": "my-openai-key",
  "OpenAIEndpoint": null
}
```

> Important Note: For production, you will want to protect your OpenAI API Key, the appsettings.json is not secure and your key can be easily retrieved. Implement a web service to retrieve the key, and then if you wish to store it, use encryption such as Secure String.

Install NuGet packages `Microsoft.Extensions.Configuration.Json` and `Microsoft.Extensions.Configuration.Binder`. This is so we can consume the `appsettings.json` file.

Install the `Azure.AI.OpenAI` package. it's in beta so you need to include prerelease. 

Add a Services folder, and create the following `OpenAIService` class:

```
using Azure.AI.OpenAI;
using Azure;

namespace OpenAIRecommendationAppMaui.Services;
public class OpenAIService
{
    OpenAIClient client;
    static readonly char[] trimChars = new char[] { '\n', '?' };
    public void Initialize(string openAIKey, string? openAIEndpoint = null)
    {
        client = !string.IsNullOrWhiteSpace(openAIEndpoint)
            ? new OpenAIClient(
                new Uri(openAIEndpoint),
                new AzureKeyCredential(openAIKey))
            : new OpenAIClient(openAIKey);
    }


    internal async Task<string> CallOpenAI(string recommendationType, string location)
    {
        string prompt = GeneratePrompt(recommendationType, location);
        Response<Completions> response = await client.GetCompletionsAsync(
            "text-davinci-003", // assumes a matching model deployment or model name
            prompt);
        StringWriter sw = new StringWriter();
        foreach (Choice choice in response.Value.Choices)
        {
            var text = choice.Text.TrimStart(trimChars);
            sw.WriteLine(text);
        }
        var message = sw.ToString();
        return message;
    }

    internal async Task<string> CallOpenAIChat(string recommendationType, string location)
    {
        string prompt = GeneratePrompt(recommendationType, location);
        ChatCompletionsOptions options = new ChatCompletionsOptions();
        options.ChoiceCount = 1;
        options.Messages.Add(new ChatMessage(ChatRole.User, prompt));
        var response = await client.GetChatCompletionsAsync(
            "gpt-3.5-turbo-16k", // assumes a matching model deployment or model name
            options);
        StringWriter sw = new StringWriter();
        foreach (ChatChoice choice in response.Value.Choices)
        {
            var text = choice.Message.Content.TrimStart(trimChars);
            sw.WriteLine(text);
        }
        var message = sw.ToString();
        return message;
    }

    private static string GeneratePrompt(string recommendationType, string location)
    {
        return $"What is a recommended {recommendationType} near {location}";
    }
}
```

So there are two methods here to call into OpenAI, one if you want to interact with the non-chat DaVinci model or the chat based GPT 3.5 model. Choose your model based on your need, one of these or a different one, the chat based model is much slower but will provide better results. If you wanted to use chat in a more interactive way, build up your list of chat history as chat messages, and hold on to it so you can pass it back in to the API. In this sample we are starting a new conversation every time. Some models are only available through the chat interface, like the GPT 3.5 model.

We'll register `MainPage`, retrieve the `appsettings.json`, and register the OpenAI service we just created. This is done in `MauiProgram.cs`.

Add the following right after the builder calls into UseMauiApp etc:

```
            builder.Services.AddTransient<MainPage>();
            // add appsettings.json to the Config
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenAIRecommendationAppMaui.appsettings.json");
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);

            // Set up the OpenAI client
            var openAIKey = config["OpenAIKey"];
            var openAIEndpoint = config["OpenAIEndpoint"];
            OpenAIService svc = new OpenAIService(openAIKey, openAIEndpoint);
            builder.Services.AddSingleton<OpenAIService>(svc);
```

If your project name is different, you'll need to change the location of the `appsettings.json` file.

Now that we have our helper service registered, and the `MainPage` is registered, we can do dependency injection to import the service into `MainPage`. Update `MainPage.xaml.cs` as follows:

```
        OpenAIService openAIService;
        public MainPage(OpenAIService svc)
        {
            openAIService = svc;
            InitializeComponent();
        }
```

Now we have an instance of the service, so let's build out the page to use it. In `MainPage.xaml`, replace the contents of the `VerticalStackLayout` with this:

```
            <Label
                Text="Local AI recommendations"
                SemanticProperties.HeadingLevel="Level1"
                FontSize="32"
                HorizontalOptions="Center" />
            <Entry
                x:Name="LocationEntry"
                Placeholder="Enter your location"
                SemanticProperties.Hint="Enter the location for recommendations"
                HorizontalOptions="Center"/>
            <Button
                x:Name="RestaurantBtn"
                Text="Get restaurant recommendations"
                SemanticProperties.Hint="Gets restaurant recommendations when you click"
                Clicked="OnRestaurantClicked"
                HorizontalOptions="Center" />

            <Button
                x:Name="HotelBtn"
                Text="Get hotel recommendations"
                SemanticProperties.Hint="Gets hotel recommendations when you click"
                Clicked="OnHotelClicked"
                HorizontalOptions="Center" />

            <Button
                x:Name="AttractionBtn"
                Text="Get attraction recommendations"
                SemanticProperties.Hint="Gets attraction recommendations when you click"
                Clicked="OnAttractionClicked"
                HorizontalOptions="Center" />

            <Label x:Name="SmallLabel"
                Text="Click a button for recommendations!"
                SemanticProperties.HeadingLevel="Level2"
                FontSize="18"
                HorizontalOptions="Center" />
```

In the `MainPage.xaml.cs` we need to wire up event handlers for these buttons and call OpenAI. This is what the file should look like after those changes:

```
using OpenAIRecommendationAppMaui.Services;

namespace OpenAIRecommendationAppMaui
{
    public partial class MainPage : ContentPage
    {
        OpenAIService openAIService;
        public MainPage(OpenAIService svc)
        {
            openAIService = svc;
            InitializeComponent();
        }
        private async void OnRestaurantClicked(object sender, EventArgs e)
        {
            await GetRecommendation("restaurant");
        }

        private async void OnHotelClicked(object sender, EventArgs e)
        {
            await GetRecommendation("hotel");
        }

        private async void OnAttractionClicked(object sender, EventArgs e)
        {
            await GetRecommendation("attraction");
        }

        private async Task GetRecommendation(string recommendationType)
        {
            if (string.IsNullOrWhiteSpace(LocationEntry.Text))
            {
                await DisplayAlert("Empty location", "Please enter a location (city or postal code)", "OK");
                return;
            }
            SmallLabel.Text = "Working on it...This can take a little while based on the model selected.";
            var message = await openAIService.CallOpenAI(recommendationType, LocationEntry.Text);
            SmallLabel.Text = message;
        }
    }

}
```

You can change the method call from CallOpenAI to CallOpenAIChat if you want to use the ChatGPT 3.5 model. 
