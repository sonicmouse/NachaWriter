namespace NachaWriter.Configuration
{
	/// <summary>
	/// Fundamental domain rules for the NACHA specification.
	/// These are internal to prevent the consuming application from coupling to NACHA specifics.
	/// </summary>
	internal static class NachaConstants
	{
		/// <summary>
		/// The default priority code required by the Federal Reserve.
		/// </summary>
		public const int DefaultPriorityCode = 1;

		/// <summary>
		/// Every single record (line) in a NACHA file must be exactly <see cref="RecordLength"/> characters long.
		/// </summary>
		public const int RecordLength = 94;

		/// <summary>
		/// A NACHA file must be padded with <see cref="FillerCharacter"/> so the total line count is a multiple of <see cref="BlockingFactor"/>.
		/// </summary>
		public const int BlockingFactor = 10;

		/// <summary>
		/// The default format code required by the NACHA spec for the File Header.
		/// </summary>
		public const int DefaultFormatCode = 1;

		/// <summary>
		/// Represents the character used to fill unused lines when padding the file to meet the blocking factor.
		/// </summary>
		public const char FillerCharacter = '9';

		/// <summary>
		/// Line feed format for NACHA files, explicitly using CRLF as required by most mainframe parsers, regardless of the host OS.
		/// </summary>
		public const string NewLine = "\r\n";
	}
}
