using NachaWriter.Domain.Attributes;

namespace NachaWriter.Domain.Enums
{
	/// <summary>
	/// Identifies the hierarchy and structure of a NACHA file line.
	/// </summary>
	[EnumFormat(EnumFormat.Numeric)]
	public enum RecordType
	{
		FileHeader = 1,
		BatchHeader = 5,
		EntryDetail = 6,
		Addenda = 7,
		BatchControl = 8,
		FileControl = 9
	}
}
