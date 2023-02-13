/*
    This class retrieves the local IP address.
    The method within this class is not my own
    work, it is taken from:

    https://stackoverflow.com/questions/6803073/get-local-ip-address
*/

using System.Net;
using System.Net.Sockets;

public class IP
{
    public static string Local()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }

        return localIP;
    }
}
