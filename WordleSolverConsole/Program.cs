using static System.Console;

using WordleLib;



WriteLine("This program helps you solve the Wordle puzzles.");
WriteLine("Available Recommenders:");
WriteLine("    1. First 5 Matches (what a normal person would do).");
WriteLine("    2. Exhaustive Search (optimized outcome).");
WriteLine();

var choice = Get_Input("Choose a recommender: ");


IRecommender recommender;

if (choice == "1")
    recommender = new First5_Recommender();

else if (choice == "2")
{
    var r = new Exhaustive_Search_Recommender();
    r.Skip_First_Search = true;
    recommender = r;
}

else
    return;


while (true)
{
    // Get recommendations
    var recommendations = recommender.Recommend();

    WriteLine();
    foreach ((var r, var score) in recommendations)
        WriteLine($"recommendation: {r}    score: {score}");


    // Quit if there is no recommendation 
    if (recommendations.Count == 0) return;

    // Get Wordle feedback
    WriteLine();
    var word = Get_Input("Enter word: ").ToLower();
    var colors = Get_Input("Enter colors (G, Y, B): ").ToUpper();

    // Quit if the match has been found
    bool all_Gs = true;
    foreach (char c in colors)
        if (c != 'G')
        {
            all_Gs = false;
            break;
        }

    if (all_Gs) return;

    recommender.Add_Knowledge(word, String_to_RuleColor(colors));
}



string Get_Input(string prompt)
{
    Write(prompt);
    string? input = ReadLine();

    if (input is null)
        throw new Exception("Input cannot be null.");

    return input;
}


RuleColor[] String_to_RuleColor(string colors)
{
    var rc = new RuleColor[colors.Length];

    for (int i = 0; i < colors.Length; i++)
    {
        if (colors[i] == 'G')
            rc[i] = RuleColor.GREEN;
        else if (colors[i] == 'Y')
            rc[i] = RuleColor.YELLOW;
        else if (colors[i] == 'B')
            rc[i] = RuleColor.GREY;
        else
            throw new Exception($"The color string '{colors}' can only contain 'G', 'Y', or 'B' characters.");
    }

    return rc;
}



