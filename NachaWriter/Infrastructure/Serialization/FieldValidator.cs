using NachaWriter.Domain.Enums;

namespace NachaWriter.Infrastructure.Serialization
{
	internal static class FieldValidator
	{
		public static void Validate(string? value, FieldValidationMode mode)
		{
			if (string.IsNullOrEmpty(value) || mode is FieldValidationMode.None)
			{
				return;
			}

			for (var i = 0; i < value.Length; ++i)
			{
				var c = value[i];
				var isValidChar = mode switch
				{
					FieldValidationMode.Numeric => IsNumeric(c),
					FieldValidationMode.UppercaseAlphanumeric => IsUppercaseAlphanumeric(c),
					FieldValidationMode.Alphanumeric => IsAlphanumeric(c),
					FieldValidationMode.AddendaText => IsAddendaText(c),
					_ => true
				};

				if (!isValidChar)
				{
					throw new ArgumentException($"The field contains an invalid " +
						$"character '{c}' at position {i} for the validation mode: {mode}. Value: '{value}'");
				}
			}
		}

		private static bool IsNumeric(char c) =>
			c is >= '0' and <= '9';

		private static bool IsUppercaseAlphanumeric(char c) =>
			c is (>= 'A' and <= 'Z') or (>= '0' and <= '9');

		private static bool IsAlphanumeric(char c) =>
			c is (>= 'A' and <= 'Z') or
				 (>= 'a' and <= 'z') or
				 (>= '0' and <= '9') or
				 ' ' or '.' or '/' or '(' or ')' or '&' or '\'' or '-';

		private static bool IsAddendaText(char c) =>
			IsAlphanumeric(c) ||
			c is '!' or '#' or '$' or '%' or '*' or '+' or ':' or ';' or
				 '=' or '?' or '@' or '[' or ']' or '^' or '_' or '{' or '|' or '}';
	}
}
