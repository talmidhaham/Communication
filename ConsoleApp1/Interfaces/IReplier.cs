namespace Communication.Interfaces;

public interface IReplier<out TRequest, in TReply> where TRequest : class where TReply : class
{
    IDisposable SubscribeRequests(Func<TRequest, TReply> handler);
}