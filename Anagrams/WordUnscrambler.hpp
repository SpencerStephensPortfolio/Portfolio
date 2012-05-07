// 
// WordUnscrambler.h
// Written by Spencer Stephens
//
#ifndef _WORDUNSCRAMBLER_H
#define _WORDUNSCRAMBLER_H

#include <vector>
#include <list>
#include <string>

// Returns the sum of the ASCII values for all characters in the string.
int sumCharacters(std::string word);

// Returns true if word has letter ch
bool hasLetter(char ch, std::string word);

// A container class for storing words in a word list.
class ListWord
{
public:
	// Ctor
	ListWord()
		: _word(), _charSum(-1)
	{ }

	ListWord(std::string word)
		: _word(word), _charSum(sumCharacters(word))
	{ }

	ListWord(const ListWord& lw)
		: _word(lw._word), _charSum(lw._charSum)
	{ }

	ListWord& operator=(const ListWord& lw)
	{
		_word = lw._word;
		_charSum = lw._charSum;

		return *this;
	}

	~ListWord()
	{
	}

	// access
	const std::string& getWord() const
	{
		return _word;
	}

	void setWord(const std::string& word)
	{
		_charSum = sumCharacters(word);
		_word = word;
	}
	int getSum() const
	{
		return _charSum;
	}

	// Comparison operators. These compare ListWords by _charSum, not _word
	bool operator<(const ListWord& word) const
	{
		return _charSum < word._charSum;
	}
	bool operator>(const ListWord& word) const
	{
		return _charSum > word._charSum;
	}
	bool operator==(const ListWord& word) const
	{
		return _charSum == word._charSum;
	}
	// Compares ListWord to int.
	bool operator==(const int a) const
	{
		return _charSum == a;
	}

private:

	std::string _word;
	int _charSum; // sum of all the ASCII values in the word
};

// An algorithm class that uses a word list to unscramble words
class Unscrambler
{
public:

	// Ctor

	// Initializes object, given path to a dictionary (word list)
	explicit Unscrambler(std::string path) throw(...);
	
	// Implicit destructor will work
	~Unscrambler();

	// Unscrambles word and returns a list of permutations
	std::list<std::string> unscramble(std::string word) const;

private:

	std::vector<ListWord> _words;
};

#endif // _WORDUNSCRAMBLER_H