﻿using System.Collections.Generic;

namespace QSP.RouteFinding.TerminalProcedures
{
    public class ProcedureFilter
    {
        private Dictionary<string, FilterEntry> items =
            new Dictionary<string, FilterEntry>();

        public ProcedureFilter() { }

        public FilterEntry this[string icao, string rwy]
        {
            set
            {
                var key = (icao + rwy).ToUpper();
                items.Remove(key);
                items.Add(key, value);
            }

            get
            {
                var key = (icao + rwy).ToUpper();
                return items[key];
            }
        }

        public bool Exists(string icao, string rwy)
        {
            return items.ContainsKey((icao + rwy).ToUpper());
        }
    }

    public class FilterEntry
    {
        public bool IsBlackList { get; private set; }
        private List<string> _procedures;

        public IReadOnlyList<string> Procedures
        {
            get
            {
                return _procedures;
            }
        }

        public FilterEntry(bool isBlackList, List<string> procedures)
        {
            this.IsBlackList = isBlackList;
            this._procedures = procedures;
        }
    }
}
