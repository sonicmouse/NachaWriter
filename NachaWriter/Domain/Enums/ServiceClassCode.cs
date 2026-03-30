using NachaWriter.Domain.Attributes;

namespace NachaWriter.Domain.Enums
{
	/// <summary>
	/// Identifies whether the batch contains debits, credits, or both.
	/// </summary>
	[EnumFormat(EnumFormat.Numeric)]
	public enum ServiceClassCode
	{
		Mixed = 200,
		CreditsOnly = 220,
		DebitsOnly = 225
	}
}
