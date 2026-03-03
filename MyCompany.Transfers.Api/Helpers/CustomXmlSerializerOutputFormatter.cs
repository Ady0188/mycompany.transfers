using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;
using System.Xml;

namespace MyCompany.Transfers.Api.Helpers;

/// <summary>
/// XML-форматтер для протокола Tillabuy (НКО): убирает xsi/xsd, подставляет encoding windows-1251 в объявлении.
/// Включается только на эндпоинтах с атрибутом <see cref="UseCustomXmlAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class UseCustomXmlAttribute : Attribute { }

public class CustomXmlSerializerOutputFormatter : XmlSerializerOutputFormatter
{
    public CustomXmlSerializerOutputFormatter()
    {
        SupportedMediaTypes.Clear();
        SupportedMediaTypes.Add("application/xml");
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        if (!base.CanWriteResult(context))
            return false;

        var endpoint = context.HttpContext.GetEndpoint();
        return endpoint?.Metadata.GetMetadata<UseCustomXmlAttribute>() != null;
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var xmlWriterSettings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = true,
            Encoding = Encoding.UTF8
        };

        var obj = context.Object;
        if (obj is null)
            throw new InvalidOperationException("Response object is null.");

        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings))
        {
            var serializer = CreateSerializer(obj.GetType())
                ?? throw new InvalidOperationException("XmlSerializer could not be created.");
            serializer.Serialize(xmlWriter, obj);
        }

        var xmlString = stringWriter.ToString()
            .Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", "")
            .Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "")
            .Replace("Response  ", "Response")
            .Replace("encoding=\"utf-16\"", "encoding=\"windows-1251\"")
            .Replace(" url=\"http://nko-rr.ru\"", "");

        var response = context.HttpContext.Response;
        response.ContentType = "application/xml";
        return response.WriteAsync(xmlString);
    }
}
