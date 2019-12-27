using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace manga_dl.Common
{
    public static class Converter
    {
        public static void CreatePDF(string pngPath, string pdfPath)
        {
            Document doc = new Document();
            PdfWriter.GetInstance(doc, new FileStream(pdfPath, FileMode.Create));
            doc.Open();
            Image png = Image.GetInstance(pngPath);
            png.SetAbsolutePosition(0, 0);
            doc.Add(png);
            doc.Close();
        }
        public static void CreatePDF(string[] pngPaths, string pdfPath)
        {
            Document doc = new Document();
            PdfWriter.GetInstance(doc, new FileStream(pdfPath, FileMode.Create));
            doc.Open();
            int i = 0;
            foreach(string pngPath in pngPaths) {
                Image png = Image.GetInstance(pngPath);
                png.SetAbsolutePosition(0, 0);
                doc.Add(png);
                i++;
                if(i < pngPaths.Length)
                    doc.NewPage();
            }
            doc.Close();
        }
    }
}