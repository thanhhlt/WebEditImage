public interface IEmailTemplateService
{
    Task<string> GetTemplateAsync(string templateName);
}

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _env;

    public EmailTemplateService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> GetTemplateAsync(string templateName)
    {
        var templatePath = Path.Combine(_env.ContentRootPath, "EmailTemplates", templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file '{templateName}' not found in EmailTemplates folder.");
        
        return await File.ReadAllTextAsync(templatePath);
    }
}
