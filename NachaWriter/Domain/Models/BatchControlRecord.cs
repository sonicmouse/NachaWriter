using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public sealed record BatchControlRecord : IRecord
	{
		[Field(1, 1, FieldValidationMode.Numeric)]
		public RecordType RecordTypeCode { get; } = RecordType.BatchControl;

		[Field(2, 3, FieldValidationMode.Numeric)]
		public ServiceClassCode ServiceClassCode { get; init; }

		[Field(3, 6, FieldValidationMode.Numeric)]
		public int EntryAddendaCount { get; init; }

		[Field(4, 10, FieldValidationMode.Numeric)]
		public ulong EntryHash { get; init; }

		[Field(5, 12, FieldValidationMode.Numeric)]
		public decimal TotalDebitEntryDollarAmount { get; init; }

		[Field(6, 12, FieldValidationMode.Numeric)]
		public decimal TotalCreditEntryDollarAmount { get; init; }

		[Field(7, 10, FieldValidationMode.Alphanumeric)]
		public string CompanyIdentification { get; init; } = string.Empty;

		[Field(8, 19, FieldValidationMode.Alphanumeric)]
		public string MessageAuthenticationCode { get; init; } = string.Empty;

		[Field(9, 6, FieldValidationMode.Alphanumeric)]
		public string Reserved { get; } = string.Empty;

		[Field(10, 8, FieldValidationMode.Numeric)]
		public string OriginatingDfiIdentification { get; init; } = string.Empty;

		[Field(11, 7, FieldValidationMode.Numeric)]
		public int BatchNumber { get; init; }
	}
}
