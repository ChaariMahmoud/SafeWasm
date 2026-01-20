namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        public PreludeOptions PreludeOptions { get; set; } = new PreludeOptions();

        private void AddPrelude(BoogieAST.BoogieProgram program)
        {
            var opt = PreludeOptions;

            if (opt.Sections.HasFlag(PreludeSection.CoreGlobals))
                AddPreludeCoreGlobals(program);

            if (opt.Sections.HasFlag(PreludeSection.Memory) && opt.EnableMemory)
                AddPreludeMemory(program);

            if (opt.Sections.HasFlag(PreludeSection.Nondet))
                AddPreludeNondet(program);

            if (opt.Sections.HasFlag(PreludeSection.BoolCasts))
                AddPreludeBoolCasts(program);

            if (opt.Sections.HasFlag(PreludeSection.NumericCasts))
                AddPreludeNumericCasts(program);

            if (opt.Sections.HasFlag(PreludeSection.Math))
                AddPreludeMath(program, opt);

            if (opt.Sections.HasFlag(PreludeSection.StackOps))
                AddPreludeStackOps(program);
        }
    }
}
