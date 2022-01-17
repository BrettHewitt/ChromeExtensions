using Newtonsoft.Json.Linq;

namespace ChromeNativeMessagingHost.Events
{
    public class MessageReceievedArgs : EventArgs
    {
        public JObject Data { get; set; }
    }

    public delegate void MessageReceivedHandler(object sender, MessageReceievedArgs args);
}
