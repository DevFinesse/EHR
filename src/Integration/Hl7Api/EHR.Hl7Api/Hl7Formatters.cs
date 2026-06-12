using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace EHR.Hl7Api;

public sealed class Hl7TextInputFormatter : TextInputFormatter
{
    public Hl7TextInputFormatter()
    {
        SupportedMediaTypes.Add("text/plain");
        SupportedMediaTypes.Add("application/hl7-v2");
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.ASCII);
    }

    protected override bool CanReadType(Type type) => type == typeof(string);

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
        return await InputFormatterResult.SuccessAsync(await reader.ReadToEndAsync(context.HttpContext.RequestAborted));
    }
}

public sealed class Hl7TextOutputFormatter : TextOutputFormatter
{
    public Hl7TextOutputFormatter()
    {
        SupportedMediaTypes.Add("application/hl7-v2");
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.ASCII);
    }

    protected override bool CanWriteType(Type? type) => type == typeof(string);

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        await context.HttpContext.Response.WriteAsync((string?)context.Object ?? string.Empty, selectedEncoding, context.HttpContext.RequestAborted);
    }
}
