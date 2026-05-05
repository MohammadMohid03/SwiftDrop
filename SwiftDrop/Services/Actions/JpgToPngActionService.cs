using SwiftDrop.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    /// <summary>
    /// Converts JPG/JPEG images to PNG format.
    /// Output file is saved alongside the original.
    /// </summary>
    public sealed class JpgToPngActionService : IActionService
    {
        public string Name => "JPG → PNG Converter";

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(input))
                        return ActionResult.Fail($"File not found: {input}");

                    string ext = Path.GetExtension(input).ToLowerInvariant();
                    if (ext is not (".jpg" or ".jpeg"))
                        return ActionResult.Fail($"Expected a .jpg/.jpeg file, got '{ext}'");

                    // Build output path
                    string outputPath = Path.ChangeExtension(input, ".png");
                    if (File.Exists(outputPath))
                    {
                        var nameNoExt = Path.GetFileNameWithoutExtension(input);
                        var dir = Path.GetDirectoryName(input)!;
                        outputPath = Path.Combine(dir, $"{nameNoExt}_converted.png");
                    }

                    // Load the JPEG
                    using var sourceImage = Image.FromFile(input);

                    // Save as PNG (lossless, preserves quality)
                    sourceImage.Save(outputPath, ImageFormat.Png);

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
    }
}
