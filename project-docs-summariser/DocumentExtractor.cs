using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;

namespace project_docs_summariser
{
    public static class DocumentExtractor
    {
        public static string ExtractText(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file could not be found.", filePath);

            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".pdf":
                    return ReadPdf(filePath);
                case ".docx":
                    return ReadDocx(filePath);
                case ".pptx":
                    return ReadPptx(filePath);
                case ".doc":
                    throw new NotSupportedException("Older binary Word files (.doc) are outdated. Please save your document as a modern .docx file and try again.");
                case ".ppt":
                    throw new NotSupportedException("Older binary PowerPoint files (.ppt) are outdated. Please save your presentation as a modern .pptx file and try again.");
                default:
                    throw new NotSupportedException($"File format '{extension}' is not supported.");
            }
        }

        private static string ReadPdf(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }

        private static string ReadDocx(string filePath)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                return body != null ? body.InnerText : string.Empty;
            }
        }

        private static string ReadPptx(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            using (PresentationDocument pptDoc = PresentationDocument.Open(filePath, false))
            {
                var presentationPart = pptDoc.PresentationPart;
                if (presentationPart != null && presentationPart.SlideParts != null)
                {
                    foreach (var slidePart in presentationPart.SlideParts)
                    {
                        if (slidePart.Slide != null)
                        {
                            foreach (var text in slidePart.Slide.Descendants<A.Text>())
                            {
                                sb.AppendLine(text.Text);
                            }
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}