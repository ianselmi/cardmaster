using System.Text.Json;
using CardMaster.Services.Receipts;

namespace CardMaster.Services.Ai;

internal static class OpenAiRequestBody
{
    public static string Build(string model, int maxOutputTokens, byte[] image) => JsonSerializer.Serialize(new
    {
        model,
        max_completion_tokens = maxOutputTokens,
        messages = new object[]
        {
            new { role = "system", content = ReceiptAiPrompt.System },
            new { role = "user", content = new object[]
            {
                new { type = "image_url", image_url = new { url = $"data:{ReceiptAiImage.MediaType};base64,{Convert.ToBase64String(image)}", detail = "high" } },
                new { type = "text", text = ReceiptAiPrompt.User },
            } },
        },
        response_format = new { type = "json_schema", json_schema = new { name = "receipt", strict = true, schema = JsonDocument.Parse(ReceiptAiSchema.Json).RootElement } },
    });
}
