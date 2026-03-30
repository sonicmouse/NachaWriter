using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	internal sealed class FieldAttribute(int order, int length,
		FieldValidationMode validationMode = FieldValidationMode.None,
		bool isRoutingNumber = false) : Attribute
	{
		public int Order { get; } = order;
		public int Length { get; } = length;
		public FieldValidationMode ValidationMode { get; } = validationMode;
		public bool IsRoutingNumber { get; } = isRoutingNumber;
	}
}
