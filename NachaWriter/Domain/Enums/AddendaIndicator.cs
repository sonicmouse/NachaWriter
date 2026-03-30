using NachaWriter.Domain.Attributes;

namespace NachaWriter.Domain.Enums
{
	/// <summary>
	/// Indicates whether a transaction carries an attached Addenda record.
	/// </summary>
	[EnumFormat(EnumFormat.Numeric)]
	public enum AddendaIndicator
	{
		None = 0,
		HasAddenda = 1
	}
}
