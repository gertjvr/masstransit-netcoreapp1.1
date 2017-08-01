using System;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace Bus
{
    public static class BusExtensions
    {
        public static IRequestClient<TRequest, TResponse> CreateRequestClient<TRequest, TResponse>(this IBus bus, TimeSpan timeout, TimeSpan? ttl = null, Action<SendContext<TRequest>> callback = null)
            where TRequest : class
            where TResponse : class
        {
            var settings = bus.Address.GetHostSettings();
            var sendAddress = settings.Topology.GetDestinationAddress(typeof(TRequest));

            return bus.CreateRequestClient<TRequest, TResponse>(sendAddress, timeout, ttl, callback);
        }
    }

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

                    endpoint.Handler<MyRequest>(async context =>
                    {
                        await context.RespondAsync(new MyResponse {Value = context.Message.Value + 1});
                    });
                });
            });

            bus.Start();

            bus.Publish(new MyMessage{Value = "Hello, World."});

            var client = bus.CreateRequestClient<MyRequest, MyResponse>(TimeSpan.FromSeconds(30));

            var response1 = client.Request(new MyRequest {Value = 0});
            response1.Wait();
            Console.WriteLine(response1.Result.Value);

            var response2 = client.Request(new MyRequest { Value = 1 });
            response2.Wait();
            Console.WriteLine(response2.Result.Value);

            Console.ReadLine();

            bus.Stop();
        }
    }

    public class MyMessage
    {
        public string Value { get; set; }
    }

    public class MyRequest
    {
        public int Value { get; set; }
    }

    public class MyResponse
    {
        public int Value { get; set; }
    }
}
