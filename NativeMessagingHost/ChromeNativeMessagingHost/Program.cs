using ChromeNativeMessagingHost;
using ChromeNativeMessagingHost.Messages;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Pipes;

ChromeServerHost Host;
bool ChromeConnectionStable = true;
NamedPipeServerStream PipeServer;

ManualResetEvent ResetEvent = null;
ManualResetEvent ProcessFinishedEvent;

Host = new ChromeServerHost();
Host.LostConnectionToChrome += Host_LostConnectionToChrome;
Thread serverThread = new Thread(ServerThread);
serverThread.Start();
Thread.Sleep(250);

Host.Listen();

void Host_LostConnectionToChrome(object sender, EventArgs e)
{
    ChromeConnectionStable = false;
    ResetEvent.Set();
}

void ProcessConnection(IAsyncResult result)
{
    try
    {
        PipeServer.EndWaitForConnection(result);

        var serverCommunication = new ServerCommunication(PipeServer);
        serverCommunication.SendMessage("dataDyne Chrome Server");

        var command = serverCommunication.ReadMessage();
        var responseConfirmation = new ResponseConfirmation(command);

        var waitForReponse = true;
        var processResponse = true;
        JObject reply = null;

        Host.MessageReceived += (s, a) =>
        {
            if (processResponse)
            {
                reply = a.Data;
            }

            waitForReponse = false;
        };

        Host.SendMessage(responseConfirmation.GetJObject());

        var sw = new Stopwatch();
        sw.Start();
        while (waitForReponse)
        {
            if (sw.ElapsedMilliseconds >= (10 * 1000))
            {
                waitForReponse = false;
                processResponse = false;
            }
        }

        var response =string.Empty;
        if (processResponse && reply != null)
        {
            response = reply.ToString();
        }
        else
        {
            response = "{\"text\": \"Failed to communicate with chrome extension\"}";
        }

        PipeServer.RunAsClient(() => serverCommunication.SendMessage(response));
    }
    catch (IOException e)
    {
        Console.WriteLine("ERROR: {0}", e.Message);
    }

    PipeServer.Close();

    ProcessFinishedEvent.Set();
}

void ServerThread(object data)
{
    ResetEvent = new ManualResetEvent(false);

    while (true)
    {
        PipeServer = new NamedPipeServerStream("dataDyneChromeServerPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        ProcessFinishedEvent = new ManualResetEvent(false);
        
        PipeServer.BeginWaitForConnection(ProcessConnection, PipeServer);

        var result = WaitHandle.WaitAny(new[] { ResetEvent, ProcessFinishedEvent });

        if (!ChromeConnectionStable || result == 0)
        {
            PipeServer.Close();
            break;
        }
    }
}