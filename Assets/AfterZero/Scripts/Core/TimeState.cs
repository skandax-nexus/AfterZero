using System;

namespace AfterZero
{
    /// <summary>The three coexisting states of Veyra.</summary>
    public enum TimeState
    {
        Past = 0,
        Present = 1,
        Future = 2
    }

    /// <summary>Bitmask of states, used for "exists in" / "allowed in" checks.</summary>
    [Flags]
    public enum TimeMask
    {
        None = 0,
        Past = 1 << 0,
        Present = 1 << 1,
        Future = 1 << 2,
        All = Past | Present | Future
    }

    public static class TimeStateExtensions
    {
        public static TimeMask ToMask(this TimeState state) => (TimeMask)(1 << (int)state);

        public static bool Contains(this TimeMask mask, TimeState state) => (mask & state.ToMask()) != 0;

        /// <summary>Label plus glyph so the state is never communicated by colour alone.</summary>
        public static string Label(this TimeState state)
        {
            switch (state)
            {
                case TimeState.Past: return "<< PAST";
                case TimeState.Present: return "[ PRESENT ]";
                default: return "FUTURE >>";
            }
        }
    }
}
