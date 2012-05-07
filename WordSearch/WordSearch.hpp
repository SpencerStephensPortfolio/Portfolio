// WordSearch.hpp
// by: Spencer Stephens
//
// Loads a tab/space and newline delimited wordsearch from a file and
// provides in interface to solve it via the "Contains" method.
//

#include <iostream>
#include <string>
#include <vector>

class WordSearch
{
public:
	// Typedefs

	typedef std::string symbol_t;
	typedef std::vector<symbol_t> search_word_t; // (i.e. )
	typedef const search_word_t& search_word_ref;

	// Ctor/

	// Creates a WordSearch object 
	WordSearch(std::string path) throw()
		: _values()
	{
		Load(path);
	}

	~WordSearch()
	{ }

	// Returns true if this WordSearch contains "word," and changes xcoord and
	// ycoord to its location
	bool Contains(std::string word, int* xcoord, int* ycoord);

	// DEBUG: Prints the word search
	void PrintWordSearch()
	{
		for (int y = 0; y < _height; y++) {
			for (int x = 0; x < _width; x++) {
				std::cout << _values[y * _width + x] << '\t';
			}
			std::cout << '\n';
		}
	}

private:
	// No copy constructor
	WordSearch(const WordSearch&);
	
	// Data

	std::vector<std::string> _values; // row-major
	int _height, _width;

	// Helper methods

	bool WordStartsAtIndex(search_word_ref word, int startX, int startY);
	bool CheckWord(search_word_ref word, int startX, int startY, int offsetX, int offsetY);
	void Load(std::string file) throw();
	bool InBounds(int x, int y);
};