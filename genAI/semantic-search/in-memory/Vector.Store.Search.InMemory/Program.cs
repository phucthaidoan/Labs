using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI;
using System.ClientModel;
using static MyNamespace;

// Create a chat completion service with a model from GitHub Models
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
var modelId = "openai/text-embedding-3-small";
var uri = "https://models.github.ai/inference";
var githubPAT = config["GH_PAT"];

// Create an embedding generation service.
var embeddingGenerator = new OpenAIClient(new ApiKeyCredential(githubPAT), new OpenAIClientOptions { Endpoint = new Uri(uri) })
    .GetEmbeddingClient(modelId)
    .AsIEmbeddingGenerator(1536);

// Construct an InMemory vector store.
// option 1: create directly
//var vectorStore = new InMemoryVectorStore();

// option 2: create via DI
var kernelBuilder = Kernel.CreateBuilder();
// Add the in-memory vector store to DI
kernelBuilder.Services.AddInMemoryVectorStore();
var kernel = kernelBuilder.Build();
var vectorStore = kernel.Services.GetRequiredService<InMemoryVectorStore>();

// Get and create collection if it doesn't exist.
var collection = vectorStore.GetCollection<ulong, Glossary>("skglossary");
await collection.EnsureCollectionExistsAsync();

// Create glossary entries and generate embeddings for them.
var glossaryEntries = CreateGlossaryEntries().ToList();
var tasks = glossaryEntries.Select(entry => Task.Run(async () =>
{
    entry.DefinitionEmbedding = (await embeddingGenerator.GenerateAsync(entry.Definition)).Vector;
}));
await Task.WhenAll(tasks);

// Upsert the glossary entries into the collection and return their keys.
await collection.UpsertAsync(glossaryEntries);

// Search the collection using a vector search.
var searchString = "What is an Application Programming Interface";
var searchVector = (await embeddingGenerator.GenerateAsync(searchString)).Vector;
var resultRecords = collection.SearchAsync(searchVector, top: 1);

await foreach (var resultRecord in resultRecords)
{
    Console.WriteLine("Search string: " + searchString);
    Console.WriteLine("Result: " + resultRecord.Record.Definition);
    Console.WriteLine();
}

// Search the collection using a vector search.
searchString = "What is Retrieval Augmented Generation";
searchVector = (await embeddingGenerator.GenerateAsync(searchString)).Vector;
resultRecords = collection.SearchAsync(searchVector, top: 3);

await foreach (var resultRecord in resultRecords)
{
    Console.WriteLine("Search string: " + searchString);
    Console.WriteLine("Result: " + resultRecord.Record.Definition);
    Console.WriteLine();
}

// Search the collection using a vector search with pre-filtering.
searchString = "What is Retrieval Augmented Generation";
searchVector = (await embeddingGenerator.GenerateAsync(searchString)).Vector;
resultRecords = collection.SearchAsync(
    searchVector,
    top: 3,
    options: new() { Filter = g => g.Category == "External Definitions" });

await foreach (var resultRecord in resultRecords)
{
    Console.WriteLine("-----------------------------------------");
    Console.WriteLine("Search string: " + searchString);
    Console.WriteLine("Result Score: " + resultRecord.Score);
    Console.WriteLine("Result : " + resultRecord.Record.Definition);
    Console.WriteLine("-----------------------------------------");
}
