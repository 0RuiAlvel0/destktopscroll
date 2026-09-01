namespace DesktopScroll.Tests;

public sealed class KeyBindingResolverTests
{
    [Theory]
    [InlineData("Win+Enter", Keys.Enter, HotkeyModifiers.Win)]
    [InlineData(" ctrl + windows + enter ", Keys.Enter, HotkeyModifiers.Control | HotkeyModifiers.Win)]
    [InlineData("Shift+Alt+F12", Keys.F12, HotkeyModifiers.Shift | HotkeyModifiers.Alt)]
    public void TryParseHotkey_ParsesConfiguredKeyAndModifiers(string value, Keys expectedKey, HotkeyModifiers expectedModifiers)
    {
        var parsed = KeyBindingResolver.TryParseHotkey(value, out var key, out var modifiers);

        Assert.True(parsed);
        Assert.Equal(expectedKey, key);
        Assert.Equal(expectedModifiers, modifiers);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Ctrl+Win")]
    [InlineData("Win+NotAKey")]
    public void TryParseHotkey_RejectsMissingOrInvalidKeys(string value)
    {
        var parsed = KeyBindingResolver.TryParseHotkey(value, out var key, out var modifiers);

        Assert.False(parsed);
        Assert.Equal(Keys.None, key);
    }

    [Theory]
    [InlineData("w", Keys.W)]
    [InlineData(" Left ", Keys.Left)]
    public void TryParseSingleKey_ParsesKeyIgnoringCaseAndWhitespace(string value, Keys expectedKey)
    {
        var parsed = KeyBindingResolver.TryParseSingleKey(value, out var key);

        Assert.True(parsed);
        Assert.Equal(expectedKey, key);
    }
}