using System;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;

class Program {
    static void Main() {
        try {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var doc = PdfReader.Open(@"C:\Users\gabri\Documents\Wyvern\Wyvern\Wyvern.Api\Templates\ficha55.pdf", PdfDocumentOpenMode.ReadOnly);
            if (doc.AcroForm != null) {
                Console.WriteLine("AcroForm found! Fields:");
                foreach (var name in doc.AcroForm.Fields.Names) {
                    Console.WriteLine(name);
                }
            } else {
                Console.WriteLine("NO ACROFORM FOUND!");
            }
        } catch (Exception ex) {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
