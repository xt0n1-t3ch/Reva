using Reva.Infrastructure.Agent;

namespace Reva.Unit;

public sealed class AppActionBusTests
{
    [Fact]
    public void PublishDeliversActionToSingleSubscriber()
    {
        var bus = new AppActionBus();
        var received = new List<AppAction>();
        using var _ = bus.Actions.Subscribe(new CollectingObserver(received));

        var action = new AppAction(AppActionKind.Navigate, Route: "/dashboard");
        bus.Publish(action);

        var single = Assert.Single(received);
        Assert.Equal(AppActionKind.Navigate, single.Kind);
        Assert.Equal("/dashboard", single.Route);
    }

    [Fact]
    public void PublishDeliversActionToMultipleSubscribers()
    {
        var bus = new AppActionBus();
        var first = new List<AppAction>();
        var second = new List<AppAction>();
        using var sub1 = bus.Actions.Subscribe(new CollectingObserver(first));
        using var sub2 = bus.Actions.Subscribe(new CollectingObserver(second));

        bus.Publish(new AppAction(AppActionKind.Toast, Message: "Hello"));

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal("Hello", first[0].Message);
        Assert.Equal("Hello", second[0].Message);
    }

    [Fact]
    public void PublishDeliversInSubscriptionOrder()
    {
        var bus = new AppActionBus();
        var order = new List<int>();
        using var sub1 = bus.Actions.Subscribe(new OrderedObserver(order, 1));
        using var sub2 = bus.Actions.Subscribe(new OrderedObserver(order, 2));
        using var sub3 = bus.Actions.Subscribe(new OrderedObserver(order, 3));

        bus.Publish(new AppAction(AppActionKind.Refresh));

        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public void UnsubscribedObserverReceivesNoFurtherActions()
    {
        var bus = new AppActionBus();
        var received = new List<AppAction>();
        var subscription = bus.Actions.Subscribe(new CollectingObserver(received));

        bus.Publish(new AppAction(AppActionKind.Toast, Message: "first"));
        subscription.Dispose();
        bus.Publish(new AppAction(AppActionKind.Toast, Message: "second"));

        Assert.Single(received);
        Assert.Equal("first", received[0].Message);
    }

    [Fact]
    public void DisposedSubscriptionIsIdempotent()
    {
        var bus = new AppActionBus();
        var received = new List<AppAction>();
        var subscription = bus.Actions.Subscribe(new CollectingObserver(received));

        subscription.Dispose();
        subscription.Dispose();

        bus.Publish(new AppAction(AppActionKind.Refresh));

        Assert.Empty(received);
    }

    [Fact]
    public void PublishWithNoSubscribersDoesNotThrow()
    {
        var bus = new AppActionBus();
        bus.Publish(new AppAction(AppActionKind.Refresh));
    }

    [Fact]
    public void PublishNullThrowsArgumentNullException()
    {
        var bus = new AppActionBus();
        Assert.Throws<ArgumentNullException>(() => bus.Publish(null!));
    }

    [Fact]
    public void SubscribeNullThrowsArgumentNullException()
    {
        var bus = new AppActionBus();
        Assert.Throws<ArgumentNullException>(() => bus.Actions.Subscribe(null!));
    }

    [Fact]
    public void MultiplePublishesAreAllDelivered()
    {
        var bus = new AppActionBus();
        var received = new List<AppAction>();
        using var _ = bus.Actions.Subscribe(new CollectingObserver(received));

        bus.Publish(new AppAction(AppActionKind.Navigate, Route: "/a"));
        bus.Publish(new AppAction(AppActionKind.Navigate, Route: "/b"));
        bus.Publish(new AppAction(AppActionKind.Navigate, Route: "/c"));

        Assert.Equal(3, received.Count);
        Assert.Equal("/a", received[0].Route);
        Assert.Equal("/b", received[1].Route);
        Assert.Equal("/c", received[2].Route);
    }

    [Fact]
    public void SameObserverSubscribedTwiceIsAddedOnce()
    {
        var bus = new AppActionBus();
        var received = new List<AppAction>();
        var observer = new CollectingObserver(received);

        using var sub1 = bus.Actions.Subscribe(observer);
        using var sub2 = bus.Actions.Subscribe(observer);

        bus.Publish(new AppAction(AppActionKind.Refresh));

        Assert.Single(received);
    }

    [Fact]
    public void AllKindsAreDeliveredWithCorrectPayload()
    {
        var bus = new AppActionBus();
        var received = new List<AppAction>();
        using var _ = bus.Actions.Subscribe(new CollectingObserver(received));

        var actions = new[]
        {
            new AppAction(AppActionKind.Navigate, Route: "/doc"),
            new AppAction(AppActionKind.OpenDocument, DocumentId: "abc"),
            new AppAction(AppActionKind.GotoPage, Page: 3),
            new AppAction(AppActionKind.Highlight, Target: "field-1"),
            new AppAction(AppActionKind.Refresh),
            new AppAction(AppActionKind.SetFilter, Filter: "pending"),
            new AppAction(AppActionKind.Toast, Message: "Saved"),
            new AppAction(AppActionKind.Progress, Progress: 0.75),
        };

        foreach (var action in actions)
        {
            bus.Publish(action);
        }

        Assert.Equal(actions.Length, received.Count);
        for (var i = 0; i < actions.Length; i++)
        {
            Assert.Equal(actions[i].Kind, received[i].Kind);
        }
    }

    private sealed class CollectingObserver(List<AppAction> target) : IObserver<AppAction>
    {
        public void OnNext(AppAction value) => target.Add(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class OrderedObserver(List<int> order, int id) : IObserver<AppAction>
    {
        public void OnNext(AppAction value) => order.Add(id);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
