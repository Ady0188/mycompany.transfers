using MyCompany.Transfers.Contract.Tillabuy.Responses;
using MyCompany.Transfers.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;
using System.Xml;

namespace MyCompany.Transfers.Api.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class UseCustomXmlAttribute : Attribute { }

public class CustomXmlSerializerOutputFormatter : XmlSerializerOutputFormatter
{
    public CustomXmlSerializerOutputFormatter()
    {
        SupportedMediaTypes.Clear();
        SupportedMediaTypes.Add("application/xml");
        // при необходимости:
        // SupportedMediaTypes.Add("text/xml");
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        // ❶ сначала проверим стандартные условия (тип, media type и т.д.)
        if (!base.CanWriteResult(context))
            return false;

        // ❷ включаемся только если endpoint помечен атрибутом
        var endpoint = context.HttpContext.GetEndpoint();
        var enabled = endpoint?.Metadata.GetMetadata<UseCustomXmlAttribute>() != null;

        return enabled;
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var xmlWriterSettings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = true,
            Encoding = Encoding.UTF8
        };

        using (var stringWriter = new StringWriter())
        using (var xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings))
        {
            var typeToSerialize = context.Object.GetType();
            var serializer = CreateSerializer(typeToSerialize);

            // Serialize the object to XML
            serializer.Serialize(xmlWriter, context.Object);

            // Get the XML string
            var xmlString = stringWriter.ToString();

            // Remove xmlns:xsi and xmlns:xsd namespaces
            xmlString = xmlString.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", "")
                                 .Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "")
                                 .Replace("Response  ", "Response")
                                 .Replace("Response  ", "Response")
                                 .Replace("encoding=\"utf-16\"", "encoding=\"windows-1251\"")
                                 .Replace(" url=\"http://nko-rr.ru\"", "");

            // Write the modified XML to the response body
            var response = context.HttpContext.Response;
            response.ContentType = "application/xml";

            string? tillabuyResult = null;
            string? nkoResult = null;
            //if (typeToSerialize.Name.Equals(nameof(NKOGetProductsResponse)))
            //    nkoResult = xmlString.DeserializeXML<NKOGetProductsResponse>()!.StringResult;
            //else if (typeToSerialize.Name.Equals(nameof(NKOPrepareResponse)))
            //    nkoResult = xmlString.DeserializeXML<NKOPrepareResponse>()!.StringResult;
            if (typeToSerialize.Name.Equals(nameof(NKOCheckResponse)))
                nkoResult = xmlString.DeserializeXML<NKOCheckResponse>()!.StringResult;
            else if (typeToSerialize.Name.Equals(nameof(NKOPaymentResponse)))
                nkoResult = xmlString.DeserializeXML<NKOPaymentResponse>()!.StringResult;

            if (tillabuyResult != null)
                return response.WriteAsync(tillabuyResult);

            return response.WriteAsync(xmlString);
        }

        //return base.WriteResponseBodyAsync(context, selectedEncoding);
    }
}