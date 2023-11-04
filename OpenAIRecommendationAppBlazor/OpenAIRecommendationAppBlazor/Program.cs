using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenAIRecommendationAppBlazor;
using OpenAIRecommendationAppBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Set up the OpenAI client
OpenAIService svc = new OpenAIService();
builder.Services.AddSingleton<OpenAIService>(svc);

var app = builder.Build();
var config = builder.Configuration;
var openAIKey = config["OpenAIKey"]!;
var openAIEndpoint = config["OpenAIEndpoint"];
svc.Initialize(openAIKey, openAIEndpoint); 
await app.RunAsync();
