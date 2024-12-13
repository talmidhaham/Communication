using System.Collections.Concurrent;
using Communication.Interfaces;

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