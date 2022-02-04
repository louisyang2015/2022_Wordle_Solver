namespace WordleLib
{
    public enum RuleColor { GREEN, YELLOW, GREY }


    public class Knowledge
    {
        // Wordle word length
        const int LEN = 5;

        // Rule #1 (the green boxes):
        char[] known = new char[LEN];
        // known[3] = 'a' means the 4th letter is 'a'

        // Rule #2A (the yellow and gray boxes):
        HashSet<char>[] not_possible = new HashSet<char>[LEN];
        // not_possible[3] = {'a', 'b'} means the 4th letter cannot be 'a' and 'b'

        // Rule #2B (the yellow box):
        HashSet<char> contains = new HashSet<char>();
        // contains = {'s'} means one (or more) of the unknown letter contain an 's'


        public Knowledge()
        {
            for (int i = 0; i < not_possible.Length; i++)
                not_possible[i] = new HashSet<char>();
        }


        /// <summary>
        /// Return a new "Knowledge" object that's a copy of the
        /// current knowledge object.
        /// </summary>
        public Knowledge Clone()
        {
            var k = new Knowledge();

            // clone "known"
            Array.Copy(known, k.known, known.Length);

            // clone "not_possible"
            for (int i = 0; i < not_possible.Length; i++)
                foreach (var c in not_possible[i])
                    k.not_possible[i].Add(c);

            // clone "contains"
            foreach (var c in contains)
                k.contains.Add(c);

            return k;
        }


        /// <summary>
        /// Add wordle's output to the knowledge base.
        /// </summary>
        /// <param name="word">Must be in lower case.</param>
        public void Add(string word, RuleColor[] colors)
        {
            // Error message prefix.
            string error = $"Failed to add '{word}'. ";

            // check word
            if (word.Length != LEN)
                throw new Exception(error + $"Word length is not {LEN}.");

            // For now no need to check if the "word" exist; assume it's
            // accepted by Wordle

            // check color
            if (colors.Length != LEN)
                throw new Exception(error + $"Color length is not {LEN}.");

            // Check each rule before adding it.
            // The entire {word, color} rule set is viewed as a
            // transaction. Either all rules get added, or none gets added.
            for (int i = 0; i < word.Length; i++)
                check_one_character_rule(word, colors, i);

            // Add rules
            add_green_character_rules(word, colors);
            add_yellow_character_rules(word, colors);
            add_grey_character_rules(word, colors);
        }


        /// <summary>
        /// For the given "guess" and Wordle "colors", check the 
        /// color at "index". The rule needs to not conflict with
        /// the existing rules. If there is an error, throw an
        /// exception.
        /// </summary>
        void check_one_character_rule(string guess, RuleColor[] colors, int index)
        {
            char c = guess[index];
            var color = colors[index];

            string error = $"Failed to add character \"{c} {color}\" at index {index}. ";

            if (color == RuleColor.GREEN)
            {
                // Error if the letter at "index" is already known.
                if ((known[index] == 0) || (known[index] == c))
                {
                    // OK if unkown, or if it's the same character
                }
                else
                    throw new Exception(error + $"The character at index {index} is already known to be '{known[index]}'.");

                // Error if the character is already on the "not_possible" list.
                if (not_possible[index].Contains(c))
                    throw new Exception(error + $"The character {c} is already known to be impossible at index {index}.");
            }
            else if (color == RuleColor.YELLOW)
            {
                // Error if the letter at "index" is already known to be "c".
                if (known[index] == c)
                    throw new Exception(error + $"The character at index {index} is already known to be '{known[index]}'.");

                // Error if all of the "not_possible[]" for unkown characters contain "c"
                bool possible_found = false;

                for(int i = 0; i< not_possible.Length; i++)
                    if (known[i] == 0)
                    {
                        if (not_possible[i].Contains(c) == false)
                            possible_found = true;
                    }

                if (possible_found == false)
                    throw new Exception(error + $"The character {c} is thought to be impossible in the remaining unknown characters.");
            }
            else if (color == RuleColor.GREY)
            {
                // Is 'c' the only occurance?
                // Count how many times 'c' appeared..
                int c_count = 0;
                foreach (var c2 in guess)
                    if (c2 == c)
                    {
                        c_count++;
                        if (c_count > 1)
                            break;
                    }

                if (c_count <= 1)
                {
                    // 'c' is not allowed at all
                    // Error if one of the known letters is already 'c'
                    for (int i = 0; i < known.Length; i++)
                        if (known[i] == c)
                            throw new Exception(error + $"The character '{c}' at index '{index}' conflicts with a known character.");
                }
                else
                {
                    // 'c' is a repeat character
                    // For now just check against known[index].
                    // This does not cover all repeat character cases.
                    if (known[index] == c)
                        throw new Exception(error + $"The character '{c}' at index '{index}' conflicts with a known character.");
                }
                
                // In the current implmentation, rule #2A (if grey) 
                // automatically overrides rule #2B, so there is no 
                // check against the "contains" set.
            }
        }


        void add_green_character_rules(string word, RuleColor[] color)
        {
            for (int i = 0; i < word.Length; i++)
                if (color[i] == RuleColor.GREEN)
                {
                    known[i] = word[i];

                    // override "contains" knowledge
                    if (contains.Contains(word[i]))
                        contains.Remove(word[i]);
                }
        }


        void add_yellow_character_rules(string word, RuleColor[] color)
        {
            for (int i = 0; i < word.Length; i++)
                if (color[i] == RuleColor.YELLOW)
                {
                    not_possible[i].Add(word[i]);
                    contains.Add(word[i]);
                }
        }


        void add_grey_character_rules(string word, RuleColor[] color)
        {
            for (int i = 0; i < word.Length; i++)
                if (color[i] == RuleColor.GREY)
                {
                    var c = word[i];

                    if (word.IndexOf(c) == i)
                    {
                        // First time seeing 'c'
                        // Standard interpretation of "Grey"
                        for (int j = 0; j < not_possible.Length; j++)
                        {
                            // Make sure "not_possible" does not contradict "known"
                            if (known[j] != c)
                                not_possible[j].Add(c);
                        }

                        if (contains.Contains(c))
                            contains.Remove(c);
                    }
                    else
                    {
                        // Repeat of letter 'c'
                        // Only the current index cannot be 'c'
                        not_possible[i].Add(c);
                    }
                }
        }


        /// <summary>
        /// Returns "true" if "word" pass all the rules represented
        /// by this knowledge object.
        /// </summary>
        public bool Check(string word)
        {
            // Return false immediately on any violation.

            // Check word length
            if (word.Length != LEN) return false;

            // Check "known"
            for (int i = 0; i < known.Length; i++)
                if (known[i] != 0)
                    // This is a known character
                    if (known[i] != word[i])
                        return false;

            // Check "not possible"
            for (int i = 0; i < word.Length; i++)
                if (not_possible[i].Contains(word[i]))
                    return false;

            // Check "contains"
            foreach(var c in contains)
            {
                bool char_found = false;

                // The unknown characters must contain c
                for (int i = 0; i < word.Length; i++)
                    if (known[i] == 0) 
                        // This is an unknown character
                        if (word[i] == c)
                            char_found = true;                    

                if (char_found == false) return false;
            }

            return true;
        }


        /// <summary>
        /// Go through the "words" array and count the number of 
        /// words that can satisfy the current restrictions.
        /// </summary>
        public int Count_Pass(string[] words)
        {
            int count = 0;

            foreach (string word in words)
                if (word != "")
                    // Don't check the empty strings
                    if (Check(word))
                        count++;

            return count;
        }

    }
}