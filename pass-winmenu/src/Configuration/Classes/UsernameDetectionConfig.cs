using System;
using PassWinmenu.Utilities.ExtensionMethods;
using YamlDotNet.Serialization;

namespace PassWinmenu.Configuration
{
	public class UsernameDetectionConfig
	{
		[YamlIgnore]
		public UsernameDetectionMethod Method => (UsernameDetectionMethod)Enum.Parse(typeof(UsernameDetectionMethod), MethodString.ToPascalCase(), true);
		[YamlMember(Alias = "method")]
		public string MethodString { get; set; } = "regex";
		public UsernameDetectionOptions Options { get; set; } = new UsernameDetectionOptions();
	}
}
