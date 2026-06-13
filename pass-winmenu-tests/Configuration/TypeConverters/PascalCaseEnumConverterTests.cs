using PassWinmenu.Configuration;
using PassWinmenu.Configuration.TypeConverters;
using Shouldly;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace PassWinmenuTests.Configuration.TypeConverters;

public class PascalCaseEnumConverterTests
{
	[Theory]
	[InlineData("add-password", HotkeyAction.AddPassword)]
	[InlineData("decrypt-password", HotkeyAction.DecryptPassword)]
	[InlineData("Decrypt-Metadata", HotkeyAction.DecryptMetadata)]
	[InlineData("DecryptMetadata", HotkeyAction.DecryptMetadata)]
	[InlineData("decryptMetadata", HotkeyAction.DecryptMetadata)]
	[InlineData("decrypt_metadata", HotkeyAction.DecryptMetadata)]
	[InlineData("Decrypt_Metadata", HotkeyAction.DecryptMetadata)]
	public void Deserialize_ValidInput_DeserializesCorrectly(string value, HotkeyAction expected)
	{
		var des = GetDeserialiser();

		var obj = des.Deserialize<WrapperObject>("Action: " + value);

		obj.Action.ShouldBe(expected);
	}

	[Theory]
	[InlineData("text")]
	[InlineData("decryptmetadata")]
	public void Deserialize_InvalidInput_ThrowsException(string action)
	{
		var des = GetDeserialiser();

		Should.Throw<YamlException>(() => des.Deserialize<WrapperObject>("Action: " + action));
	}

	private class WrapperObject
	{
		public HotkeyAction Action { get; init; }
	}

	private static IDeserializer GetDeserialiser()
	{
		return new DeserializerBuilder()
			.WithTypeConverter(new PascalCaseEnumConverter<HotkeyAction>())
			.Build();
	}
}
