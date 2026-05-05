using SwiftDrop.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    public sealed class ImageToPdfActionService : IActionService
    {
        private static readonly string[] SupportedExtensions =
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"
        };

        private const long JpegQuality = 92L;

        public string Name => "Image to PDF Converter";

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(input))
                        return ActionResult.Fail($"File not found: {input}");

                    string ext = Path.GetExtension(input).ToLowerInvariant();
                    if (!SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        return ActionResult.Fail(
                            $"Only image files can be converted to PDF. Unsupported file type: {ext}");
                    }

                    string outputPath = BuildOutputPath(input);

                    using var sourceImage = Image.FromFile(input);
                    byte[] jpegBytes = ConvertImageToJpeg(sourceImage);
                    byte[] pdfBytes = BuildPdfDocument(sourceImage, jpegBytes);

                    File.WriteAllBytes(outputPath, pdfBytes);

                    return ActionResult.Ok(
                        $"Converted to PDF: {Path.GetFileName(outputPath)}",
                        outputPath: outputPath);
                }
                catch (OutOfMemoryException)
                {
                    return ActionResult.Fail("PDF conversion failed: the file is not a valid image.");
                }
                catch (Exception ex)
                {
                    return ActionResult.Fail($"PDF conversion failed: {ex.Message}");
                }
            });
        }

        private static string BuildOutputPath(string input)
        {
            string outputPath = Path.ChangeExtension(input, ".pdf");
            if (!File.Exists(outputPath))
                return outputPath;

            string dir = Path.GetDirectoryName(input) ?? "";
            string name = Path.GetFileNameWithoutExtension(input);
            return Path.Combine(dir, $"{name}_converted.pdf");
        }

        private static byte[] ConvertImageToJpeg(Image sourceImage)
        {
            using var canvas = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(canvas))
            {
                graphics.Clear(Color.White);
                graphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);
            }

            using var stream = new MemoryStream();
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);
            canvas.Save(stream, GetJpegCodecInfo(), encoderParams);
            return stream.ToArray();
        }

        private static byte[] BuildPdfDocument(Image sourceImage, byte[] jpegBytes)
        {
            double horizontalDpi = NormalizeDpi(sourceImage.HorizontalResolution);
            double verticalDpi = NormalizeDpi(sourceImage.VerticalResolution);

            double pageWidth = sourceImage.Width * 72d / horizontalDpi;
            double pageHeight = sourceImage.Height * 72d / verticalDpi;

            string widthText = pageWidth.ToString("0.###", CultureInfo.InvariantCulture);
            string heightText = pageHeight.ToString("0.###", CultureInfo.InvariantCulture);
            string contentStream = $"q{Environment.NewLine}{widthText} 0 0 {heightText} 0 0 cm{Environment.NewLine}/Im0 Do{Environment.NewLine}Q";
            byte[] contentBytes = Encoding.ASCII.GetBytes(contentStream);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

            var offsets = new long[6];

            WriteAscii(writer, "%PDF-1.4\n");
            writer.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

            offsets[1] = stream.Position;
            WriteAscii(writer, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = stream.Position;
            WriteAscii(writer, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = stream.Position;
            WriteAscii(
                writer,
                $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthText} {heightText}] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

            offsets[4] = stream.Position;
            WriteAscii(
                writer,
                $"4 0 obj\n<< /Type /XObject /Subtype /Image /Width {sourceImage.Width} /Height {sourceImage.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpegBytes.Length} >>\nstream\n");
            writer.Write(jpegBytes);
            WriteAscii(writer, "\nendstream\nendobj\n");

            offsets[5] = stream.Position;
            WriteAscii(
                writer,
                $"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            writer.Write(contentBytes);
            WriteAscii(writer, "\nendstream\nendobj\n");

            long xrefOffset = stream.Position;
            WriteAscii(writer, "xref\n0 6\n");
            WriteAscii(writer, "0000000000 65535 f \n");
            for (int i = 1; i <= 5; i++)
                WriteAscii(writer, $"{offsets[i]:D10} 00000 n \n");

            WriteAscii(
                writer,
                $"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

            writer.Flush();
            return stream.ToArray();
        }

        private static double NormalizeDpi(float dpi)
            => dpi > 0 ? dpi : 96d;

        private static void WriteAscii(BinaryWriter writer, string text)
            => writer.Write(Encoding.ASCII.GetBytes(text));

        private static ImageCodecInfo GetJpegCodecInfo()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.MimeType == "image/jpeg")
                    return codec;
            }

            throw new InvalidOperationException("JPEG codec not found on this system.");
        }
    }
}
