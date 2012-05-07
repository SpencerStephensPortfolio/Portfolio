#include "WordUnscrambler.hpp"
using namespace std;

static int nWords = WORDLIST_NUM_WORDS;

ListWord nWordList(string word)
{
	ListWord m(word);
	return m;
}

ListWord* getList()
{
	ListWord* bigAssList = new ListWord[WORDLIST_SIZE];
	bigAssList[0].word = "a";
	bigAssList[1].word = "b";
	// . . .
	bigAssList[9000].word = "zwy";

	for (int i = 0; i < WORDLIST_SIZE; i++) {
		bigAssList[i].charSum = sumCharacters(

	return bigAssList;
}