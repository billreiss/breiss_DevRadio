This sample is inspired by Alvin Ashcraft's sample here [Tutorial--Create a recommendation app with .NET MAUI and ChatGPT - Windows apps | Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-dotnet-maui/tutorial-maui-ai), but I swapped out the OpenAI library he used with the official Microsoft Azure OpenAI package. Despite its name, it can hit OpenAI.com, doesn't have to hit Azure. So all you need is an OpenAI account and an API key. I also ported it to Blazor.

If you want to recreate this sample yourself, follow the following steps:

Create a new Blazor WebAssembly project. Name it `OpenAIRecommendationAppBlazor`. 

Add an `appsettings.json`file to the `wwwroot` of the project. This will automatically be picked up by Blazor. The content should look like this. A null endpoint will hit openai.com, if you have an Azure OpenAI instance put the endpoint here.

```
{
  "OpenAIKey": "my-openai-key",
  "OpenAIEndpoint": null
}
```

> Important Note: For production, you will want to protect your OpenAI API Key, the appsettings.json is not secure and your key can be easily retrieved. Implement a web service to retrieve the key, and then if you wish to store it, use encryption.

Install the Azure.AI.OpenAI package. it's in beta so you need to include prerelease. 

> Important Note: Since this library is in beta, the API is still evolving. This sample is currently expecting version 1.0.0-beta.8. At this time, there is a beta 9 that has breaking API changes. It also seems to have an issue with non-chat models currently and the response is truncated. I'll update this sample when there is a viable new beta or official release.

Add a Services folder, and create the following `OpenAIService` class:

```
using Azure.AI.OpenAI;
using Azure;

namespace OpenAIRecommendationAppBlazor.Services;
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

There are two methods here to call into OpenAI, one if you want to interact with the non-chat DaVinci model or the chat based GPT 3.5 model. Choose your model based on your need, one of these or a different one, the chat based model is much slower but will provide better results. If you wanted to use chat in a more interactive way, build up your list of chat history as chat messages, and hold on to it so you can pass it back in to the API. In this sample we are starting a new conversation every time. Some models are only available through the chat interface, like the GPT 3.5 model.

We'll register and register the OpenAI service we just created. This is done in `Program.cs`.

Replace the builder.Build().RunAsync call with the following, the config isn't available until the builder is built, so we register the service then initialize it based on the config once it's available.

```
// Set up the OpenAI client
OpenAIService svc = new OpenAIService();
builder.Services.AddSingleton<OpenAIService>(svc);

var app = builder.Build();
var config = builder.Configuration;
var openAIKey = config["OpenAIKey"]!;
var openAIEndpoint = config["OpenAIEndpoint"];
svc.Initialize(openAIKey, openAIEndpoint); 
await app.RunAsync();
```

Now that we have our helper service registered, let's replace the contents of `Index.razor` with the following:

```
@page "/"
@using OpenAIRecommendationAppBlazor.Services;
@using System.ComponentModel.DataAnnotations;
@inject OpenAIService openAIService;

<PageTitle>Index</PageTitle>

<h1>Local AI Recommendations</h1>

<EditForm OnValidSubmit="OnSubmit" Model="@recommendation">
    <DataAnnotationsValidator />
    <ValidationSummary />
    <div>City</div>
    <InputText @bind-Value="@recommendation.City"></InputText>
    <div>I'd like to have a recommendation for'</div>
    <InputSelect @bind-Value="@recommendation.RecommendationType">
        <option value="">--Select--</option>
        <option value="restaurant">Restaurants</option>
        <option value="hotel">Hotels</option>
        <option value="attractions">Attractions</option>
    </InputSelect>
    <div style="margin: 5px 0px 5px 0px">
        <button type="submit" class="btn btn-success">
            Submit
        </button>
    </div>
</EditForm>
<div>@responseText</div>
@code
{
    Recommendation recommendation = new Recommendation();
    string responseText = "";

    async void OnSubmit()
    {
        responseText = "Working on it...This can take a little while based on the model selected.";
        var message = await openAIService.CallOpenAI(recommendation.RecommendationType, recommendation.City);
        responseText = message;
        this.StateHasChanged();
    }

    public class Recommendation
    {
        [Required]
        public string City { get; set; } = "";
        [Required]
        public string RecommendationType { get; set; } = "";
    }
}
```

We are receiving the AI helper service via Dependency Injection then calling into it on the submission of the form. You can change the method call from CallOpenAI to CallOpenAIChat if you want to use the ChatGPT 3.5 model.


