using ReactorOutlook.Models;

namespace ReactorOutlook.Services;

public interface IMailService
{
    Task<MailFolder[]> GetFoldersAsync();
    Task<EmailMessage[]> GetMessagesAsync(string folderId);
}
