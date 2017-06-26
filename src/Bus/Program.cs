using System;
using MassTransit;

namespace Bus
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = MassTransit.Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri("rabbitmq://rabbit.localtest.me/"), h => {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint(host, "my_queue", endpoint =>
                {
                    endpoint.Handler<MyMessage>(async context =>
                    {
                        await Console.Out.WriteLineAsync($"Received: {context.Message.Value}");
                    });
                });
            });

            bus.Start();

            bus.Publish(new MyMessage{Value = "Hello, World."});

            Console.ReadLine();

            bus.Stop();
        }
    }

    class MyMessage
    {
        public string Value { get; set; }
    }
}
