using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Selection lifetime independent of pointer position and of the native drag manager.</summary>
    internal sealed class PipeEditState
    {
        public bool Active { get; private set; }
        public RouteShape Shape { get; private set; } = RouteShape.Center;
        public int Bends { get; private set; } = 2;
        private int protectedFrame;
        public void Begin(int frame, RouteShape shape, int bends)
        { Active = true; Shape = shape; Bends = RouteBendPlanner.Normalize(bends); protectedFrame = frame; }
        public void Touch(int frame) => protectedFrame = frame;
        public bool CanReadKeys(int frame) => Active && frame > protectedFrame;
        public bool ShouldDismiss(int frame, bool clicked, bool overGrabber, bool draggingGrabber)
            => CanReadKeys(frame) && clicked && !overGrabber && !draggingGrabber;
        public void Cycle() { if (Active) Shape = RouteModes.Next(Shape); }
        public void Adjust(int direction) { if (Active) Bends = RouteBendPlanner.Normalize(Bends + direction * 2); }
        public void Dismiss() => Active = false;
    }
}
