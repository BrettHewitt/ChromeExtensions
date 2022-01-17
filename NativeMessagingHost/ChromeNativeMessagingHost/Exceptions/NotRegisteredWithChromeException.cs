namespace ChromeNativeMessagingHost.Exceptions
{
    [Serializable]
    public class NotRegisteredWithChromeException : Exception
    {
        public NotRegisteredWithChromeException(string message) : base(message) { }
    }
}
