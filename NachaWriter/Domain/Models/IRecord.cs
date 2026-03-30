using NachaWriter.Domain.Enums;

namespace NachaWriter.Domain.Models
{
	public interface IRecord
	{
		public abstract RecordType RecordTypeCode { get; }
	}
}
