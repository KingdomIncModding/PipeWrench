namespace KingdomPipeWrench.Routing
{
    internal static class AvoidanceRules
    {
        public static AvoidanceMode Next(AvoidanceMode mode) => (AvoidanceMode)(((int)mode + 1) % 4);
        public static bool Buildings(AvoidanceMode mode) => mode != AvoidanceMode.Overlap;
        public static bool Overlap(AvoidanceMode mode) => mode != AvoidanceMode.Buildings;
        public static bool Crossings(AvoidanceMode mode) => mode == AvoidanceMode.Strict;
        public static string Label(AvoidanceMode mode)
        {
            switch (mode)
            {
                case AvoidanceMode.Overlap: return "Avoid pipe overlap; crossing allowed";
                case AvoidanceMode.BuildingsAndOverlap: return "Avoid buildings and overlap; crossing allowed";
                case AvoidanceMode.Strict: return "Avoid buildings, overlap and crossing";
                default: return "Avoid buildings";
            }
        }
    }
}
