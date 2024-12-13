using System;
using System.Threading;
using System.Threading.Tasks;

namespace Communication
{
    /*
     * The two following interfaces describe a Publish–subscribe pattern (https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern).
     * Generic implementation of the interfaces already registered in the container.
     */
    public interface IPublisher<in TMessage> where TMessage : class
    {
        Task Publish(TMessage message, CancellationToken cancellationToken = default);
    }

    public interface ISubscriber<out TMessage> where TMessage : class
    {
        IObservable<TMessage> MessageReceived { get; }
    }

    /*
    * The two following interfaces describe a Request–response pattern (https://en.wikipedia.org/wiki/Request%E2%80%93response).
    */
    public interface IRequester<in TRequest, TReply> where TRequest : class where TReply : class
    {
        Task<TReply> Request(TRequest message, CancellationToken cancellationToken = default);
    }

    public interface IReplier<out TRequest, in TReply> where TRequest : class where TReply : class
    {
        IDisposable SubscribeRequests(Func<TRequest, TReply> handler);
    }

    /* Your mission is to create a generic implementation of the
     * request-reply interfaces by using the publish-subscribe interfaces.
     * Your class can get whatever dependency they need with the help of DI.
     *
     * We recommend first-able to implement "naive" request-reply
     * and only then handle the matching of a request for an answer.
     *
     * Bonus: Take care of maintaining order between receiving answers and requests.
     *
     * If you need to add a constraint to interfaces\implementation\types, feel free to contact us and request it.
     */
}