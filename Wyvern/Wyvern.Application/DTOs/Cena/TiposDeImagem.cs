namespace Wyvern.Application.DTOs.Cena
{
    public static class TiposDeImagem
    {
        /// <summary>Identifica PNG, JPEG e WebP pelos primeiros bytes (o tipo informado pelo cliente não vale).</summary>
        public static string? Identificar(byte[] b)
        {
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "image/webp";
            return null;
        }
    }
}
