using NachaWriter.Configuration;
using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public sealed record FileHeaderRecord : IRecord
	{
		[Field(1, 1, FieldValidationMode.Numeric)]
		public RecordType RecordTypeCode { get; } = RecordType.FileHeader;

		[Field(2, 2, FieldValidationMode.Numeric)]
		public int PriorityCode { get; init; } = NachaConstants.DefaultPriorityCode;

		[Field(3, 10, FieldValidationMode.Numeric, isRoutingNumber: true)]
		public string ImmediateDestination { get; init; } = string.Empty;

		[Field(4, 10, FieldValidationMode.Numeric, isRoutingNumber: true)]
		public string ImmediateOrigin { get; init; } = string.Empty;

		[Field(5, 6)]
		public DateOnly FileCreationDate { get; init; }

		[Field(6, 4)]
		public TimeOnly FileCreationTime { get; init; }

		[Field(7, 1, FieldValidationMode.UppercaseAlphanumeric)]
		public char FileIdModifier { get; init; } = 'A';

		[Field(8, 3, FieldValidationMode.Numeric)]
		public int RecordSize { get; } = NachaConstants.RecordLength;

		[Field(9, 2, FieldValidationMode.Numeric)]
		public int BlockingFactor { get; } = NachaConstants.BlockingFactor;

		[Field(10, 1, FieldValidationMode.Numeric)]
		public int FormatCode { get; } = NachaConstants.DefaultFormatCode;

		[Field(11, 23, FieldValidationMode.Alphanumeric)]
		public string ImmediateDestinationName { get; init; } = string.Empty;

		[Field(12, 23, FieldValidationMode.Alphanumeric)]
		public string ImmediateOriginName { get; init; } = string.Empty;

		[Field(13, 8, FieldValidationMode.Alphanumeric)]
		public string ReferenceCode { get; init; } = string.Empty;
	}
}
