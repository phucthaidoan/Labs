using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace TalkToPdf.Qdrant;

/// <summary>
/// Main service class for the application.
/// </summary>
/// <typeparam name="TKey">The type of the data model key.</typeparam>
/// <param name="dataLoader">Used to load data into the vector store.</param>
/// <param name="vectorStoreTextSearch">Used to search the vector store.</param>
/// <param name="kernel">Used to make requests to the LLM.</param>
/// <param name="ragConfigOptions">The configuration options for the application.</param>
/// <param name="appShutdownCancellationTokenSource">Used to gracefully shut down the entire application when cancelled.</param>
internal sealed class RAGChatService(
    IDataLoader dataLoader,
    VectorStoreTextSearch<TextSnippet<Guid>> vectorStoreTextSearch,
    Kernel kernel)
{
    private string pdfFilePath = ""; // TODO pdf file path

    public async Task ChatLoopAsync()
    {
        Console.WriteLine("PDF loading complete\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Assistant > Press enter with no prompt to exit.");

        // Add a search plugin to the kernel which we will use in the template below
        // to do a vector search for related information to the user query.
        kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

        // Prompt the user for a question.
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Assistant > What would you like to know from the loaded PDFs: ({pdfFilePath})?");

        // Read the user question.
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("User > ");
        var question = Console.ReadLine();

        // Invoke the LLM with a template that uses the search plugin to
        // 1. get related information to the user query from the vector store
        // 2. add the information to the LLM prompt.
        var response = kernel.InvokePromptStreamingAsync(
            promptTemplate: """
                    Please use this information to answer the question:
                    {{#with (SearchPlugin-GetTextSearchResults question)}}  
                      {{#each this}}  
                        Name: {{Name}}
                        Value: {{Value}}
                        Link: {{Link}}
                        -----------------
                      {{/each}}
                    {{/with}}

                    Include citations to the relevant information where it is referenced in the response.
                    
                    Question: {{question}}
                    """,
            arguments: new KernelArguments()
            {
                    { "question", question },
            },
            templateFormat: "handlebars",
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());

        // Stream the LLM response to the console with error handling.
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("\nAssistant > ");

        try
        {
            await foreach (var message in response.ConfigureAwait(false))
            {
                Console.Write(message);
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Call to LLM failed with error: {ex}");
        }
    }

    public async Task LoadDataAsync()
    {

        var DataLoadingBatchSize = 10;
        var DataLoadingBetweenBatchDelayInMilliseconds = 1000;
        try
        {
            Console.WriteLine($"Loading PDF into vector store: {pdfFilePath}");
            await dataLoader.LoadPdf(
                pdfFilePath,
                DataLoadingBatchSize,
                DataLoadingBetweenBatchDelayInMilliseconds,
                default);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load PDFs: {ex}");
            throw;
        }
    }
}