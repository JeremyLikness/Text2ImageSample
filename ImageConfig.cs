
using Microsoft.Extensions.Configuration;

namespace Text2ImageSample
{
    public class ImageConfig(IConfiguration config)
    {
        private readonly Uri _endPoint = new (
            config["AzureOpenAI:EndPoint"] 
            ?? throw new InvalidOperationException("Azure OpenAI EndPoint is not configured."));

        private readonly string _promptModel = config["Text2Image:PromptModel"] 
            ?? "gpt-4o-mini";

        private readonly string _imageModel = config["Text2Image:ImageModel"]
            ?? "gpt-image-1";

        private readonly string _pathToCharacterImage = config["Text2Image:PathToCharacterImage"]
            ?? "jeremy.jpg";

        public Uri EndPoint => _endPoint;
        public string PromptModel => _promptModel;
        public string ImageModel => _imageModel;
        public string PathToCharacterImage => _pathToCharacterImage;
    }
}   
