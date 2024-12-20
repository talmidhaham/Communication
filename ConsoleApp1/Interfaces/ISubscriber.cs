namespace Communication.Interfaces;

public interface ISubscriber<out TMessage> //where TMessage : class
{
    IObservable<TMessage> MessageReceived { get; }
}