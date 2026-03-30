using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public sealed record EntryDetailRecord : IRecord
	{
		[Field(1, 1, FieldValidationMode.Numeric)]
		public RecordType RecordTypeCode { get; } = RecordType.EntryDetail;

		[Field(2, 2, FieldValidationMode.Numeric)]
		public TransactionCode TransactionCode { get; init; }

		[Field(3, 8, FieldValidationMode.Numeric)]
		public string ReceivingDfiIdentification { get; init; } = string.Empty;

		[Field(4, 1, FieldValidationMode.Numeric)]
		public int CheckDigit { get; init; }

		[Field(5, 17, FieldValidationMode.Alphanumeric)]
		public string DfiAccountNumber { get; init; } = string.Empty;

		[Field(6, 10, FieldValidationMode.Numeric)]
		public decimal Amount { get; init; }

		[Field(7, 15, FieldValidationMode.UppercaseAlphanumeric)]
		public string IndividualIdentificationNumber { get; init; } = string.Empty;

		[Field(8, 22, FieldValidationMode.Alphanumeric)]
		public string IndividualName { get; init; } = string.Empty;

		[Field(9, 2, FieldValidationMode.Alphanumeric)]
		public string DiscretionaryData { get; init; } = string.Empty;

		[Field(10, 1, FieldValidationMode.Numeric)]
		public AddendaIndicator AddendaRecordIndicator { get; init; } = AddendaIndicator.None;

		[Field(11, 15, FieldValidationMode.Numeric)]
		public ulong TraceNumber { get; init; }
	}
}
