using System;
using System.Collections.Generic;
using System.Text;

namespace Ogam {
    public class Pair {
        public object Car;
        public object Cdr;

        public Pair() {}

        public Pair(object car, object cdr = null) {
            Car = car;
            Cdr = cdr;
        }

        public Pair Add(object o) {
            if (Car == null && Cdr == null) {
                Car = o;
                return this;
            }

            var cndr = this;
            while (cndr.Cdr != null) {
                if (cndr.Cdr is Pair) {
                    cndr = cndr.Cdr as Pair;
                }
                else {
                    break;
                }
            }
            cndr.Cdr = new Pair(o);

			return cndr.Cdr as Pair;
        }

        public override string ToString() {
            if (Car == null && Cdr == null) {
                return "()";
            }

            var sb = new StringBuilder();

            //var str = "(";
            sb.Append("(");

            //str += Car != null ? O2String(Car) : "()";

            sb.Append(Car != null ? O2String(Car) : "()");
            
            if (Cdr is Pair) {
                var cndr = Cdr as Pair;
                while (cndr != null) {
                    //str += " " + O2String(cndr.Car);
                    sb.Append(" ");
                    sb.Append(O2String(cndr.Car));
                    if (cndr.Cdr is Pair) {
                        cndr = cndr.Cdr as Pair;
                    }
                    else {
                        if (cndr.Cdr != null) {
                            //str += " . ";
                            sb.Append(" . ");
                            //str += O2String(cndr.Cdr);
                            sb.Append(O2String(cndr.Cdr));
                        }
                        break;
                    }
                }

                
            }
            else {
                if (Cdr != null) {
                    //str += " . ";
                    sb.Append(" . ");
                    //str += O2String(Cdr);
                    sb.Append(O2String(Cdr));
                }
            }

            
            sb.Append(")");
            return sb.ToString();
            //return str + ")";
        }

        public static string O2String(object o) {
            if (o == null) return "#nil";

            var str = "";
            if (IsBaseType(o) || o is Pair || o is Symbol) {
                    str = o.ToString();
                } else if (o is string) {
                    str = (string) o;
                    str = str.Replace("\\", "\\\\");
                    str = str.Replace("\"", "\\\"");
                    str = (string.Format("\"{0}\"", str));
                } else if (o is bool) {
                    str = string.Format("{0}", ((bool)o ? "#t" : "#f"));
                } else {
                    //strs.Add(string.Format(" \"{0}\"", Pack(o)));
                    str = string.Format("\"{0}\"", o.ToString());
                }


            return str;
        }

        private static bool IsBaseType(object o) {
            return (o is byte)
                   || (o is sbyte)
                   || (o is char)
                   || (o is decimal)
                   || (o is double)
                   || (o is float)
                   || (o is int)
                   || (o is uint)
                   || (o is long)
                   || (o is ulong)
                   || (o is short)
                   || (o is ushort);
    }

        public object ObjecttAt(int position) {

            var cndr = this;

            for (var i = 0; i < position; i++) {
                if (cndr == null) return null;
                cndr = cndr.Cdr as Pair;
            }

            return (cndr != null) ? cndr.Car : null;
        }

        public string StringAt(int position) {
            return Convert.ToString(ObjecttAt(position));
        }

        public int IntAt(int position) {
            return Convert.ToInt32(ObjecttAt(position));
        }

        public double DoubleAt(int position) {
            return Convert.ToDouble(ObjecttAt(position));
        }

        public bool BoolAt(int position) {
            return Convert.ToBoolean(ObjecttAt(position));
        }

        public object AsObject() {
            return Car;
        }

        public string AsString() {
            var o = AsObject();

            return o == null ? "" : Convert.ToString(o);
        }

        public int AsInt() {
            var o = AsObject();

            return o == null ? 0 : Convert.ToInt32(o);
        }

        public double AsDouble() {
            var o = AsObject();

            return o == null ? 0.0 : Convert.ToDouble(o);
        }

        public bool AsBool() {
            var o = AsObject();

            return o != null && Convert.ToBoolean(o);
        }

        public Pair MoveNext() {
            var next = Cdr as Pair;

            if (next != null) {
                Car = next.Car;
                Cdr = next.Cdr;
            }
            else {
                return null;
                //Car = null;
                //Cdr = null;
            }
            
            return this;
        }

        public int Count() {
            var cndr = this;

            var i = 0;
            while (cndr != null) {
                i++;
                cndr = cndr.Cdr as Pair;
            }

            return i;
        }


        public static object GetCarOrNull(object obj) {
            var pair = obj as Pair;
            if (pair == null)
                return null;

            return pair.Car;
        }

        public static object GetCdrOrNull(object obj) {
            var pair = obj as Pair;
            if (pair == null)
                return null;

            return pair.Cdr;
        }
    }
}
