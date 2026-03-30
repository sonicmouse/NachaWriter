using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public sealed record AddendaRecord : IRecord
	{
		[Field(1, 1, FieldValidationMode.Numeric)]
		public RecordType RecordTypeCode { get; } = RecordType.Addenda;

		[Field(2, 2, FieldValidationMode.Numeric)]
		public AddendaTypeCode AddendaTypeCode { get; init; } = AddendaTypeCode.PaymentRelatedInformation;

		[Field(3, 80, FieldValidationMode.AddendaText)]
		public string PaymentRelatedInformation { get; init; } = string.Empty;

		[Field(4, 4, FieldValidationMode.Numeric)]
		public int AddendaSequenceNumber { get; init; } = 1;

		[Field(5, 7, FieldValidationMode.Numeric)]
		public int EntryDetailSequenceNumber { get; init; }
	}
}
