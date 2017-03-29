using System;
using System.Collections.Generic;

namespace droll
{
    public partial class Roll
    {
        public class DiceExpr : Expr
        {
            public DiceExpr(int times, int sides)
            {
                Times = times;
                Sides = sides;
            }

            public int Times { get; }
            public int Sides { get; }

            public override IEnumerable<DiceResult> Execute(Random rng)
            {
                yield return new DiceResult(rng, this);
            }
        }
    }
}