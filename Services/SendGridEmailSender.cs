namespace Microfinance.Services;

// Asegúrate de tener los usings necesarios
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class SendGridEmailSender : IEmailSender
{
    private readonly ILogger<SendGridEmailSender> _logger;
    private readonly string _sendGridApiKey;
    private readonly string _sendGridFromEmail;
    private readonly string _sendGridFromName;

    public SendGridEmailSender(ILogger<SendGridEmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _sendGridApiKey = configuration["SendGrid:ApiKey"];
        _sendGridFromEmail = configuration["SendGrid:FromEmail"];
        _sendGridFromName = configuration["SendGrid:FromName"] ?? "Your App";

        if (string.IsNullOrEmpty(_sendGridApiKey))
        {
             _logger.LogWarning("SendGrid API Key is not configured.");
        }
         if (string.IsNullOrEmpty(_sendGridFromEmail))
        {
             _logger.LogWarning("SendGrid FromEmail is not configured.");
        }
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(_sendGridApiKey) || string.IsNullOrEmpty(_sendGridFromEmail))
        {
            _logger.LogError("SendGrid API Key or FromEmail is missing. Cannot send email.");
            return;
        }

        var client = new SendGridClient(_sendGridApiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_sendGridFromEmail, _sendGridFromName),
            Subject = subject,
            HtmlContent = htmlMessage,
        };
        msg.AddTo(new EmailAddress(email));

        try
        {
            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Email to {email} queued successfully!");
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError($"Error sending email to {email}. Status Code: {response.StatusCode}. Body: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An exception occurred while sending email to {email}");
        }
    }
}