namespace NachaWriter.Domain.Attributes
{
	internal enum EnumFormat
	{
		Default,
		Numeric
	}

	[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
	internal sealed class EnumFormatAttribute(EnumFormat enumFormat) : Attribute
	{
		public EnumFormat EnumFormat { get; } = enumFormat;
	}
}
