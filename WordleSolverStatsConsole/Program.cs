using static System.Console;

using WordleLib;


WriteLine("This program simulates the recommenders to determine their performance.");
WriteLine("Available Recommenders:");
WriteLine("    1. First 5 Matches (what a normal person would do).");
WriteLine("    2. Exhaustive Search (optimized outcome).");
WriteLine();

var choice = Get_Input("Choose a recommender: ");

if (choice == "1")
    sim_First5_Recommender();

else if (choice == "2")
    sim_Exhaustive_Search_Recommender();




string Get_Input(string prompt)
{
    Write(prompt);
    string? input = ReadLine();

    if (input is null)
        throw new Exception("Input cannot be null.");

    return input;
}


/// <summary>
/// Return true if all "colors" are green.
/// </summary>
bool is_guess_successful(RuleColor[] colors)
{
    foreach (var c in colors)
        if (c != RuleColor.GREEN) return false;

    return true;
}


/// <summary>
/// Simulate "First5_Recommender" over the set of all possible answers.
/// </summary>
void sim_First5_Recommender()
{
    var recommender = new First5_Recommender();

    var answers = AnswerList.Clone_AnswerList();
    var num_guesses_list = new List<int>(answers.Length);
    int num_failures = 0;

    foreach(var answer in answers)
    {
        recommender.Reset();
        int num_guesses = 0;

        // Use "raise" for the first guess
        string guess = "raise";
        var colors = WordleSim.Sim(guess, answer);
        num_guesses++;

        while(is_guess_successful(colors) == false)
        {
            recommender.Add_Knowledge(guess, colors);
            var recommendations = recommender.Recommend();

            if (recommendations.Count < 1)
                break;
            else
            {
                guess = recommendations[0].guess;
                colors = WordleSim.Sim(guess, answer);
                num_guesses++;
            }
        }

        if (is_guess_successful(colors) == true)
            num_guesses_list.Add(num_guesses);
        else
        {
            // Failed to guess the "answer"
            WriteLine($"Failed to guess the word {answer}.");
            num_failures++;
        }
    }

    print_statistics(num_guesses_list, num_failures);
}


/// <summary>
/// Simulate "Exhaustive_Search_Recommender" over the set of 
/// all possible answers.
/// </summary>
void sim_Exhaustive_Search_Recommender()
{
    var recommender = new Exhaustive_Search_Recommender();
    recommender.Skip_First_Search = true;

    var answers = AnswerList.Clone_AnswerList();
    var num_guesses_list = new List<int>(answers.Length);
    int num_failures = 0;

    int num_simulations = 0;

    foreach (var answer in answers)
    {
        recommender.Reset();
        int num_guesses = 0;

        // First guess
        var recommendations = recommender.Recommend();
        string guess = recommendations[0].guess;
        var colors = WordleSim.Sim(guess, answer);
        num_guesses++;

        while (is_guess_successful(colors) == false)
        {
            recommender.Add_Knowledge(guess, colors);
            recommendations = recommender.Recommend();

            if (recommendations.Count < 1)
                break;
            else
            {
                guess = recommendations[0].guess;
                colors = WordleSim.Sim(guess, answer);
                num_guesses++;
            }
        }

        if (is_guess_successful(colors) == true)
            num_guesses_list.Add(num_guesses);
        else
        {
            // Failed to guess the "answer"
            WriteLine($"Failed to guess the word {answer}.");
            num_failures++;
        }

        num_simulations++;
        if (num_simulations % 50 == 0)
            WriteLine($"{num_simulations} simulations has been completed.");
    }

    print_statistics(num_guesses_list, num_failures);
}


/// <summary>
/// Print statistics for "num_guesses_list".
/// </summary>
void print_statistics(List<int> num_guesses_list, int num_failures)
{
    WriteLine();
    WriteLine($"Simulation completed. There has been {num_failures} failures.");
    WriteLine();

    int max = num_guesses_list.Max();
    int min = num_guesses_list.Min();

    WriteLine("Max guesses:     " + max);
    WriteLine("Average guesses: " + num_guesses_list.Average().ToString("G3"));
    WriteLine("Min guesses:     " + min);
    WriteLine();

    // Build historgram
    var histogram = new int[max - min + 1];

    foreach (var n in num_guesses_list)
        histogram[n - min]++;

    // Print historgram
    WriteLine("# Guesses".PadRight(12) + "Frequency");

    for (int i = 0; i < histogram.Length; i++)
        WriteLine((min + i).ToString().PadRight(12) + histogram[i]);

    WriteLine("Total: ".PadLeft(12) + histogram.Sum());
}

