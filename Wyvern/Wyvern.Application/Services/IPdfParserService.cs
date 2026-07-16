using System.IO;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    public interface IPdfParserService
    {
        Personagem ParsePdf(Stream pdfStream);
    }
}
