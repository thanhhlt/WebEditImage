using SendGrid;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid.Helpers.Mail;

public class MailContent
{
    public string? To {get; set;}
    public string? Sub {get; set;}
    public string? Body {get; set;}
}

public class SendMailService : IEmailSender
{
    private readonly string _apiKey;

    private readonly ILogger<SendMailService> logger;

    public SendMailService(ILogger<SendMailService> _logger, string apiKey)
    {
        logger = _logger;
        _apiKey = apiKey;
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(email))
        {
            logger.LogError("Địa chỉ email không hợp lệ.");
            return;
        }

        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress("no-reply@perfectpix.art", "PerfecPix");
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlMessage);
        msg.ReplyTo = new EmailAddress("no-reply@perfectpix.art");

        try {
            await client.SendEmailAsync(msg);
        } 
        catch (Exception ex) {
            System.IO.Directory.CreateDirectory("mailssave");
            var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid ());
            await File.WriteAllTextAsync(emailsavefile, $"To: {email}\nSubject: {subject}\nBody: {htmlMessage}");

            logger.LogInformation("Lỗi gửi mail, lưu tại - " + emailsavefile);
            logger.LogError(ex.Message);
        }

        logger.LogInformation("send mail to: " + email);
    }
}