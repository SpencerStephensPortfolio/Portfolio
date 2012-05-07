// WordSearch.cpp
// by: Spencer Stephens
//
// Loads a tab/space and newline delimited wordsearch from a file and
// provides in interface to solve it via the "Contains" method.
//

#include "WordSearch.hpp"
#include <fstream>
#include <sstream>
#include <string>

// Returns true if this WordSearch contains "word," and changes xcoord and
// ycoord to its location
bool WordSearch::Contains(std::string input, int* xcoord, int* ycoord)
{
	// get character count
	int nCharacters = 1;
	typedef std::string::size_type strsize_t;
	for (strsize_t i = 0; i < input.length(); i++) {
		if (input[i] == ' ' || input[i] == '\t')
			nCharacters++;
	}

	if (nCharacters < 2) {
		std::cout << "Error: input needs to contain at least two characters.\n";
		return false;
	}

	// split up input into an array of characters
	std::stringstream stream;
	stream << input;

	search_word_t word(nCharacters);
	for (int i = 0; i < nCharacters; i++) {
		stream >> word[i];
	}

	// loop through the entire WordSearch
	int startX, startY;
	for (startX = 0; startX < _width; startX++) {
		for (startY = 0; startY < _height; startY++) {
			// Check for a match
			if (startX == 22 && startY == 32)
				int stopit = 0;
			if (WordStartsAtIndex(word, startX, startY)) {
				*xcoord = startX;
				*ycoord = startY;
				return true;
			}
		}
	}

	// didn't find it
	return false;
}

// Checks 
bool WordSearch::WordStartsAtIndex(search_word_ref word, int startX, int startY)
{
	// Check first character
	if (word[0] != _values[startY * _width + startX])
		return false;

	// Find direction of the word by checking the surrounding characters
	for (int offsetY = -1; offsetY <= 1; offsetY++) {
		for (int offsetX = -1; offsetX <= 1; offsetX++) {
			// Ignore (0,0) offset
			if (!(offsetX == 0 && offsetY == 0) && CheckWord(word, startX, startY, offsetX, offsetY))
				return true;
		}
	}

	// couldn't find word
	return false;
}

// 
bool WordSearch::CheckWord(search_word_ref word, int startX, int startY, int dirX, int dirY)
{
	int indexX = startX + dirX;
	int indexY = startY + dirY;

	typedef search_word_t::size_type wordsize_t;
	for (wordsize_t i = 1; i < word.size(); i++, indexX += dirX, indexY += dirY) {
		if (!InBounds(indexX, indexY))
			return false;
		if (word[i] != _values[indexY * _width + indexX])
			return false;
	}

	// word is in WordSearch puzzle
	return true;
}

// Load WordSearch file into the array
void WordSearch::Load(std::string path) throw()
{
	_height = _width = 0;

	std::ifstream file;
	file.open(path.c_str(), std::ios::binary);

	if (!file.is_open()) {
		// Throw exception
		std::stringstream ss;
		ss << "invalid filepath \"" << path << "\"";
		char* msg = new char[ss.str().length()];
		strcpy(msg, ss.str().c_str());
		throw std::exception(msg, 1);
	}

	// ascertain the size of rows by counting spaces
	_width = 1;
	int ch;
	while ((ch = file.get()) != '\n') {
		if (ch == '\t' || ch == ' ')
			_width++;
	}

	// determine the number of columns based on how many newlines there are
	_height = 1;

	while (!file.eof()) {
		if (file.get() == '\n')
			_height++;
	}

	// reset file pointer
	file.clear(); // clear EOF error
	file.seekg(0, std::ios::beg);

	// allocate memory
	_values.resize(_height * _width);

	for (int y = 0; y < _height; y++) {
		for (int x = 0; x < _width; x++) {
			int index = y * _width + x;
			
			file >> _values[index];
		}
	}
}

// Checks to see if x and y are valid coordinates for "_values"
bool WordSearch::InBounds(int x, int y)
{
	if (((x >= 0) && (x < _width)) && ((y >= 0) && (y < _height))) {
		return true;
	}

	return false;
}