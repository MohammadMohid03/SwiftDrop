// SwiftDrop/Services/Actions/ImageConvertActionService.cs
//
// Converts a dropped PNG file to JPG using Windows Imaging Component (WIC)
// via the System.Drawing.Common NuGet package (backed by GDI+).
// Output file is saved alongside the original with _converted suffix.

using SwiftDrop.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    public sealed class ImageConvertActionService : IActionService
    {
        public string Name => "PNG → JPG Converter";

        // JPG quality 0-100. 92 is perceptually lossless for most images.
        private const long JpegQuality = 92L;

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Validate: must be an existing .png file
                    if (!File.Exists(input))
                        return ActionResult.Fail($"File not found: {input}");

                    string ext = Path.GetExtension(input).ToLowerInvariant();
                    if (ext != ".png")
                        return ActionResult.Fail(
                            $"Expected a .png file, got '{ext}'");

                    // Build output path
                    string outputPath = Path.ChangeExtension(input, ".jpg");
                    if (File.Exists(outputPath))
                        outputPath = Path.ChangeExtension(input, "_converted.jpg");

                    // Load the PNG
                    using var sourceImage = Image.FromFile(input);

                    // Create a white-background canvas (PNG may have transparency)
                    using var canvas = new Bitmap(sourceImage.Width, sourceImage.Height,
                                                  PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(canvas))
                    {
                        g.Clear(Color.White); // fill transparent areas with white
                        g.DrawImage(sourceImage, 0, 0,
                                    sourceImage.Width, sourceImage.Height);
                    }

                    // Configure JPEG encoder quality
                    var jpegCodec = GetJpegCodecInfo();
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(
                        System.Drawing.Imaging.Encoder.Quality, JpegQuality);

                    // Save as JPEG
                    canvas.Save(outputPath, jpegCodec, encoderParams);

                    return ActionResult.Ok(
                        $"Converted: {Path.GetFileName(outputPath)}",
                        outputPath: outputPath);
                }
                catch (Exception ex)
                {
                    return ActionResult.Fail($"Conversion failed: {ex.Message}");
                }
            });
        }

        /// <summary>Gets the system JPEG ImageCodecInfo by MIME type.</summary>
        private static ImageCodecInfo GetJpegCodecInfo()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.MimeType == "image/jpeg") return codec;

            throw new InvalidOperationException("JPEG codec not found on this system.");
        }
    }
}