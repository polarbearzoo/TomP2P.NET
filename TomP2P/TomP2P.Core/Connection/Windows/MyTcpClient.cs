﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Core.Storage;
using TomP2P.Extensions;

namespace TomP2P.Core.Connection.Windows
{
    public class MyTcpClient : BaseClient, ITcpClientChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly TcpClient _tcpClient;

        public MyTcpClient(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
        {
            // bind
            _tcpClient = new TcpClient(localEndPoint);
        }

        public Task ConnectAsync(IPEndPoint remoteEndPoint)
        {
            return _tcpClient.ConnectAsync(remoteEndPoint.Address, remoteEndPoint.Port);
        }

        public override async Task SendBytesAsync(byte[] bytes, IPEndPoint senderEp, IPEndPoint receiverEp = null)
        {
            // send bytes
            var recvEp = _tcpClient.Client.RemoteEndPoint;
            await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length);
            Logger.Debug("Sent TCP: Sender {0} --> Recipient {1}. {2} : {3}", senderEp, recvEp,
                Convenient.ToHumanReadable(bytes.Length), Convenient.ToString(bytes));
        }

        public override async Task DoReceiveMessageAsync()
        {
            // TODO find zero-copy way, use same buffer
            // receive bytes
            var bytesRecv = new byte[256];
            var buf = AlternativeCompositeByteBuf.CompBuffer();

            var stream = _tcpClient.GetStream();
            var pieceCount = 0;
            do
            {
                Array.Clear(bytesRecv, 0, bytesRecv.Length);
                buf.Clear();
                int nrBytes;
                try
                {
                    nrBytes = await stream.ReadAsync(bytesRecv, 0, bytesRecv.Length).WithCancellation(CloseToken);
                }
                catch (OperationCanceledException)
                {
                    // the socket has been closed
                    return;
                }
                buf.WriteBytes(bytesRecv.ToSByteArray(), 0, nrBytes);

                var localEp = (IPEndPoint)Socket.LocalEndPoint;
                RemoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;

                var piece = new StreamPiece(buf, localEp, RemoteEndPoint);
                Logger.Debug("[{0}] Received {1}. {2} : {3}", ++pieceCount, piece, Convenient.ToHumanReadable(nrBytes), Convenient.ToString(bytesRecv));

                // execute inbound pipeline, per piece (reset session!)
                if (Session.IsTimedOut)
                {
                    return;
                }
                Session.Read(piece);
                Session.Reset();
            } while (!IsClosed && stream.DataAvailable && !Session.IsTimedOut);
        }

        protected override void DoClose()
        {
            base.DoClose();
            _tcpClient.Close();
        }

        public override string ToString()
        {
            return String.Format("MyTcpClient ({0})", RuntimeHelpers.GetHashCode(this));
        }

        public override Socket Socket
        {
            get { return _tcpClient.Client; }
        }

        public override bool IsUdp
        {
            get { return false; }
        }

        public override bool IsTcp
        {
            get { return true; }
        }
    }
}
