﻿using System;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

namespace RabbitLink.Builders
{
    internal class LinkTopologyBuilder : ILinkTopologyBuilder
    {
        private readonly Link _link;

        private readonly TimeSpan _recoveryInterval;
        private readonly LinkStateHandler<LinkTopologyState> _stateHandler;
        private readonly LinkStateHandler<LinkChannelState> _channelStateHandler;
        private readonly ILinkTopologyHandler _topologyHandler;

        public LinkTopologyBuilder(
            Link link,
            TimeSpan recoveryInterval,
            LinkStateHandler<LinkTopologyState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            ILinkTopologyHandler topologyHandler = null
        )
        {
            _link = link ?? throw new ArgumentNullException(nameof(link));

            _recoveryInterval = recoveryInterval;
            _stateHandler = stateHandler ?? ((old, @new) => { });
            _channelStateHandler = channelStateHandler ?? ((old, @new) => { });
            _topologyHandler = topologyHandler;
        }

        private LinkTopologyBuilder(
            LinkTopologyBuilder prev,
            TimeSpan? recoveryInterval = null,
            LinkStateHandler<LinkTopologyState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            ILinkTopologyHandler topologyHandler = null
        ) : this(
            prev._link,
            recoveryInterval ?? prev._recoveryInterval,
            stateHandler ?? prev._stateHandler,
            channelStateHandler ?? prev._channelStateHandler,
            topologyHandler ?? prev._topologyHandler
        )
        {
        }


        public ILinkTopologyBuilder RecoveryInterval(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than TimeSpan.Zero");

            return new LinkTopologyBuilder(this, recoveryInterval: value);
        }

        public ILinkTopologyBuilder OnStateChange(LinkStateHandler<LinkTopologyState> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkTopologyBuilder(this, stateHandler: handler);
        }

        public ILinkTopologyBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkTopologyBuilder(this, channelStateHandler: handler);
        }

        public ILinkTopologyBuilder Topology(ILinkTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkTopologyBuilder(this, topologyHandler: handler);
        }

        public ILinkTopologyBuilder Topology(LinkTopologyConfigDelegate config, LinkTopologyReadyDelegate ready)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (ready == null)
                throw new ArgumentNullException(nameof(ready));

            return new LinkTopologyBuilder(
                this,
                topologyHandler: new LinkTopologyHandler(config, ready, ex => Task.CompletedTask)
            );
        }

        public ILinkTopologyBuilder Topology(
            LinkTopologyConfigDelegate config,
            LinkTopologyReadyDelegate ready,
            LinkTopologyErrorDelegate error
        )
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (ready == null)
                throw new ArgumentNullException(nameof(ready));

            if (error == null)
                throw new ArgumentNullException(nameof(error));

            return new LinkTopologyBuilder(this, topologyHandler: new LinkTopologyHandler(config, ready, error));
        }

        public ILinkTopology Build()
        {
            if (_topologyHandler == null)
                throw new InvalidOperationException("Queue must be set");

            var config = new LinkTopologyConfiguration(
                _recoveryInterval,
                _stateHandler,
                _topologyHandler
            );

            return new LinkTopology(_link.CreateChannel(_channelStateHandler, config.RecoveryInterval));
        }
    }
}