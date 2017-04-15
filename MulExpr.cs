using System;
using System.Collections.Generic;

namespace droll
{
    public partial class Roll
    {
        public class MulExpr : Expr
        {
            public DiceExpr Dice { get; }
            public int Times { get; }

            public MulExpr(DiceExpr dice, int times)
            {
                Dice = dice;
                Times = times;
            }

            public override IEnumerable<DiceResult> Execute(Random rng)
            {
                for (var i = 0; i < Times; i++)
                    yield return new DiceResult(rng, Dice, Dice.Mod);
            }
        }
    }
}