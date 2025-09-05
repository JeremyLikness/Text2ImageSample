using Microsoft.Extensions.AI;

namespace Text2ImageSample
{
    public class ImageContext
    {
        public Func<IChatClient> ChatClientFactory { get; set; } = () => throw new InvalidOperationException("ChatClientFactory is not initialized.");
        public Func<IImageGenerator> ImageGeneratorFactory { get; set; } = () => throw new InvalidOperationException("ImageGeneratorFactory is not initialized.");

        public string[] GenreList { get; set; } = [];
        public string Genre { get; set; } = string.Empty;
        public string SceneDescription { get; set; } = string.Empty;
        public string CharacterDescription { get; set; } = string.Empty;
        public DataContent? Scene { get; set; } = null;
        public DataContent? Character { get; set; } = null;
        public DataContent? ModifiedCharacter { get; set; } = null;
        public DataContent[] FinalImages { get; set; } = [];
        public byte[]? CharacterBytes { get; set; } = [];
    }
}
