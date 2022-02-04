using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordleLib
{
    public static class WordleSim
    {
        /// <summary>
        /// Simulates the decision making process of Wordle.
        /// </summary>
        public static RuleColor[] Sim(string guess, string answer)
        {
            // It's assumed that guess and answer are the same length.

            var colors = new RuleColor[guess.Length];

            // Set all to yellow
            for (int i = 0; i < colors.Length; i++)
                colors[i] = RuleColor.YELLOW;

            // Set green - for the perfect matches
            for (int i = 0; i < colors.Length; i++)
                if (guess[i] == answer[i])
                    colors[i] = RuleColor.GREEN;

            // Set grey - for characters that are not in answer at all
            for (int i = 0; i < colors.Length; i++)
                if (guess[i] != answer[i])
                {
                    if (answer.IndexOf(guess[i]) == -1)
                        colors[i] = RuleColor.GREY;
                }


            // Handle repeated characters - some "yellow"
            // need to be turned into black due to repeat
            // character rules that are not spelled out clearly
            // by Wordle

            // Mark green characters as "taken"
            bool[] taken = new bool[guess.Length];

            for (int i = 0; i < taken.Length; i++)
                if (colors[i] == RuleColor.GREEN)
                    taken[i] = true;

            // For each yellow character in "guess", find a
            // pair in "answer" that is not "taken"
            for (int i = 0; i < taken.Length; i++)
                if (colors[i] == RuleColor.YELLOW)
                {
                    char c = guess[i];

                    // Find a pairing character in "answer", that is
                    // not "taken".
                    bool pair_found = false;

                    for(int j = 0; j < answer.Length; j++)
                        if (answer[j] == c && taken[j] == false)
                        {
                            // A pair for 'c' has been found
                            pair_found = true;
                            taken[j] = true;
                        }

                    // If the yellow character cannot find a pair in the 
                    // "answer", change Yellow -> black
                    if (pair_found == false)
                        colors[i] = RuleColor.GREY;
                }

            return colors;
        }


        /// <summary>
        /// Tests the "Sim()". Throws Exception on test failure.
        /// </summary>
        public static void Test_Sim()
        {
            // The "colors2" is something like GYBBB, where G = green,
            // Y = yellow, and B = black / dark / grey
            static void compare(RuleColor[] colors1, string colors2)
            {
                string error = $"Test_Sim failed, where colors2 is '{colors2}'";

                for(int i = 0; i < colors1.Length; i++)
                {
                    // compare colors1[i] vs colors2[i]
                    if (colors1[i] == RuleColor.GREEN && colors2[i] != 'G')
                        throw new Exception(error);

                    if (colors1[i] == RuleColor.YELLOW && colors2[i] != 'Y')
                        throw new Exception(error);

                    if (colors1[i] == RuleColor.GREY && colors2[i] != 'B')
                        throw new Exception(error);
                }
            }

            // 2021-02-04 word test cases
            compare(Sim("trust", "pleat"), "BBBBG");
            compare(Sim("exalt", "pleat"), "YBYYG");

            // test cases from:
            // https://nerdschalk.com/wordle-same-letter-twice-rules-explained-how-does-it-work/
            compare(Sim("opens", "abbey"), "BBYBB");
            compare(Sim("babes", "abbey"), "YYGGB");
            compare(Sim("kebab", "abbey"), "BYGYY");
            compare(Sim("abyss", "abbey"), "GGYBB");

            compare(Sim("algae", "abbey"), "GBBBY");
            compare(Sim("keeps", "abbey"), "BYBBB");
            compare(Sim("orbit", "abbey"), "BBGBB");
            compare(Sim("abate", "abbey"), "GGBBY");

            Console.WriteLine("Test_Sim() completed without errors.");
        }
    }
}
