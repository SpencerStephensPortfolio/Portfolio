// main.cpp
// by: Spencer Stephens
//
// Provides the user an interface for testing the WordSearch class. The 
//

#include <iostream>
#include "WordSearch.hpp"

int main(int argc, char* argv[])
{
	// Load WordSearch puzzle
	try {
		WordSearch puzzle("wordsearch.txt");
		//puzzle.PrintWordSearch();

		// main loop
		for (;;) {
			std::string key;
			std::cout << "Enter word: ";
			std::getline(std::cin, key);
			if (key.length() == 0)
				break;

			int x, y;
			if (puzzle.Contains(key, &x, &y)) {
				std::cout << "Word found at (" << x << "," << y << ")\n";
			}
			else {
				std::cout << "Word not found\n";
			}
		}
	}
	catch (std::exception e) {
		std::cout << "Error: " << e.what() << "\n";
	}

	return 0;
}