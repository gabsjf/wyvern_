Add-Type -Path "c:\Users\gabri\.nuget\packages\pdfsharp\6.1.1\lib\net6.0\PdfSharp.dll"
[System.Text.Encoding]::RegisterProvider([System.Text.CodePagesEncodingProvider]::Instance)
$doc = [PdfSharp.Pdf.IO.PdfReader]::Open("C:\Users\gabri\.gemini\antigravity\brain\30f80570-05bd-4e5c-866a-54054b2a9567\media__1783967591405.pdf", [PdfSharp.Pdf.IO.PdfDocumentOpenMode]::Import)
foreach ($f in $doc.AcroForm.Fields) {
    Write-Host "$($f.Name): $($f.Value)"
}
