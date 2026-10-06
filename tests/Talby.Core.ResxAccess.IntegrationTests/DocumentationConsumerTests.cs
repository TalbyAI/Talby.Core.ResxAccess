using System.Text.RegularExpressions;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class DocumentationConsumerTests
{
    [Fact]
    public async Task CompilesAndInvokesConsumerGuideExamples()
    {
        var guide = File.ReadAllText(
            Path.Combine(ConsumerProject.FindRepository(), "docs/resource-access.md")
        );
        using var consumer = new ConsumerProject(ReadExample(guide, "Program.cs", "csharp"));
        consumer.Write("Texts.cs", ReadExample(guide, "Texts.cs", "csharp"));
        consumer.Write("Resources/Labels.resx", ReadExample(guide, "Resources/Labels.resx", "xml"));
        consumer.Write(
            "Resources/Labels.es.resx",
            ReadExample(guide, "Resources/Labels.es.resx", "xml")
        );

        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        var run = await ConsumerProject.Invoke(
            Path.Combine(consumer.DirectoryPath, "bin/Release/net10.0/Consumer.dll")
        );
        Assert.True(run.ExitCode == 0, run.Output);
        Assert.Equal(
            new[]
            {
                "Hello",
                "Hola",
                "Hello",
                "Hello {name@string}, {amount@decimal:N2}!",
                "Hola {name}, {amount:N2}!",
                "Hello Ada, 12.50!",
                "Hola Ada, 12.50!",
                "Hola Ada, 12,50!",
                "second / 12.50",
                "Ada second first Ada",
                "first Ada second Ada",
                "{  12.5} [     ]",
                "Optional []",
                "Dash text",
                "Keyword",
            },
            run.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        );
    }

    private static string ReadExample(string guide, string title, string language)
    {
        var example = Regex.Match(
            guide,
            $"(?s)### {Regex.Escape(title)}\\r?\\n.*?```{language}\\r?\\n(.*?)\\r?\\n```"
        );
        Assert.True(example.Success, $"Missing {title} example in the consumer guide.");
        return example.Groups[1].Value;
    }
}
