using RazorLight;

namespace Learnix.Infrastructure.Email;

internal sealed class EmailRenderer
{
    private readonly IRazorLightEngine _engine;

    public EmailRenderer()
    {
        var templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Email", "Templates");
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesPath)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync<TModel>(string templateFileName, TModel model)
    {
        var html = await _engine.CompileRenderAsync(templateFileName, model);
        var preMailer = new PreMailer.Net.PreMailer(html);
        var result = preMailer.MoveCssInline(removeStyleElements: true);
        return result.Html;
    }
}
