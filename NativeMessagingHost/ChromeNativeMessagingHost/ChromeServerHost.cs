﻿using ChromeNativeMessagingHost.Events;
using ChromeNativeMessagingHost.Exceptions;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace ChromeNativeMessagingHost
{
    public class ChromeServerHost
    {
        public event EventHandler LostConnectionToChrome;

        public event MessageReceivedHandler MessageReceived;

        public string Hostname
        {
            get => "com.datadyne.chromeserver.message";
        }

        private readonly string ManifestPath;
        private const string RegKeyBaseLocation = "SOFTWARE\\Google\\Chrome\\NativeMessagingHosts\\";

        /// <summary>
        /// Creates the Host Object
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="sendConfirmationReceipt"></param>
        public ChromeServerHost()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ManifestPath = Path.Combine(assemblyLocation, Hostname + "-manifest.json");
        }

        /// <summary>
        /// Starts listening for input.
        /// </summary>
        public void Listen()
        {
            if (!IsRegisteredWithChrome()) throw new NotRegisteredWithChromeException(Hostname);
            JObject data;
            while ((data = Read()) != null)
            {
                ProcessReceivedMessage(data);
            }

            if (LostConnectionToChrome != null)
            {
                LostConnectionToChrome(this, EventArgs.Empty);
            }
        }

        private JObject Read()
        {
            var stdin = Console.OpenStandardInput();

            var lengthBytes = new byte[4];
            stdin.Read(lengthBytes, 0, 4);

            var buffer = new char[BitConverter.ToInt32(lengthBytes, 0)];

            using (var reader = new StreamReader(stdin)) while (reader.Peek() >= 0) reader.Read(buffer, 0, buffer.Length);

            return JsonConvert.DeserializeObject<JObject>(new string(buffer));
        }

        /// <summary>
        /// Sends a message to Chrome, note that the message might not be able to reach Chrome if the stdIn / stdOut aren't properly configured (i.e. Process needs to be started by Chrome)
        /// </summary>
        /// <param name="data"></param>
        public void SendMessage(JObject data)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data.ToString(Formatting.None));
            var stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Flush();
        }

        protected void ProcessReceivedMessage(JObject data)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceievedArgs() { Data = data });
            }
        }

        public bool IsRegisteredWithChrome()
        {
            var regHostnameKeyLocation = RegKeyBaseLocation + Hostname;
            var regKey = Registry.CurrentUser.OpenSubKey(regHostnameKeyLocation, true);

            if (regKey != null)
            {
                string keyString = regKey.GetValue("").ToString();
                if (keyString == ManifestPath)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
