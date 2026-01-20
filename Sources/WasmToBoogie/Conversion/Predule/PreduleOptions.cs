using System;

namespace WasmToBoogie.Conversion
{
    [Flags]
    public enum PreludeSection
    {
        None = 0,
        CoreGlobals = 1 << 0,
        Memory = 1 << 1,
        Nondet = 1 << 2,
        BoolCasts = 1 << 3,
        NumericCasts = 1 << 4,
        Math = 1 << 5,
        StackOps = 1 << 6,

        All = CoreGlobals | Memory | Nondet | BoolCasts | NumericCasts | Math | StackOps,
    }

    public sealed class PreludeOptions
    {
        public PreludeSection Sections { get; init; } = PreludeSection.All;

        public bool EnableMemory { get; init; } = true;

        public bool EnableSqrtAxioms { get; init; } = true;
        public bool EnableNearestAxioms { get; init; } = true;
        public bool EnableFloorAxioms { get; init; } = true;

        // Pour abs_real : false = uninterpreted (simple), true = défini par ITE
        public bool DefineAbsWithITE { get; init; } = true;
    }
}
