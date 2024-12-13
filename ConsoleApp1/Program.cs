using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Communication.Interfaces;

namespace Communication
{

    // Example Console Application
    public static class Program
    {
        public static async Task Main()
        {
            // In-memory pub-sub setup
            var requestPublisher = new InMemoryPublisher<Request>();
            var replyPublisher = new InMemoryPublisher<Reply>();

            var requestSubscriber = new InMemorySubscriber<Request>(requestPublisher.GetObservable());
            var replySubscriber = new InMemorySubscriber<Reply>(replyPublisher.GetObservable());

            // DI setup for requester and replier
            var requester = new Requester<Request, Reply>(requestPublisher, replySubscriber);
            var replier = new Replier<Request, Reply>(requestSubscriber, replyPublisher);

            // Replier logic
            replier.SubscribeRequests(request => new Reply
            {
                RequestId = request.RequestId,
                Message = $"Reply to {request.Message}"
            });

            // Example request-response
            Console.WriteLine($"Request Message: Hello");
            var reply = await requester.Request(new Request { Message = "Hello" });

            Console.WriteLine($"Received reply: {reply.Message}");


                       Console.WriteLine($"Request Message: How are you");
            var reply2 = await requester.Request(new Request { Message = "How are you" });

            Console.WriteLine($"Received reply: {reply2.Message}");
        }
    }

    // Example Request and Reply classes
    public class Request
    {
        public Guid RequestId { get; set; }
        public string Message { get; set; }
    }

    public class Reply
    {
        public Guid RequestId { get; set; }
        public string Message { get; set; }
    }
}
