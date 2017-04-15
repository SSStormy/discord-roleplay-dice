using System;
using System.Collections.Generic;

namespace droll
{
    public partial class Roll
    {
        public class DiceExpr : Expr
        {
            public DiceExpr(int times, int sides, int mod)
            {
                Times = times;
                Sides = sides;
                Mod = mod;
            }

            public int Times { get; }
            public int Sides { get; }
            public int Mod { get; }

            public override IEnumerable<DiceResult> Execute(Random rng)
            {
                yield return new DiceResult(rng, this, Mod);
            }
        }
    }
}