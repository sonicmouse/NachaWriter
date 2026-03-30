using NachaWriter.Domain.Attributes;

namespace NachaWriter.Domain.Enums
{
	/// <summary>
	/// Standard Entry Class (SEC) Codes. 
	/// Identifies the specific type of payment/application (e.g., Consumer vs. B2B).
	/// </summary>
	[EnumFormat(EnumFormat.Default)]
	public enum StandardEntryClassCode
	{
		/// <summary>
		/// Prearranged Payment and Deposit (Consumer - Direct Deposit/Bill Pay)
		/// </summary>
		PPD,
		/// <summary>
		/// Corporate Credit or Debit (B2B - Vendor Payments)
		/// </summary>
		CCD,
		/// <summary>
		/// Internet-Initiated Entry (E-commerce)
		/// </summary>
		WEB,
		/// <summary>
		/// Telephone-Initiated Entry
		/// </summary>
		TEL,
		/// <summary>
		/// Corporate Trade Exchange (EDI Payments)
		/// </summary>
		CTX,
		/// <summary>
		/// International ACH Transaction (Cross-border payments)
		/// </summary>
		IAT,
		/// <summary>
		/// Acknowledgment Entry (ACH credit acknowledgment)
		/// </summary>
		ACK,
		/// <summary>
		/// Corporate Trade Exchange Acknowledgment (EDI payment acknowledgment)
		/// </summary>
		ATX,
		/// <summary>
		/// Accounts Receivable Entry (Paper check conversion - consumer)
		/// </summary>
		ARC,
		/// <summary>
		/// Back Office Conversion (Paper check conversion - merchant)
		/// </summary>
		BOC,
		/// <summary>
		/// Point-of-Purchase Entry (In-person check conversion)
		/// </summary>
		POP,
		/// <summary>
		/// Re-presented Check Entry (Returned check re-presentment)
		/// </summary>
		RCK,
		/// <summary>
		/// Destroyed Check Entry (Checks destroyed at point of presentment)
		/// </summary>
		XCK,
		/// <summary>
		/// Machine Transfer Entry (ATM-initiated debits/credits)
		/// </summary>
		MTE,
		/// <summary>
		/// Shared Network Transaction (Shared network POS/ATM)
		/// </summary>
		SHR,
		/// <summary>
		/// Point-of-Sale Entry (POS debit card transactions)
		/// </summary>
		POS,
		/// <summary>
		/// Automated Enrollment Entry (ODFI enrollment notifications)
		/// </summary>
		ENR,
		/// <summary>
		/// Truncated Entry (Check truncation)
		/// </summary>
		TRC,
		/// <summary>
		/// Truncated Credits (Bulk check truncation credits)
		/// </summary>
		TRX,
		/// <summary>
		/// Cash Concentration or Disbursement (Same-day CCD)
		/// </summary>
		CIE,
		/// <summary>
		/// Customer Initiated Entry (Consumer-initiated credits)
		/// </summary>
		DNE,
		/// <summary>
		/// Death Notification Entry (Federal agency death notifications)
		/// </summary>
		CBR,
		/// <summary>
		/// Corporate Cross-Border Payment (International corporate payments)
		/// </summary>
		PBR
	}
}
