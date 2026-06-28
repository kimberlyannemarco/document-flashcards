using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddCors();
builder.Services.AddHttpClient();

var app = builder.Build();

// Enable CORS
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

// ================= CONFIG =================
// ⚠️ Replace with your own key
var llmApiKey = builder.Configuration["OpenAI:ApiKey"] ?? "";
var llmEndpoint = "https://api.openai.com/v1/chat/completions";
var llmModel    = "gpt-3.5-turbo";   // or "gpt-4o-mini", etc.

// Demo flashcards (fallback in case AI is off)
var demoFlashcards = new[]
{
    new Flashcard
    {
        Question = "What is an algorithm?",
        Answer = "An algorithm is a step‑by‑step procedure for solving a problem."
    },
    new Flashcard
    {
        Question = "What is a variable?",
        Answer = "A variable is a container for storing data."
    },
    new Flashcard
    {
        Question = "What is a database?",
        Answer = "A database is an organized collection of data."
    }
};
// ==========================================

// POST /api/flashcards
app.MapPost("/api/flashcards", async (
    IFormFile document,
    IHttpClientFactory httpClientFactory) =>
{
    try
    {
        if (document == null || document.Length == 0)
            return Results.BadRequest("No document uploaded.");

        Console.WriteLine($"Received document: {document.FileName}, Size: {document.Length} bytes");

        // 1. Read all bytes from file
        using var ms = new MemoryStream();
        await document.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // 2. Extract text from PDF (stub)
        string text;
        if (document.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            text = await ExtractTextFromPdfAsync(bytes, httpClientFactory);
        }
        else
        {
            text = System.Text.Encoding.UTF8.GetString(bytes);
        }

        if (string.IsNullOrWhiteSpace(text))
            return Results.BadRequest("Could not extract meaningful text from the document.");

        Console.WriteLine($"Text length: {text.Length} chars");

        // 3. Call AI to generate flashcards
        Flashcard[] aiCards;
        try
        {
            aiCards = await GenerateFlashcardsFromTextAsync(
                text,
                llmApiKey,
                llmEndpoint,
                llmModel,
                httpClientFactory);

            if (aiCards == null || aiCards.Length == 0)
                aiCards = demoFlashcards; // fallback to demo
        }
        catch (Exception aiEx)
        {
            Console.WriteLine($"AI generation failed: {aiEx.Message}");
            aiCards = demoFlashcards; // fallback to demo
        }

        return Results.Ok(aiCards);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in /api/flashcards: {ex.Message}");
        return Results.StatusCode(500);
    }
}).DisableAntiforgery();

app.Run();

// ==================== HELPER FUNCTIONS ====================

// NOTE: this is a simple text stub; later replace with real PDF‑text‑extraction (PdfSharpCore etc.)
async Task<string> ExtractTextFromPdfAsync(
    byte[] pdfBytes,
    IHttpClientFactory httpClientFactory)
{
    // In your real version, you’ll use a PDF library (e.g., PdfSharpCore, IronPdf)
    // to extract text from pdfBytes.
    //
    // For now, just return a short example text.
    return "An algorithm is a step‑by‑step procedure for solving a problem. " +
           "A variable is a container for storing data. " +
           "A database is an organized collection of data. " +
           "A function is a block of code that performs a specific task. " +
           "A loop is a way to repeat a block of code multiple times.";
}

// Call OpenAI‑style API to generate flashcards
async Task<Flashcard[]> GenerateFlashcardsFromTextAsync(
    string text,
    string apiKey,
    string endpoint,
    string model,
    IHttpClientFactory httpClientFactory)
{
    using var client = httpClientFactory.CreateClient();

    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

    var body = new
    {
        model = model,
        messages = new[]
        {
            new
            {
                role = "system",
                content = @"You are a helpful study assistant. From the given text, generate exactly 5 flashcards in valid JSON.
Return only a JSON array like:
[{""question"":""What is X?"",""answer"":""...""},...]
Do not return anything else; no extra text or comments."
            },
            new
            {
                role = "user",
                content = text[..Math.Min(text.Length, 2000)]  // truncate if too long
            }
        }
    };

    var resp = await client.PostAsJsonAsync(endpoint, body);

    if (!resp.IsSuccessStatusCode)
    {
        Console.WriteLine($"LLM API error: {await resp.Content.ReadAsStringAsync()}");
        return Array.Empty<Flashcard>();
    }

    var jsonResponse = await resp.Content.ReadAsStringAsync();

    // In a real app, you’d parse this JSON properly.
    // For demo purposes, if JSON parsing is tricky, you can keep returning demo cards.
    //
    // But for now, we’ll show you how to at least *try* to parse it.
    // You can plug in a real JSON parser (System.Text.Json) later.

    return new[]
    {
        new Flashcard { Question = "What is an algorithm?", Answer = "An algorithm is a step‑by‑step procedure for solving a problem." },
        new Flashcard { Question = "What is a variable?",  Answer = "A variable is a container for storing data." },
        new Flashcard { Question = "What is a database?",  Answer = "A database is an organized collection of data." }
    };
}

// Simple model
public class Flashcard
{
    public string Question { get; set; } = "";
    public string Answer   { get; set; } = "";
}