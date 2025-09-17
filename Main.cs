using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Text2ImageSample
{
    public class Main(
        AgentProvider agent, 
        ImageConfig config,
        IHostApplicationLifetime hostApplicationLifetime) : IHostedService
    {
        private readonly ImageContext Context = new ();
        private readonly ConcurrentQueue<string> _messages = new ();
        private readonly AsyncTimer _timer = new();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Started hosted service.");
        
            var chat = agent.GetChatClient();
            var imageGen = agent.GetImageGenerator();
        
            Context.ChatClientFactory = () => chat!;
            Context.ImageGeneratorFactory = () => imageGen!;
       
            var job = Task.Run(
                async () => 
                await RunAsync(cancellationToken), cancellationToken);

            while (!job.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                _timer.Ping();
            
                if (!_messages.IsEmpty)
                {
                    Console.WriteLine();
                
                    while (_messages.TryDequeue(out string? msg) 
                        && !cancellationToken.IsCancellationRequested)
                    {
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            Console.WriteLine(msg);
                        }
                    }
                    
                }
            }

            //hostApplicationLifetime.StopApplication();
            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                foreach (var step in RunWorkflow(ct))
                {
                    _timer.NewTask();
                    await step;
                }
            }
            catch (OperationCanceledException)
            {
                _messages.Enqueue("Operation was cancelled.");
            }
            catch (Exception ex)
            {
                _messages.Enqueue($"Operation failed: {ex.Message}");
            }
            finally
            {
                _messages.Enqueue("Exiting...");
                hostApplicationLifetime.StopApplication();
            }
        }

        private IEnumerable<Task> RunWorkflow(CancellationToken ct)
        {
            yield return InitAsync(ct);
            yield return GetGenresAsync(ct);
            yield return ChooseGenreAsync(ct);
            yield return GeneratePromptAsync(ct);
            yield return GenerateSceneImageAsync(ct);
            yield return ModifyCharacterAsync(ct);
            yield return MergeSceneAndCharacterAsync(ct);
            yield return RenderAsync(ct);
            yield break;
        }

        private async Task InitAsync(CancellationToken ct)
        {
            _messages.Enqueue("Loading character model...");
            var imagePath = config.PathToCharacterImage;
            if (!File.Exists(imagePath))
            {
                imagePath = Path.Combine(Environment.CurrentDirectory, config.PathToCharacterImage);
            }

            var extension = Path.GetExtension(imagePath).ToLower();

            var type = extension switch
            {
                ".jpg" => "image/jpeg",
                ".gif" => "image/gif",
                ".png" => "image/png",
                _ => throw new InvalidOperationException($"Can't process extension ${extension}"),
            };

            Context.CharacterBytes = await File.ReadAllBytesAsync(imagePath, ct);
            Context.Character = new DataContent(Context.CharacterBytes, type);

            var getDescription = new ChatMessage(ChatRole.User, [Context.Character,
            new TextContent("Describe what you see.")]);
            Context.CharacterDescription = (await Context.ChatClientFactory()
                .GetResponseAsync(getDescription, cancellationToken: ct)).Text;
        }

        private async Task GetGenresAsync(CancellationToken ct)
        {
            _messages.Enqueue("Get the list of genres...");
            var chat = Context.ChatClientFactory();
            var response = await chat!.GetResponseAsync<string[]>(
                [
                new ChatMessage(
                    ChatRole.User, 
                    "Provide a list of 3 - 15 Genres for a story, one per line. Do not add commentary or provide an explanation - just the genres. Thank you!")], 
                cancellationToken: ct);
            Context.GenreList = [.. response.Result.Where(g => !string.IsNullOrWhiteSpace(g))];
            var genreList = string.Join(" / ", Context.GenreList);
            _messages.Enqueue($"Generated {Context.GenreList.Length} genres: {genreList}");
        }

        private Task ChooseGenreAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Context.Genre = Context.GenreList[new Random().Next(0, Context.GenreList.Length - 1)];
            _messages.Enqueue($"Chose genre: {Context.Genre}");
            return Task.CompletedTask;
        }

        private async Task GeneratePromptAsync(CancellationToken ct)
        {
            var promptForPrompt = $"Describe a location that would exist in a {Context.Genre} story. The location should be as detailed and imaginative as possible, make it extraordinary and design it as if the entire book had to take place there. Do not describe any individuals or persons, only the physical location itself. Wildlife and animals are fine to include.";
            var promptForImage = (await Context.ChatClientFactory().GetResponseAsync(promptForPrompt, cancellationToken: ct)).Text;
            Context.SceneDescription = promptForImage;
        }

        private async Task GenerateSceneImageAsync(CancellationToken ct)
        {
            _messages.Enqueue("Generating image...");
            var imageResponse = await Context.ImageGeneratorFactory().GenerateImagesAsync(Context.SceneDescription, cancellationToken: ct);
            Context.Scene = imageResponse.Contents.OfType<DataContent>().First();
        }

        private async Task ModifyCharacterAsync(CancellationToken ct)
        {
            _messages.Enqueue("Modifying character...");
            var variation = await Context.ImageGeneratorFactory().EditImageAsync(
                Context.Character!,
                $"The source image should contain a person. Create a new image that transforms the person into a character in a {Context.Genre} novel. They should be doing something interesting/productive in this location: {Context.SceneDescription}", cancellationToken: ct); 
            Context.ModifiedCharacter = variation.Contents.OfType<DataContent>().First();
        }

        public async Task MergeSceneAndCharacterAsync(CancellationToken ct)
        {
            _messages.Enqueue("Performing final transformation...");
            var finalImageReponse = await Context.ImageGeneratorFactory()
                .EditImagesAsync([Context.ModifiedCharacter!, Context.Scene!], 
                "Render these images as black and white sketches.", cancellationToken: ct);
            Context.FinalImages = [.. finalImageReponse.Contents.OfType<DataContent>()];
        }

        public async Task RenderAsync(CancellationToken ct)
        {
            _messages.Enqueue("Rendering final results...");
            var outputDirectory = 
                Path.Combine(Environment.CurrentDirectory, $"output-{DateTime.Now.Ticks}");
            Directory.CreateDirectory(outputDirectory);
            var renderer = new Renderer(outputDirectory);
            var index = await renderer.RenderAsync(Context);
            _messages.Enqueue($"Output written to {outputDirectory}");
            _messages.Enqueue("Completed image generation.");
            Process.Start(new ProcessStartInfo
            {
                FileName = index,
                UseShellExecute = true // Ensures the default browser is used
            });

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine("Stopped hosted service.");
            return Task.CompletedTask;
        }
    }
}
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

