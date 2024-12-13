namespace Communication.Interfaces;

public interface IRequester<in TRequest, TReply> where TRequest : class where TReply : class
{
    Task<TReply> Request(TRequest message, CancellationToken cancellationToken = default);
}