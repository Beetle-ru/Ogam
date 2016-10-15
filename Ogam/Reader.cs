using System;
using System.Linq;

namespace Ogam {
    public class Reader {
        static public Pair Parse(string str) {
            var sr = new StringReader(str);
            var programm = new Pair();

            while (!sr.IsEndOfString()) {
                Skip(sr);
                programm.Add(ReadPrimitive(sr));
                Skip(sr);
            }

            return programm;
        }

        private static object ReadPrimitive(StringReader sr) {
            var c = sr.GetC();

            if (IsDigit(c) || (IsNumber(c) && IsDigit(sr.GetC(1)))) {
                return ReadNumber(sr);
            }

            if (IsString(c)) {
                return ReadString(sr);
            }

            if (IsPair(c)) {
                return ReadList(sr);
            }

            if (IsQuote(c)) {
                return ReadQuote(sr);
            }

            if (IsSharp(c)) {
                return ReadSahrp(sr);
            }

            if (IsBadSymbol(sr)) {
                throw new Exception(string.Format("READER:ReadPrimitive{0}Bad symbol '{1}' in {2} position{0}", Environment.NewLine, c, sr.GetPosition()));
            }

            return ReadSymbol(sr);
        }

        private static bool IsBadSymbol(StringReader sr) {
            var c = sr.GetC();

            return (c == ')') || (c == '"');
        }


        private static Pair ReadQuote(StringReader sr) {
            var quote = new Pair();

            sr.GetNext();

            quote.Car = "quote".ToSymbol();
            
            Skip(sr);
            quote.Cdr = new Pair() {Car = ReadPrimitive(sr)};

            return quote;
        }

        private static Pair ReadVector(StringReader sr) {
            var operation = new Pair();

            sr.GetNext();

            operation.Car = "vector".ToSymbol();

            Skip(sr);
            operation.Cdr = ReadPrimitive(sr);

            return operation;
        }

        private static object ReadSahrp(StringReader sr) {

            sr.GetNext();

            if (sr.IsEndOfString()) {
                throw new Exception(string.Format("READER:ReadSahrp{0}Expected next symbol in {1} position", Environment.NewLine, sr.GetPosition()));
            }

            var c = sr.GetC();

            switch (c) {
                case  '\\':
                    return ReadCharter(sr);

                case '(':
                    sr.GetPrev();
                    return ReadVector(sr);

                case 'T':
                case 't':
                    sr.GetNext();
                    return true;

                case 'F':
                case 'f':
                    sr.GetNext();
                    return false;
                   
                default :
                    sr.GetPrev();
                    return ReadSymbol(sr);
            }
        }

        private static char ReadCharter(StringReader sr) {
            sr.GetNext();

            if (sr.IsEndOfString()) {
                throw new Exception(string.Format("READER:ReadCharter{0}Expected next symbol in {1} position", Environment.NewLine, sr.GetPosition()));
            }

            var c = sr.GetC();
            sr.GetNext();
            return c;
        }

        private static object ReadNumber(StringReader sr) {
            var buf = "";
            var c = sr.GetC();
            while ((!sr.IsEndOfString()) && ((IsNumber(c)) || (c == '.'))) {
                buf = string.Concat(buf, c);
                sr.GetNext();
                c = sr.GetC();
            }

            try {
                return ParseNumber(buf);
            }
            catch (Exception) {
                throw new Exception(string.Format("READER:ReadNumber{0}Expected number in {1} position{0}Number destroyed:{0}\"{2}\"", Environment.NewLine, sr.GetPosition(), buf));
            }
        }

        private static object ParseNumber(string str) {
            var factor = 1.0;
            for (var i = str.Length - 1; i >= 0; i--) {
                if (str[i] == '.') {
                    str = str.Remove(i, 1);

                    return Int64.Parse(str)*factor;
                }

                factor *= 0.1;
            }

            return int.Parse(str);
        }

        private static object ReadSymbol(StringReader sr) {
            var buf = "";
            var c = sr.GetC();
            while ((!sr.IsEndOfString()) && ((c != '"') && (c != '(') && (c != ')') && (c != '\0') && (!IsWhite(c)))) {
                buf = string.Concat(buf, c);
                sr.GetNext();
                c = sr.GetC();
            }

            return  buf.ToSymbol();
        }

        private static object ReadString(StringReader sr) {
            if (sr.IsEndOfString()) return null;

            var buf = "";
            var c = sr.GetNext();
            while (!sr.IsEndOfString()) {
                if (c == '"') { //ok
                    sr.GetNext();
                    return buf;
                }

                if (c == '\\') {
                    while (c == '\\') {
                        sr.GetNext();
                        c = sr.GetC();

                        buf = string.Concat(buf, c);
                        sr.GetNext();
                        c = sr.GetC();
                    }
                }
                else {
                    buf = string.Concat(buf, c);
                    sr.GetNext();
                    c = sr.GetC();
                }
            }

            sr.GetNext();

            throw new Exception(string.Format("READER:ReadString{0}Expected symbol '\"' in {1} position{0}String destroyed:{0}\"{2}\"", Environment.NewLine, sr.GetPosition(), buf));
        }

        private static Pair ReadList(StringReader sr) {
            var rootPair = new Pair();
            var lastPair = rootPair;

            sr.GetNext();

            while ((!sr.IsEndOfString())) {
                Skip(sr);

                if (sr.GetC() == ')') { //ok
                    sr.GetNext();

                    return rootPair;
                }

                if (sr.GetC() == '.') {
                    sr.GetNext();
                    Skip(sr);
                    lastPair.Cdr = ReadPrimitive(sr);
                    continue;
                }

                if (lastPair.Car != null) {
                    var newC = new Pair();
                    lastPair.Cdr = newC;
                    lastPair = newC;
                }

                lastPair.Car = ReadPrimitive(sr);
            }

            throw new Exception(string.Format("READER:ReadList{0}Expected symbol ')' in {1} position{0}Expression destroyed:{0}{2}", Environment.NewLine, sr.GetPosition(), rootPair.ToString()));

            return rootPair;
        }

        private static void SkipWhite(StringReader sr) {
            while ((!sr.IsEndOfString()) && (IsWhite(sr.GetC()))) {
                sr.GetNext();
            }
        }

        private static void Skip(StringReader sr) {
            SkipWhite(sr);

            while ((!sr.IsEndOfString()) && IsComment(sr.GetC())) {
                SkipComment(sr);
                SkipWhite(sr);
            }

        }

        private static void SkipComment(StringReader sr) {
            while ((!sr.IsEndOfString()) && ((sr.GetC() != '\n') && (sr.GetC() != '\r'))) {
                sr.GetNext();
            }
        }

        private static bool IsComment(char c) {
            return c == ';';
        }

        private static bool IsWhite(char c) {
            return c == ' ' || c == '\n' || c == '\r' || c == '\t' || c == '\0';
        }

        private static bool IsDigit(char c) {
            return c >= '0' && c <= '9';
        }

        private static bool IsNumber(char c) {
            return IsDigit(c) || IsSign(c);
        }

        private static bool IsPair(char c) {
            return c == '(';
        }

        private static bool IsString(char c) {
            return c == '"';
        }

        private static bool IsQuote(char c) {
            return c == '\'';
        }

        private static bool IsSharp(char c) {
            return c == '#';
        }

        private static bool IsSign(char c) {
            return c == '-' || c == '+';
        }

        class StringReader {
            private string str;
            private uint p;
            public StringReader(string stri) {
                str = stri;
                p = 0;
            }

            public char GetC(uint preview = 0) {
                return !IsEndOfString(preview) ? str[(int)(p + preview)] : '\0';
            }

            public uint GetPosition() {
                return p;
            }

            public char GetNext(uint indx = 1) {
                p += indx;
                return !IsEndOfString() ? str[(int)p] : '\0';
            }

            public char GetPrev(uint indx = 1) {
                if (p - indx >= 0) {
                    p -= indx;
                    return str[(int)p];
                }
                return '\0';
            }

            public bool IsEndOfString(uint preview = 0) {
                return p + preview >= str.Length;
            }
        }
    }
}
