using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public sealed record FileControlRecord : IRecord
	{
		[Field(1, 1, FieldValidationMode.Numeric)]
		public RecordType RecordTypeCode { get; } = RecordType.FileControl;

		[Field(2, 6, FieldValidationMode.Numeric)]
		public int BatchCount { get; init; }

		[Field(3, 6, FieldValidationMode.Numeric)]
		public int BlockCount { get; init; }

		[Field(4, 8, FieldValidationMode.Numeric)]
		public int EntryAddendaCount { get; init; }

		[Field(5, 10, FieldValidationMode.Numeric)]
		public ulong EntryHash { get; init; }

		[Field(6, 12, FieldValidationMode.Numeric)]
		public decimal TotalDebitEntryDollarAmountInFile { get; init; }

		[Field(7, 12, FieldValidationMode.Numeric)]
		public decimal TotalCreditEntryDollarAmountInFile { get; init; }

		[Field(8, 39, FieldValidationMode.Alphanumeric)]
		public string Reserved { get; } = string.Empty;
	}
}
