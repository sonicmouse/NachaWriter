namespace NachaWriter.Domain.Enums
{
	public enum FieldValidationMode
	{
		None,
		/// <summary>
		/// 0 through 9
		/// </summary>
		Numeric,
		/// <summary>
		/// 0-9, A-Z, a-z, and specific symbols: . / () & ' - and spaces
		/// </summary>
		Alphanumeric,
		/// <summary>
		/// UPPERCASE A-Z or 0-9. No symbols allowed (e.g., File ID Modifier)
		/// </summary>
		UppercaseAlphanumeric,
		/// <summary>
		/// Extended symbols allowed only in Addenda payment information
		/// </summary>
		AddendaText
	}
}
