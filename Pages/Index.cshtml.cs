using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApplication7.Pages
{
    public class IndexModel : PageModel
    {
        private const string SessionKey = "ChatHistory";

        [BindProperty]
        public string UserMessage { get; set; }

        public List<ChatMessage> ChatHistory { get; set; } = new();

        private const string AzureEndpoint = "https://pavanazureaiservice.openai.azure.com/";
        private const string AzureDeployment = "gpt-4o";
        private const string AzureApiKey = "5ih13U1tc7MwolmgwcNzEFVau3AUcwXvYSwWKrvsuk2Koden15TJJQQJ99BEACHYHv6XJ3w3AAAAACOGZ1cS";

        public void OnGet()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(json))
            {
                ChatHistory = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(json))
            {
                ChatHistory = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new();
            }

            if (!string.IsNullOrWhiteSpace(UserMessage))
            {
                ChatHistory.Add(new ChatMessage { Role = "User", Content = UserMessage });

                var response = await GetResponseFromLLMAsync(UserMessage, ChatHistory);

                ChatHistory.Add(new ChatMessage { Role = "Assistant", Content = response });

                HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(ChatHistory));
            }

            return RedirectToPage();
        }

        private async Task<string> GetResponseFromLLMAsync(string userMessage, List<ChatMessage> chatHistory)
        {
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: AzureDeployment,
                endpoint: AzureEndpoint,
                apiKey: AzureApiKey
            );

            var kernel = builder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var conversation = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

            foreach (var message in chatHistory)
            {
                var role = message.Role.Equals("User", System.StringComparison.OrdinalIgnoreCase)
                    ? Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User
                    : Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant;
                conversation.AddMessage(role, message.Content);
            }

            conversation.AddMessage(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User, userMessage);

            var result = await chatService.GetChatMessageContentAsync(
                conversation,
                kernel: kernel
            );

            return result?.Content ?? "No response from LLM.";
        }

        public class ChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }
    }
}
