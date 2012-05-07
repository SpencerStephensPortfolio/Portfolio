#include <iostream>
#include <string>
#include <time.h>
#include <algorithm>
#include <exception>
#include "WordUnscrambler.hpp"

int main()
{
	std::cout << "Loading...";
	try {
		Unscrambler x("wordlistv2");
		std::cout << "done.\n\n";

		std::list<std::string> answers;
		std::string input;
		for (;;) {
			std::cout << "Next word: ";
			std::getline(std::cin, input);
			if (input.length() == 0)
				break;

			if (input == "")
				return 0;

			answers = x.unscramble(input);
			typedef std::list<std::string>::const_iterator c_iter;
			for (c_iter it = answers.begin(); it != answers.end(); ++it) {
				std::cout << *it << '\n';
			}
		}
	}
	catch (std::exception e)  {
		std::cout << "\nFailed: " << e.what() << '\n';
	}

	return 0;
}