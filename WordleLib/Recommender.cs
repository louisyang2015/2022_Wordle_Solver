using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordleLib
{
    public interface IRecommender
    {
        void Add_Knowledge(string word, RuleColor[] color);
        List<(string guess, double score)> Recommend(); // Returns (word, score)

        void Reset(); // needed for simulation
    }



    /// <summary>
    /// This recommender goes down the list of answers and returns
    /// the first five answers that pass the rules.
    /// </summary>
    public class First5_Recommender : IRecommender
    {
        string[] possible_words = AnswerList.Clone_AnswerList();
        Knowledge knowledge = new Knowledge();

        public void Reset()
        {
            AnswerList.Reset_Array(ref possible_words);
            knowledge = new Knowledge();
        }


        public void Add_Knowledge(string word, RuleColor[] colors)
        {
            knowledge.Add(word, colors);
        }


        public List<(string guess, double score)> Recommend()
        {
            var recommendations = new List<(string, double)>();

            for(int i = 0; i < possible_words.Length; i++)
            {
                if (possible_words[i] != "")
                {
                    // Valid "possible_words"
                    if (knowledge.Check(possible_words[i]))
                    {
                        recommendations.Add((possible_words[i], 1));

                        if (recommendations.Count > 5) return recommendations;
                    }
                    else
                        // Set invalid words to ""
                        possible_words[i] = "";
                }
            }

            return recommendations;
        }
    }



    /// <summary>
    /// This recommender looks at all possible out comes to choose
    /// the word that provides the best average outcome.
    /// </summary>
    public class Exhaustive_Search_Recommender : IRecommender
    {
        string[] possible_words = AnswerList.Clone_AnswerList();
        Knowledge knowledge = new Knowledge();


        public void Reset()
        {
            AnswerList.Reset_Array(ref possible_words);
            knowledge = new Knowledge();
        }


        /// <summary>
        /// Look up the result of the first recommendation instead
        /// of computing it.
        /// </summary>
        public bool Skip_First_Search { get; set; }

        // The exhaustive search is an O(n^3) algorithm.
        // There are "n_1" guesses possible. Each guess has
        // a "n_2" possible outcomes, where the answer != guess.
        // For each of these answers, there are "n_3" comparisons 
        // to see how the list of "possible_words" is trimmed.
        //
        // The first search is the most expensive, and the result
        // will always be the same for a given "possible_words" list.
        // This result will only change if the Wordle's answer
        // list changes.
        readonly int possible_words_start_size;
        const string first_guess = "raise";
        const double first_score = 61.03;


        public Exhaustive_Search_Recommender()
        {
            possible_words_start_size = possible_words.Length;
        }


        public void Add_Knowledge(string word, RuleColor[] color)
        {
            knowledge.Add(word, color);
        }


        /// <summary>
        /// For the combination (guess, answer), a certain rule will
        /// be generated. Using this new rule, return the number of 
        /// words that will pass.
        /// </summary>
        int compute_score(string guess, string answer)
        {
            var colors = WordleSim.Sim(guess, answer);

            var k = knowledge.Clone();
            k.Add(guess, colors);

            return k.Count_Pass(possible_words);
        }


        #region Old Recommendation approach - full counting

        // Code for old recommendation approach below. This is
        // retained because this logic is more direct.
        // The old approach does a complete count, giving a complete
        // result, but is slower.
        // The new approach only does a partial count, with the goal
        // of finding only the minimum branch.

        /// <summary>
        /// Assuming "possible_words[guess_index]" is being used as the
        /// guess, going over all possible answers, return the average
        /// length of a new "possible_words".
        /// </summary>
        double compute_score(int guess_index)
        {
            string guess = possible_words[guess_index];

            // Go over all possible answers to the "guess"
            // and compute the total score (total words that can pass)
            long total = 0;
            int num_answers = 0;

            for (int i = 0; i < possible_words.Length; i++)
            {
                if (possible_words[i] != "" && i != guess_index)
                {
                    total += compute_score(guess, possible_words[i]);
                    num_answers++;
                }
            }

            // cast to double to avoid integer division
            return ((double)total / (double)num_answers);
        }


        static int compare_recommendations((string word, double score) r1,
            (string word, double score) r2)
        {
            var diff = r1.score - r2.score;

            if (diff > 0) return 1;
            else if (diff < 0) return -1;
            return 0;
        }


        /// <summary>
        /// This is the first version of "Recommend()". It's retained
        /// here because it's easier to understand. This version
        /// will do a full counting. This gives the complete result, 
        /// but it is the slowest.
        /// The next version places an upper limit on the counting,
        /// so to abort the counting earlier.
        /// </summary>
        public List<(string, double)> Recommend_version_0()
        {
            // Use "knowledge" to disqualify "possible_words"
            // Track how many possibilities remain.
            int count = 0;

            for (int i = 0; i < possible_words.Length; i++)
                if (possible_words[i] != "")
                {
                    // Look at non-empty strings only
                    if (knowledge.Check(possible_words[i]) == false)
                        // All words that cannot pass the tests should 
                        // be set to empty strings
                        possible_words[i] = "";
                    else
                        // possible_words[i] remain possible
                        count++;
                }

            // Compute score for each recommendation
            var recommendations = new List<(string, double)>(count);

            for (int i = 0; i < possible_words.Length; i++)
                if (possible_words[i] != "")
                {
                    string guess = possible_words[i];
                    double score = compute_score(i);
                    recommendations.Add((guess, score));
                }

            // Sort recommendations
            recommendations.Sort(Exhaustive_Search_Recommender.compare_recommendations);

            return recommendations;
        }

        #endregion


        /// <summary>
        /// Assuming "possible_words[guess_index]" is being used as the
        /// guess, going over all possible answers, return the number
        /// of words that will pass. There is a maximum limit. If
        /// the counting pass the maximum limit, the counting
        /// is terminated, and the result is not valid.
        /// </summary>
        /// <param name="guess_index">The guess is stored at 
        ///         possible_words[guess_index]</param>
        /// <param name="max">The counting will continue until the 
        ///         number of passes hit this maximum.</param>
        /// <returns>(bool valid, long pass_count) - where "valid" is true if 
        ///         the "pass_count" is valid. When "valid" is false,
        ///         the counting has terminated early due to pass_count
        ///         being already higher than "max". </returns>
        (bool, long) compute_pass_below_max(int guess_index, long max)
        {
            string guess = possible_words[guess_index];

            // Go over all possible answers to the "guess"
            // and compute the total score (total words that can pass)
            bool valid = false;
            long pass_count = 0;

            for (int i = 0; i < possible_words.Length; i++)
            {
                if (possible_words[i] != "" && i != guess_index)
                {
                    pass_count += compute_score(guess, possible_words[i]);

                    // Quit early if the "pass_count" is already higher
                    // than target.
                    if (pass_count > max)
                        return (valid, pass_count);
                }
            }

            // If code reach here, then the count is completed, and the
            // "pass_count" result is valid.
            valid = true;
            return (valid, pass_count);
        }


        static int compare_recommendations((string word, long pass_count)r1, 
            (string word, long pass_count) r2)
        {
            var diff = r1.pass_count - r2.pass_count;

            if (diff > 0) return 1;
            else if (diff < 0) return -1;
            return 0;
        }
                        

        public List<(string guess, double score)> Recommend()
        {
            // Use "knowledge" to disqualify "possible_words"
            // Track how many possibilities remain.
            int possible_words_count = 0;

            for (int i = 0; i < possible_words.Length; i++)
                if (possible_words[i] != "")
                {
                    // Look at non-empty strings only
                    if (knowledge.Check(possible_words[i]) == false)
                        // All words that cannot pass the tests should 
                        // be set to empty strings
                        possible_words[i] = "";
                    else
                        // possible_words[i] remain possible
                        possible_words_count++;
                }


            // Short cut for the first (and longest compute time) recommendation
            if (Skip_First_Search)
            {
                if (possible_words_count == possible_words_start_size)
                {
                    var return_val2 = new List<(string, double)>();
                    return_val2.Add((first_guess, first_score));
                    return return_val2;
                }
            }


            // Allocate list for "recommendations".
            var recommendations = new List<(string guess, long pass_count)>();

            // Compute score for each recommendation
            long min_pass_count_so_far = long.MaxValue;

            for (int i = 0; i < possible_words.Length; i++)
                if (possible_words[i] != "")
                {
                    

                    (bool valid, long count) = compute_pass_below_max(i, 
                                                    min_pass_count_so_far);

                    if (valid)
                    {
                        min_pass_count_so_far = count;
                        string guess = possible_words[i];

                        recommendations.Add((guess, count));
                    }
                }

            // Sort recommendations
            recommendations.Sort(Exhaustive_Search_Recommender.compare_recommendations);

            // Return just the top recommendation
            var return_val = new List<(string, double)>();

            if (recommendations.Count > 0)
            {
                string guess = recommendations[0].guess;

                // The "possible_words" list has "possible_words_count"
                // valid words in it. Of these words, one word is the
                // guess, and the other word is the answer.
                //
                // The average size of the future list:
                double score;

                if (possible_words_count > 1)
                    // cast to double to avoid integer division
                    score = (double)(recommendations[0].pass_count) / (double)(possible_words_count - 1);
                else
                    score = recommendations[0].pass_count;

                return_val.Add((guess, score));
            }

            return return_val;
        }
    }

}
