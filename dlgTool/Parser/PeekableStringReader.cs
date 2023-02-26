namespace dlgTool.Parser
{
    class PeekableStringReader
    {
        private readonly int _length;
        private readonly StringReader _reader;
        private readonly IDictionary<int, char> _peekedChars;

        private int _currentPosition;

        public int Position => _currentPosition;
        public bool IsEnd => _currentPosition >= _length;

        public PeekableStringReader(string input)
        {
            _length = input.Length;
            _reader = new StringReader(input);
            _peekedChars = new Dictionary<int, char>();
        }

        /// <summary>
        /// Peeks the character at the relative position from the current one in the input.
        /// </summary>
        /// <param name="position">The relative position to the current one.</param>
        /// <returns>The peeked character.</returns>
        public char Peek(int position = 0)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            // Return already peeked character
            if (_peekedChars.ContainsKey(_currentPosition + position))
                return _peekedChars[_currentPosition + position];

            // Read and consume characters until given relative position
            var currentPosition = _currentPosition;
            for (var i = currentPosition; i <= currentPosition + position; i++)
                _peekedChars[i] = Read();
            _currentPosition -= position + 1;

            return _peekedChars[_currentPosition + position];
        }

        /// <summary>
        /// Reads and consumes the next character encountered in the input.
        /// </summary>
        /// <returns>The next character in the input.</returns>
        public char Read()
        {
            return ReadInternal();
        }

        private char ReadInternal(bool advancePosition = true)
        {
            // If character was already peeked, return it
            if (_peekedChars.ContainsKey(_currentPosition))
            {
                var nextChar = _peekedChars[_currentPosition];
                _peekedChars.Remove(_currentPosition);

                // Always advance position if we take from peeked cache
                // Peeked cache is already adjusted to ignored nodes, so we have to advance the position
                _currentPosition++;
                return nextChar;
            }

            // Read new character
            var character = (char)_reader.Read();

            if (advancePosition)
                _currentPosition++;

            return character;
        }
    }
}
