﻿using System.Threading.Tasks;
using Dafda.Producing;

namespace Dafda.Tests.TestDoubles
{
    public class KafkaProducerSpy : IKafkaProducer
    {
        public Task Produce(OutgoingMessage outgoingMessage)
        {
            LastMessage = outgoingMessage;
            return Task.CompletedTask;
        }

        public OutgoingMessage LastMessage { get; private set; }

        public void Dispose()
        {
        }
    }
}