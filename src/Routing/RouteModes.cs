namespace KingdomPipeWrench.Routing
{
    internal static class RouteModes
    {
        public static RouteShape Next(RouteShape shape) => (RouteShape)(((int)shape + 1) % 4);
        public static string Label(RouteShape shape)
            => shape == RouteShape.NearStart ? "Near start" : shape == RouteShape.NearEnd ? "Near end" : shape.ToString();
    }
}
