using System;
using System.Linq;

namespace Ogam {
    public class Reader {
        static public object Parse(string str) {
            var p = 0;
            var programm = new Pair();

            while (p < str.Length) {
                Skip(str, ref p);
                programm.Add(ReadPrimitive(str, ref p));
                Skip(str, ref p);
            }

            return programm;
        }


        private static object ReadPrimitive(string str, ref int p) {
            var c = p < str.Length ? str[p] : '\0';

            if (IsDigit(c) || (IsNumber(c) && IsDigit(str[p + 1]))) {
                return ReadNumber(str, ref p);
            }

            if (IsString(c)) {
                return ReadString(str, ref p);
            }

            if (IsPair(c)) {
                return ReadPair(str, ref p);
            }

            if (IsQuote(c)) {
                return ReadQuote(str, ref p);
            }

            if (IsSharp(c)) {
                return ReadSahrp(str, ref p);
            }

            if (IsBadSymbol(str, p)) {
                throw new Exception(string.Format("READER:ReadPrimitive{0}Bad symbol '{1}' in {2} position{0}", Environment.NewLine, c, p));
            }

            return ReadSymbol(str, ref p);
        }

        private static bool IsBadSymbol(string str, int p) {
            var c = p < str.Length ? str[p] : '\0';

            return (c == ')') || (c == '"');
        }


        private static Pair ReadQuote(string str, ref int p) {
            var quote = new Pair();

            p++;

            quote.Car = "quote".ToSymbol();
            
            Skip(str, ref p);
            quote.Cdr = new Pair() {Car = ReadPrimitive(str, ref p)};

            return quote;
        }

        private static Pair ReadVector(string str, ref int p) {
            var operation = new Pair();

            p++;

            operation.Car = "vector".ToSymbol();

            Skip(str, ref p);
            operation.Cdr = ReadPrimitive(str, ref p);

            return operation;
        }

        private static object ReadSahrp(string str, ref int p) {

            p++;

            if (p >= str.Length) {
                throw new Exception(string.Format("READER:ReadSahrp{0}Expected next symbol in {1} position", Environment.NewLine, p));
            }

            var c = str[p];

            switch (c) {
                case  '\\':
                    return ReadCharter(str, ref p);

                case '(':
                    p--;
                    return ReadVector(str, ref p);
                case 'T':
                case 't':
                    p++;
                    return true;

                case 'F':
                case 'f':
                    p++;
                    return false;
                   
                default :
                    p--;
                    return ReadSymbol(str, ref p);
            }

            return null;
        }

        private static char ReadCharter(string str, ref int p) {
            p++;

            if (p >= str.Length) {
                throw new Exception(string.Format("READER:ReadCharter{0}Expected next symbol in {1} position", Environment.NewLine, p));
            }

            return str[p++];
        }

        private static object ReadNumber(string str, ref int p) {
            var buf = "";
            var c = str[p];
            while ((p < str.Length) && ((IsNumber(c)) || (c == '.'))) {
                buf = string.Concat(buf, c);
                p++;
                c = p < str.Length ? str[p] : '\0';
            }

            try {
                return ParseNumber(buf);
            }
            catch (Exception) {
                throw new Exception(string.Format("READER:ReadNumber{0}Expected number in {1} position{0}Number destroyed:{0}\"{2}\"", Environment.NewLine, p, buf));
            }
            
            return null;
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

            var intval = int.Parse(str);
            
            return intval;
        }

        private static object ReadSymbol(string str, ref int p) {
            var buf = "";
            var c = p < str.Length ? str[p] : '\0';
            while ((p < str.Length) && ((c != '"') && (c != '(') && (c != ')') && (c != '\0') && (!IsWhite(c)))) {
                buf = string.Concat(buf, c);
                p++;
                c = p < str.Length ? str[p] : '\0';
            }

            return  buf.ToSymbol();
        }

        private static object ReadString(string str, ref int p) {
            if (p + 1 >= str.Length) return null;

            var buf = "";
            var c = str[++p];
            while (p < str.Length) {
                if (c == '"') { //ok
                    p++;
                    return buf;
                }

                

                if (c == '\\') {
                    while (c == '\\') {
                        p++;
                        c = p < str.Length ? str[p] : '\0';

                        buf = string.Concat(buf, c);

                        p++;
                        c = p < str.Length ? str[p] : '\0';
                    }
                }
                else {
                    buf = string.Concat(buf, c);
                    p++;
                    c = p < str.Length ? str[p] : '\0';
                }
            }

            p++;

            throw new Exception(string.Format("READER:ReadString{0}Expected symbol '\"' in {1} position{0}String destroyed:{0}\"{2}\"", Environment.NewLine, p, buf));
        }

        private static Pair ReadPair(string str, ref int p) {
            var root = new Pair();
            var last = root;

            p++;

            while ((p < str.Length)) {
                Skip(str, ref p);

                if (str[p] == ')') { //ok
                    p++;

                    return root;
                }

                if (str[p] == '.') {
                    p++;
                    Skip(str, ref p);
                    last.Cdr = ReadPrimitive(str, ref p);
                    continue;
                }

                if (last.Car != null) {
                    var newC = new Pair();
                    last.Cdr = newC;
                    last = newC;
                }

                last.Car = ReadPrimitive(str, ref p);
            }

            throw new Exception(string.Format("READER:ReadPair{0}Expected symbol ')' in {1} position{0}Expression destroyed:{0}{2}", Environment.NewLine, p, root.ToString()));

            return root;
        }

        private static void SkipWhite(string str, ref int p) {
            while ((p < str.Length) && (IsWhite(str[p]))) {
                p++;
            }
        }

        private static void Skip(string str, ref int p) {
            SkipWhite(str, ref p);

            while ((p < str.Length) && IsComment(str[p])) {
                SkipComment(str, ref p);
                SkipWhite(str, ref p);
            }

        }

        private static void SkipComment(string str, ref int p) {
            while ((p < str.Length) && ((str[p] != '\n') && (str[p] != '\r'))) {
                p++;
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

        private static bool IsLeter(char c) {
            const string dic = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";
            return dic.Any(chr => c == chr);
        }
    }
}
