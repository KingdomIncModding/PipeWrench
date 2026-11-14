namespace KingdomPipeWrench.Interaction
{
    internal static class PipeToolVisibility
    {
        public static bool Show(bool enabled, bool workspace, bool menu, bool connecting, bool selected, bool inserting)
            => enabled && workspace && !menu && (connecting || selected || inserting);
    }
}
