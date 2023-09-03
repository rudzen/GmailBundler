namespace GmailBundler.Domain.Models;

public sealed record Gmail(string Id, string From, string Subject, string Label, DateTime Date);