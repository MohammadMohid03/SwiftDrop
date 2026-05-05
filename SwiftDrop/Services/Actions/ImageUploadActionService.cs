using SwiftDrop.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SwiftDrop.Services.Actions
{
    public class ImageUploadActionService : IActionService
    {
        public string Name => "Imgur Uploader";

        private const string ImgurClientId = "YOUR_IMGUR_CLIENT_ID";
        private static readonly HttpClient _httpClient = new();

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            try
            {
                if (!File.Exists(input))
                    return ActionResult.Fail($"File not found: {input}");

                string ext = Path.GetExtension(input).ToLowerInvariant();
                var accepted = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
                if (Array.IndexOf(accepted, ext) < 0)
                    return ActionResult.Fail($"Unsupported: {ext}");

                byte[] imageBytes = await File.ReadAllBytesAsync(input);

                using var form = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(ext));
                form.Add(imageContent, "image");

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image");
                request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurClientId);
                request.Content = form;

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return ActionResult.Fail($"Imgur error: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                bool success = root.GetProperty("success").GetBoolean();
                if (!success)
                    return ActionResult.Fail("Imgur failed");

                string directUrl = root.GetProperty("data").GetProperty("link").GetString() ?? "";

                Clipboard.SetText(directUrl);

                return ActionResult.Ok($"Uploaded! URL copied.", url: directUrl);
            }
            catch (Exception ex)
            {
                return ActionResult.Fail($"Error: {ex.Message}");
            }
        }

        private static string GetMimeType(string extension) => extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }
}