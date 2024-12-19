namespace MyPoeLikeGame
{
    using R3;

    public class IEvent
    {
        public string gameObjectId;
    }

    public static class Reactive
    {
        public static Subject<IEvent> events = new();
    }
}