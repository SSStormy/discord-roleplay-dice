using System;
using System.Collections.Generic;

namespace droll
{
    public partial class Roll
    {
        public class DiceResult
        {
            public DiceResult(Random rng, DiceExpr dice)
            {
                Dice = dice;

                var rolls = new int[dice.Times];
                for (var i = 0; i < dice.Times; i++)
                    rolls[i] = rng.Next(1, dice.Sides + 1);

                Rolls = rolls;
            }

            public DiceExpr Dice { get; }
            public IReadOnlyList<int> Rolls { get; }
        }
    }
}