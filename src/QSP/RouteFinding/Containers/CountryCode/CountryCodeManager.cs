﻿using QSP.LibraryExtension;
using System.Collections.Generic;
using System.Linq;

namespace QSP.RouteFinding.Containers.CountryCode
{
    public class CountryCodeManager
    {
        // CountryCode <=> LetterCode
        private BiDictionary<int, string> letterCodeLookup;

        // LetterCode <=> FullName
        private Dictionary<string, string> _fullNameLookup;

        public IReadOnlyDictionary<string, string> FullNameLookup
        {
            get
            {
                return _fullNameLookup;
            }
        }

        public IReadOnlyDictionary<int, string> CodeToLetterLookup
        {
            get
            {
                return letterCodeLookup.FirstToSecond;
            }
        }

        public IReadOnlyDictionary<string, int> LetterToCodeLookup
        {
            get
            {
                return letterCodeLookup.SecondToFirst;
            }
        }

        /// <exception cref="ArgumentException"></exception>
        public CountryCodeManager(
            BiDictionary<int, string> letterCodeLookup,
            Dictionary<string, string> fullNameLookup)
        {
            this.letterCodeLookup = letterCodeLookup;
            this._fullNameLookup = fullNameLookup;
            Validate();
        }

        private void Validate()
        {
            // Make the BiDictionary and the other Dictionary compatible.
            // i.e. The sets of letter codes must be equal.

            var letterBiDict = LetterToCodeLookup.Keys;
            var letterDict = _fullNameLookup.Keys;

            var withoutFullName = letterBiDict.Except(letterDict);

            foreach (var i in withoutFullName)
            {
                _fullNameLookup.Add(i, "Unknown");
            }

            var withoutIntCode = letterDict.Except(letterBiDict);

            foreach (var j in withoutIntCode)
            {
                letterCodeLookup.RemoveBySecond(j);
            }
        }

        /// <exception cref="ArgumentException"></exception>
        public string GetFullName(int code)
        {
            return GetFullName(GetLetter(code));
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public string GetFullName(string letterCode)
        {
            return GetFullName(letterCode);
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public int GetCountryCode(string letterCode)
        {
            return letterCodeLookup.GetBySecond(letterCode);
        }

        /// <exception cref="ArgumentException"></exception>
        public string GetLetter(int code)
        {
            return letterCodeLookup.GetByFirst(code);
        }
    }
}
