using Microsoft.Extensions.AI;
using System.Text;

namespace Text2ImageSample
{
    public class Renderer(string baseDirectory)
    {
        public async Task<string> RenderAsync(ImageContext context)
        {
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            var sb = new StringBuilder($"<html><body><h1>AI Transformer - a {context.Genre} scene</h1>");
            sb.AppendLine("<h2>Character</h2>");
            var name = SaveImage(context.Character!, nameof(context.Character));
            sb.AppendLine($"<img src='{name}' alt='The character.'/>");
            sb.AppendLine($"<p>{context.CharacterDescription}</p>");
            sb.AppendLine("<h2>Scene</h2>");
            sb.AppendLine($"<p>{context.SceneDescription}</p>");
            var scene = SaveImage(context.Scene!, nameof(context.Scene));
            sb.AppendLine($"<img src='{scene}' alt='The scene.'/>");
            sb.AppendLine("<h2>Modified character</h2>");
            var modified = SaveImage(context.ModifiedCharacter!, nameof(context.ModifiedCharacter));
            sb.AppendLine($"<img src='{modified}' alt='The new character.'/>");
            sb.AppendLine("<h2>Sketches</h2>");
            var idx = 1;
            foreach (var img in context.FinalImages)
            {
                var imgPath = SaveImage(img, $"sketch{idx++}");
                sb.AppendLine($"<img src='{imgPath}' alt='Sketch'/>");
            }
            sb.AppendLine("</body></html>");
            var outputPath = Path.Combine(baseDirectory, "index.html");
            await File.WriteAllTextAsync(outputPath, sb.ToString());
            return outputPath;
        }

        private string SaveImage(DataContent content, string name)
        {
            var extension = content.MediaType.Split(@"/")[1];
            var path = Path.Combine(baseDirectory, $"{name}.{extension}");
            File.WriteAllBytes(path, content.Data.ToArray());
            return Path.GetFileName(path);
        }
    }
}
