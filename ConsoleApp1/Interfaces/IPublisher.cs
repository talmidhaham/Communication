namespace Communication.Interfaces;

public interface IPublisher<in TMessage> //where TMessage : class
{
    Task Publish(TMessage message, CancellationToken cancellationToken = default);
}