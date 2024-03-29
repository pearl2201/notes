using HandlebarsDotNet;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NotesApi.Services;
public interface IMail
{
    Task SendMail(string to, string subject, string bodyFile, Dictionary<string, string> parameters);
}

public class GmailSettings
{
    public string Sender { get; set; }

    public string From { get; set; }

    public string AppPassword { get; set; }

    public string SupportEmail { get; set; }

    public string HeadDataEmail { get; set; }
}

public class GmailKitService : IMail
{
    private readonly ILogger<GmailKitService> _logger;
    private readonly GmailSettings _settings;

    public GmailKitService(ILogger<GmailKitService> logger, IOptions<GmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task SendMail(string to, string subject, string bodyFile, Dictionary<string, string> parameters)
    {
        try
        {
            parameters["support_email"] = _settings.SupportEmail;
            parameters["head_data_email"] = _settings.HeadDataEmail;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.Sender, _settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            var bodyBuilder = new BodyBuilder();
            var textBodyTemplateSource = File.ReadAllText($"./Views/Mails/{bodyFile}.txt");
            var htmlBodyTemplateSource = File.ReadAllText($"./Views/Mails/{bodyFile}.html");
            var textBodyTemplate = Handlebars.Compile(textBodyTemplateSource);
            var htmlBodyTemplate = Handlebars.Compile(htmlBodyTemplateSource);
            var textBody = textBodyTemplate(parameters);
            var htmlBody = htmlBodyTemplate(parameters);
            bodyBuilder.HtmlBody = htmlBody;
            bodyBuilder.TextBody = textBody;

            message.Body = bodyBuilder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, false);

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(_settings.From, _settings.AppPassword);

                client.Send(message);
                client.Disconnect(true);
            }


        }
        catch (Exception ex)
        {
            _logger.LogError("Send Mail Exception: {ex}", ex);
        }
    }


}