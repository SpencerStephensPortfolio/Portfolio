// 
// WordUnscrambler.cpp
// Written by Spencer Stephens
//
#include "WordUnscrambler.hpp"
#include <iostream>
#include <string>
#include <fstream>
#include <algorithm>

// Returns the sum of the ASCII values for all characters in the string.
int sumCharacters(std::string word)
{
	int sum = 0;
	for (int i = 0; i < (int)word.length(); i++) {
		sum += tolower(word[i]);
	}
	return sum;
}

// Returns true if word has letter ch
bool hasLetter(char ch, std::string word)
{
	ch = tolower(ch);
	for (int i = 0; i < (int)word.length(); i++) {
		if (tolower(word[i]) == ch)
			return true;
	}
	return false;
}

// Initializes object and loads the word list.
Unscrambler::Unscrambler(std::string path) throw(...)
{
	// open
	std::ifstream file;
	file.open(path.c_str(), std::ios::in);
	if (!file.is_open()) {
		throw std::exception("file not found.");
	}
	
	// allocate list
	int nWords = std::count(std::istreambuf_iterator<char>(file),
		std::istreambuf_iterator<char>(), '\n');
	file.seekg(std::ios::beg);

	_words.resize(nWords); // should be about 220,000 with wordlistv2
	

	// load each word

	ListWord* iter = & _words.front();
	std::string word;

	while (file >> word) {
		iter->setWord(word);
		iter++;
	}

	// sort the array
	std::sort(&_words.front(), &_words.back());
}

Unscrambler::~Unscrambler()
{ }

// Unscrambles word and returns a list of permutations
std::list<std::string> Unscrambler::unscramble(std::string word) const
{
	std::list<std::string> words;

	// Test 1: ascii sum
	// find the index of first word with the same charSum
	int sum = sumCharacters(word);
	const ListWord* it = std::find(&_words.front(), &_words.back(), sum);

	while ((it->getSum() == sum) && (it != &_words.back())) {
		bool isPermutation = true;
		const std::string& candidate = it->getWord();

		// Test 2: length
		if (candidate.length() != word.length()) {
			isPermutation = false;
		}
		else {
			// Test 3: letters
			// make sure word has all the characters that _list does.
			for (size_t i = 0; i < candidate.length(); i++) {
				if (!hasLetter(candidate[i], word)) {
					isPermutation = false;
				}
			}
			// make sure _list has all the characters that
			// word does.
			for (size_t i = 0; i < word.length(); i++) {
				if (!hasLetter(word[i], candidate)) {
					isPermutation = false;
				}
			}
		}
		// add to list
		if (isPermutation) {
			words.push_back(candidate);
		}

		++it;
	}

	return words;
}
