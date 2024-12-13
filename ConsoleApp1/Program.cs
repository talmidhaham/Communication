using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Communication.Interfaces;

namespace Communication
{
    public class Requester<TRequest, TReply> : IRequester<TRequest, TReply>
        where TRequest : class
        where TReply : class
    {
        private readonly IPublisher<TRequest> _publisher;
        private readonly ISubscriber<TReply> _subscriber;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<TReply>> _pendingRequests;

        public Requester(IPublisher<TRequest> publisher, ISubscriber<TReply> subscriber)
        {
            _publisher = publisher;
            _subscriber = subscriber;
            _pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<TReply>>();

            _subscriber.MessageReceived.Subscribe(OnReplyReceived);
        }

        public async Task<TReply> Request(TRequest message, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<TReply>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_pendingRequests.TryAdd(requestId, tcs))
                throw new InvalidOperationException("Failed to add the request to the pending list.");

            try
            {
                // Attach requestId to the message (requires that TRequest supports it)
                dynamic dynamicMessage = message;
                dynamicMessage.RequestId = requestId;

                await _publisher.Publish(message, cancellationToken);

                using (cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        private void OnReplyReceived(TReply reply)
        {
            // Extract requestId from the reply (requires that TReply supports it)
            dynamic dynamicReply = reply;
            Guid requestId = dynamicReply.RequestId;

            if (_pendingRequests.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(reply);
            }
        }
    }

    public class Replier<TRequest, TReply> : IReplier<TRequest, TReply>
        where TRequest : class
        where TReply : class
    {
        private readonly ISubscriber<TRequest> _subscriber;
        private readonly IPublisher<TReply> _publisher;

        public Replier(ISubscriber<TRequest> subscriber, IPublisher<TReply> publisher)
        {
            _subscriber = subscriber;
            _publisher = publisher;
        }

        public IDisposable SubscribeRequests(Func<TRequest, TReply> handler)
        {
            return _subscriber.MessageReceived.Subscribe(async request =>
            {
                try
                {
                    var reply = handler(request);

                    // Attach requestId to the reply (requires that TReply supports it)
                    dynamic dynamicRequest = request;
                    dynamic dynamicReply = reply;
                    dynamicReply.RequestId = dynamicRequest.RequestId;

                    await _publisher.Publish(reply);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling request: {ex.Message}");
                }
            });
        }
    }

    // Example in-memory implementations of IPublisher and ISubscriber
    public class InMemoryPublisher<TMessage> : IPublisher<TMessage> where TMessage : class
    {
        private readonly Subject<TMessage> _subject = new();

        public Task Publish(TMessage message, CancellationToken cancellationToken = default)
        {
            _subject.OnNext(message);
            return Task.CompletedTask;
        }

        public IObservable<TMessage> GetObservable() => _subject;
    }

    public class InMemorySubscriber<TMessage> : ISubscriber<TMessage> where TMessage : class
    {
        private readonly IObservable<TMessage> _observable;

        public InMemorySubscriber(IObservable<TMessage> observable)
        {
            _observable = observable;
        }

        public IObservable<TMessage> MessageReceived => _observable;
    }

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
