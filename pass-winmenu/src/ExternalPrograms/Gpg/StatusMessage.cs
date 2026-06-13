using System;

#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class StatusMessage
	{
		public GpgStatusCode StatusCode { get; }
		public string RawStatusCode { get; }
		public string Message { get; }

		public StatusMessage(string rawStatusCode, string? message)
		{
			RawStatusCode = rawStatusCode;
			Message = message ?? string.Empty;
			if (Enum.TryParse(rawStatusCode, false, out GpgStatusCode parsedStatusCode))
			{
				StatusCode = parsedStatusCode;
			}
			else
			{
				StatusCode = GpgStatusCode.UnknownStatusCode;
			}
		}

		public StatusMessage(GpgStatusCode statusCode, string? message)
		{
			StatusCode = statusCode;
			RawStatusCode = statusCode.ToString();
			Message = message ?? string.Empty;
		}

		public override string ToString() => $"[{RawStatusCode}] {Message}";
	}
}
