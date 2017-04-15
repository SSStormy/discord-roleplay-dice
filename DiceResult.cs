using System;
using System.Collections.Generic;
using Discord;

namespace droll
{
    public partial class Roll
    {
        public sealed class RollResult
        {
            private readonly DiceExpr _dice;
            public int Result { get; }

            public RollResult(DiceExpr dice, int result)
            {
                _dice = dice;
                Result = result;
            }

            public override string ToString()
            {
                var ret = Result.ToString();

                switch (_dice.Sides)
                {
                    case 20:
                        if (Result == 1 || Result == 20)
                            ret = $"!{ret}!";
                        break;
                    case 100:
                        ret += "%";

                        if (Result == 100 || Result == 1)
                            ret = $"!{ret}!";
                        break;
                }

                return ret;
            }
        }

        public class DiceResult
        {
            public DiceResult(Random rng, DiceExpr dice, int mod)
            {
                Dice = dice;

                var rolls = new RollResult[dice.Times];
                for (var i = 0; i < dice.Times; i++)
                    rolls[i] = new RollResult(dice, rng.Next(1, dice.Sides + 1) + mod);

                Rolls = rolls;
            }

            public DiceExpr Dice { get; }
            public IReadOnlyList<RollResult> Rolls { get; }
        }
    }
}