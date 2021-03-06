﻿using System;
using System.Runtime.CompilerServices;
using NLog;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Connection
{
    public class DropConnectionInboundHandler : BaseInboundHandler, ISharable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly VolatileInteger _counter = new VolatileInteger(0);
        private readonly int _limit;

        public DropConnectionInboundHandler(int limit)
        {
            _limit = limit;
        }

        public override void ChannelActive(ChannelHandlerContext ctx)
        {
            base.ChannelActive(ctx);
            int current;
            if ((current = _counter.IncrementAndGet()) > _limit)
            {
                ctx.Channel.Close();
                Logger.Warn("Dropped connection because {0} > {1} connections active.", current, _limit);
            }
            // fireChannelActive // TODO needed?
        }

        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            _counter.Decrement();
            // fireChannelInactive // TODO needed?
        }

        public override IChannelHandler CreateNewInstance()
        {
            // does not have to be implemeted, this class is ISharable
            throw new System.NotImplementedException();
            //public DropConnectionInboundHandler(int _limit);
        }

        public override string ToString()
        {
            return String.Format("DropConnectionInboundHandler ({0})", RuntimeHelpers.GetHashCode(this));
        }
    }
}
