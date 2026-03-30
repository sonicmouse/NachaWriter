using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public sealed record BatchHeaderRecord : IRecord
	{
		[Field(1, 1, FieldValidationMode.Numeric)]
		public RecordType RecordTypeCode { get; } = RecordType.BatchHeader;

		[Field(2, 3, FieldValidationMode.Numeric)]
		public ServiceClassCode ServiceClassCode { get; init; } = ServiceClassCode.Mixed;

		[Field(3, 16, FieldValidationMode.Alphanumeric)]
		public string CompanyName { get; init; } = string.Empty;

		[Field(4, 20, FieldValidationMode.Alphanumeric)]
		public string CompanyDiscretionaryData { get; init; } = string.Empty;

		[Field(5, 10, FieldValidationMode.Alphanumeric)]
		public string CompanyIdentification { get; init; } = string.Empty;

		[Field(6, 3, FieldValidationMode.UppercaseAlphanumeric)]
		public StandardEntryClassCode StandardEntryClassCode { get; init; } = StandardEntryClassCode.PPD;

		[Field(7, 10, FieldValidationMode.Alphanumeric)]
		public string CompanyEntryDescription { get; init; } = string.Empty;

		[Field(8, 6, FieldValidationMode.Alphanumeric)]
		public string CompanyDescriptiveDate { get; init; } = string.Empty;

		[Field(9, 6)]
		public DateOnly EffectiveEntryDate { get; init; }

		[Field(10, 3, FieldValidationMode.Alphanumeric)]
		public string SettlementDate { get; } = string.Empty;

		[Field(11, 1, FieldValidationMode.Numeric)]
		public OriginatorStatusCode OriginatorStatusCode { get; init; } = OriginatorStatusCode.Commercial;

		[Field(12, 8, FieldValidationMode.Numeric)]
		public string OriginatingDfiIdentification { get; init; } = string.Empty;

		[Field(13, 7, FieldValidationMode.Numeric)]
		public int BatchNumber { get; init; }
	}
}
