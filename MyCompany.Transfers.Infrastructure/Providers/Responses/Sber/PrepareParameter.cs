using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareParameter
{
    [XmlAttribute("comment")]
    public string Comment { get; set; }

    [XmlAttribute("fullname")]
    public string FullName { get; set; }

    [XmlAttribute("isEditable")]
    public bool IsEditable { get; set; }

    [XmlAttribute("isMasked")]
    public bool IsMasked { get; set; }

    [XmlAttribute("isRequired")]
    public bool IsRequired { get; set; }

    [XmlAttribute("isVisible")]
    public bool IsVisible { get; set; }

    [XmlAttribute("max_length")]
    public int MaxLength { get; set; }

    [XmlAttribute("min_length")]
    public int MinLength { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("value")]
    public string Value { get; set; }

    //[XmlAttribute("step")]
    //public int? Step { get; set; }
}