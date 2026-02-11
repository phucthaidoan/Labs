using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ClientModel;
using TalkToPdf.Qdrant;

var builder = Host.CreateApplicationBuilder(args);

// Configure configuration and load the application configuration.
var config = builder
    .Configuration
    .AddUserSecrets<Program>()
    .Build();

var modelId = "openai/gpt-4.1";
var embeddingModelId = "openai/text-embedding-3-small";
var uri = "https://models.github.ai/inference";
var githubPAT = config["GH_PAT"];

// Register the kernel with the dependency injection container
// and add Chat Completion and Text Embedding Generation services.
var kernelBuilder = builder
    .Services.
    AddKernel();

kernelBuilder
    .AddOpenAIChatCompletion(
            modelId,
            new OpenAIClient(new ApiKeyCredential(githubPAT), new OpenAIClientOptions { Endpoint = new Uri(uri) }));

var QdrantCollectionName = "my-pdf-collection";
var QdrantHost = "http://localhost/";
var QdrantPort = 6333;

kernelBuilder
    .Services
    .AddQdrantCollection<Guid, TextSnippet<Guid>>(
            QdrantCollectionName,
            QdrantHost,
            QdrantPort);

builder
    .Services
    .AddSingleton<IEmbeddingGenerator>(
            sp => new OpenAIClient(new ApiKeyCredential(githubPAT), new OpenAIClientOptions { Endpoint = new Uri(uri) })
                .GetEmbeddingClient(embeddingModelId)
                .AsIEmbeddingGenerator());

// Add a text search implementation that uses the registered vector store record collection for search.
kernelBuilder.AddVectorStoreTextSearch<TextSnippet<Guid>>();
builder.Services.AddSingleton<IDataLoader, DataLoader>();
builder.Services.AddSingleton<RAGChatService>();
// Build the host
var host = builder.Build();

// Get the kernel from the service provider
var kernel = host.Services.GetRequiredService<Kernel>();

await host.RunAsync();