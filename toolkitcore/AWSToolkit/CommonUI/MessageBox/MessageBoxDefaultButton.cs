namespace Amazon.AWSToolkit.CommonUI.MessageBox
{
    /// <summary>
    /// Indicates the button that should be selected by default in a Message Box
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox"/>
    public enum MessageBoxDefaultButton
    {
        Button1 = 0, // 0x00000000L
        Button2 = 256, // 0x00000100L
        Button3 = 512, // 0x00000200L
        Button4 = 768, // 0x00000300L
        /// <summary>
        /// Used unless a button other than Button1 is indicated
        /// </summary>
        DefaultButton = Button1,
    }
}