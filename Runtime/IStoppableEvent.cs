namespace DGP.EventBus
{
    public interface IStoppableEvent
    {
        bool StopPropagation { get; }
    }
}