using System;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf;

class Program
{
    static void Main(string[] args)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var pdfPath = @"C:\Users\gabri\.gemini\antigravity\brain\30f80570-05bd-4e5c-866a-54054b2a9567\media__1783973567183.pdf";
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
        
        foreach (var name in doc.AcroForm.Fields.Names)
        {
            var field = doc.AcroForm.Fields[name];
            var val = field.Value;
            if (val != null) {
                string valStr = val.ToString();
                if (val is PdfString pdfStr) valStr = pdfStr.Value;
                if (val is PdfName pdfName) valStr = pdfName.Value;

                Console.WriteLine($"[{name}] ({val.GetType().Name}) = {valStr}");
            }
        }
    }
}
