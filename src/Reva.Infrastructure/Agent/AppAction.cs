namespace Reva.Infrastructure.Agent;

public enum AppActionKind
{
    Navigate,
    OpenDocument,
    GotoPage,
    Highlight,
    Refresh,
    SetFilter,
    Toast,
    Progress,
}

public sealed record AppAction(
    AppActionKind Kind,
    string? Route = null,
    string? DocumentId = null,
    int? Page = null,
    string? Target = null,
    string? Filter = null,
    string? Message = null,
    double? Progress = null);

public interface IAppActionBus
{
    IObservable<AppAction> Actions { get; }

    void Publish(AppAction action);
}

public sealed class AppActionBus : IAppActionBus, IObservable<AppAction>
{
    private readonly List<IObserver<AppAction>> _observers = new();
    private readonly Lock _gate = new();

    public IObservable<AppAction> Actions => this;

    public void Publish(AppAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        IObserver<AppAction>[] snapshot;
        lock (_gate)
        {
            snapshot = _observers.ToArray();
        }

        foreach (var observer in snapshot)
        {
            observer.OnNext(action);
        }
    }

    public IDisposable Subscribe(IObserver<AppAction> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_gate)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        return new Subscription(this, observer);
    }

    private void Unsubscribe(IObserver<AppAction> observer)
    {
        lock (_gate)
        {
            _observers.Remove(observer);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly AppActionBus _bus;
        private IObserver<AppAction>? _observer;

        public Subscription(AppActionBus bus, IObserver<AppAction> observer)
        {
            _bus = bus;
            _observer = observer;
        }

        public void Dispose()
        {
            var observer = _observer;
            if (observer is null)
            {
                return;
            }

            _observer = null;
            _bus.Unsubscribe(observer);
        }
    }
}
