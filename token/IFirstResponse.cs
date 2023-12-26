using System.Collections.Generic;

namespace mytoken
{
	internal class IFirstResponse
	{
		public List<IResponse>? data { get; set; }
		public int? code { get; set; }
		public string? message { get; set; }
	}
}