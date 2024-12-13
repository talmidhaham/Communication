using Communication.Interfaces;

public class InMemorySubscriber<TMessage> : ISubscriber<TMessage> where TMessage : class
    {
        private readonly IObservable<TMessage> _observable;

        public InMemorySubscriber(IObservable<TMessage> observable)
        {
            _observable = observable;
        }

        public IObservable<TMessage> MessageReceived => _observable;
    }