using NachaWriter.Domain.Attributes;

namespace NachaWriter.Domain.Enums
{
	/// <summary>
	/// Identifies the originator type. 1 is standard for almost all commercial entities.
	/// </summary>
	[EnumFormat(EnumFormat.Numeric)]
	public enum OriginatorStatusCode
	{
		Commercial = 1,
		FederalGovernment = 2
	}
}
