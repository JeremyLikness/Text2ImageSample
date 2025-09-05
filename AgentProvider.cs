using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace Text2ImageSample
{
    public class AgentProvider
    {
        private readonly IChatClient _chatClient;
        private readonly IImageGenerator _imageGenerator;

        public AgentProvider(ImageConfig config)
        {

            var openai = new AzureOpenAIClient(
                config.EndPoint, 
                new DefaultAzureCredential());
            var client = openai.GetChatClient(config.PromptModel);
            _chatClient = client.AsIChatClient();
            _imageGenerator = openai.GetImageClient(config.ImageModel).AsIImageGenerator();
        }

        public IChatClient? GetChatClient() => _chatClient;

        public IImageGenerator? GetImageGenerator() => _imageGenerator;
    }
}