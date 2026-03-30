using NachaWriter.Domain.Attributes;

namespace NachaWriter.Domain.Enums
{
	/// <summary>
	/// NACHA Standard Entry Class (SEC) Transaction Codes.
	/// Determines the account type and whether the transaction is a credit or debit.
	/// </summary>
	[EnumFormat(EnumFormat.Numeric)]
	public enum TransactionCode
	{
		CheckingCredit = 22,
		CheckingPreNoteCredit = 23,
		CheckingReturn = 24,
		CheckingDebit = 27,
		CheckingPreNoteDebit = 28,

		SavingsCredit = 32,
		SavingsPreNoteCredit = 33,
		SavingsReturn = 34,
		SavingsDebit = 37,
		SavingsPreNoteDebit = 38
	}
}
