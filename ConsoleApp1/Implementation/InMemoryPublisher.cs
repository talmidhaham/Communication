using System.Reactive.Subjects;
using Communication.Interfaces;

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