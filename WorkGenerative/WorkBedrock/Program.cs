using System.Diagnostics;

using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

#pragma warning disable BedrockRuntime1002 // Property value does not match required pattern
const string ModelId = "jp.anthropic.claude-sonnet-4-5-20250929-v1:0";

var client = new AmazonBedrockRuntimeClient();

var request = new ConverseStreamRequest
{
    ModelId = ModelId,
    Messages =
    [
        new Message
        {
            Role = ConversationRole.User,
            Content =
            [
                new ContentBlock { Text = "C#のどのバージョンまでの知識がありますか？" }
            ]
        }
    ],
    InferenceConfig = new InferenceConfiguration
    {
        MaxTokens = 2048
    }
};
#pragma warning restore BedrockRuntime1002 // Property value does not match required pattern

var response = await client.ConverseStreamAsync(request);
foreach (var chunk in response.Stream.AsEnumerable().OfType<ContentBlockDeltaEvent>())
{
    Console.Write(chunk.Delta.Text);
}

Console.ReadLine();
