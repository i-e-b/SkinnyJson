using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SkinnyJson.Unit.Tests.ExampleData
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RabbitMqStatistic
    {
        public bool Durable { get; set; }
        public int Consumers { get; set; }
        public DateTime? IdleSince { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string Type { get; set; }

        public RabbitMqMessageStats? MessageStats { get; set; }
        public RabbitMqMessageDetails MessagesDetails { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RabbitMqMessageDetails
    {
        public double Avg { get; set; }
        public List<TimedSample> Samples { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class TimedSample
    {
        public int Sample { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RabbitMqMessageStats
    {
        public RabbitMqAckDetails AckDetails { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RabbitMqRate
    {
        public double Rate { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RabbitMqAckDetails
    {
        public double AvgRate { get; set; }
    }
}