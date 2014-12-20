﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Sockets
{
    /// <summary>
    /// Synchronous client socket that suspends execution of the client application
    /// until the server returns a response.
    /// </summary>
    public class SyncClient
    {
        private string _hostName = "localhost"; // or IPAddress 127.0.0.1
        private short _serverPort = 5151;

        public byte[] SendBuffer { get; set; }
        public byte[] RecvBuffer { get; set; }

        public void StartTcp()
        {
            try
            {
                Socket client = null;
                IPEndPoint remoteEp = null;
                IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);

                // CONNECT
                // try each address
                foreach (var address in ipHostInfo.AddressList)
                {
                    // create a TCP/IP socket
                    client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        remoteEp = new IPEndPoint(address, _serverPort);
                        client.Connect(remoteEp);
                        break;
                    }
                    catch (SocketException)
                    {
                        // connect failed, try next address
                        client.Close();
                        client = null;
                    }
                }
                if (client == null || remoteEp == null)
                {
                    throw new Exception("Establishing connection failed.");
                }

                // SEND
                try
                {
                    // send the data throug the socket
                    int bytesSent = client.Send(SendBuffer);

                    // shutdown sending on client-side
                    client.Shutdown(SocketShutdown.Send); // TCP-only
                }
                catch (Exception)
                {
                    throw new Exception("Sending failed.");
                }

                // RECEIVE
                try
                {
                    while (true)
                    {
                        int bytesRecv = client.Receive(RecvBuffer); // blocking

                        // exit loop if server indicates shutdown
                        if (bytesRecv == 0)
                        {
                            // shutdown client
                            // TODO sender.Shutdown(SocketShutdown.Send); needed?
                            client.Close();
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Receiving failed.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void StartUdp()
        {
            try
            {
                Socket client = null;
                IPEndPoint remoteEp = null;
                IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);

                // CONNECT
                // try each address
                // TODO not needed for UDP
                foreach (var serverAddress in ipHostInfo.AddressList)
                {
                    // create a UDP/IP socket
                    client = new Socket(serverAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    try
                    {
                        remoteEp = new IPEndPoint(serverAddress, _serverPort);

                        // since response from server is expected, bind to wilcard
                        client.Bind(new IPEndPoint(IPAddress.Any, 0));
                        break;
                    }
                    catch (SocketException)
                    {
                        // connect failed, try next address
                        client.Close();
                        client = null;
                    }
                }
                if (client == null || remoteEp == null)
                {
                    throw new Exception("Establishing connection failed.");
                }

                // SEND
                try
                {
                    // send the data throug the socket
                    int bytesSent = client.SendTo(SendBuffer, remoteEp);
                }
                catch (Exception)
                {
                    throw new Exception("Sending failed.");
                }

                // RECEIVE
                try
                {
                    //while (true)
                    //{
                        var fromEp = new IPEndPoint(remoteEp.Address, 0); // TODO any available port?
                        EndPoint ep = fromEp;
                        int bytesRecv = client.ReceiveFrom(RecvBuffer, ref ep); // blocking

                        // exit loop if server indicates shutdown
                        if (bytesRecv == 0)
                        {
                            // shutdown client
                            client.Close();
                            //break;
                        }
                    //}
                }
                catch (Exception ex)
                {
                    throw new Exception("Receiving failed.", ex);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
