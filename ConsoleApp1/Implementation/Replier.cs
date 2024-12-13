using Communication.Interfaces;

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