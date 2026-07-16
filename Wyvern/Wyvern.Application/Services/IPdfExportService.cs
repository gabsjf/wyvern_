using System.IO;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    public interface IPdfExportService
    {
        byte[] ExportPdf(Personagem personagem, string templatePath);
    }
}
