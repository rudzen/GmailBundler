using GmailBundler.Dto;

namespace GmailBundler.Csv.Interfaces;

public interface ICsvConverter
{
    string Create(Gmail gmail);
}