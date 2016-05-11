using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public class OgamSerializer {
        public static string Serialize(object obj) {
            if (obj == null) return "#nil";

            if (IsBaseType(obj)) {
                return Pair.O2String(obj);
            }

            var pair = SerializeObject(obj);
            return "'" + pair.ToString();
        }

        public static object Deserialize(Pair data, Type type) {
            return DeserealizeObject(data, type);
        }

        private static object DeserealizeObject(Pair data, Type type) {
            if (data == null) return null;

            if (type.GetInterfaces().Any(ie => ie == typeof (IDictionary))) {
                return DeserializeDictionary(data, type.GetGenericArguments());
            }

            if (type.GetInterfaces().Any(ie => ie == typeof(ICollection))) {
                return DeserializeCollection(data, type.GetGenericArguments());
            }

            var o = Activator.CreateInstance(type);


            while (true) {
                var pair = data.AsObject() as Pair;

                if (pair == null) {
                    if (data.MoveNext() == null)
                        break;

                    continue;
                }

                var name = pair.AsString();
                var value = pair.Cdr;

                //var field = type.GetField(name);
                var members = type.GetMember(name);

                if (members.Any()) {

                    var memberInfo = members[0];

                    if (memberInfo is FieldInfo) {
                        var field = (FieldInfo) memberInfo;

                        if (field.IsLiteral) { // cannot be change
                            if (data.MoveNext() == null) break;
                            continue;
                        }

                        if (field.FieldType == typeof (DateTime)) {
                            value = Convert.ToDateTime(value);
                        }
                        else if (field.FieldType.GetInterfaces().Any(ie => ie == typeof (ICollection))) {
                            value = DeserializeCollection(((Pair) value).Car as Pair,
                                field.FieldType.GetGenericArguments());
                        }
                        else if (!IsBaseType(field.FieldType)) {
                            value = DeserealizeObject(value as Pair, field.FieldType);
                        }

                        field.SetValue(o, value);
                    } else if (memberInfo is PropertyInfo) {
                        var property = (PropertyInfo)memberInfo;

                        if (property.PropertyType == typeof(DateTime)) {
                            value = Convert.ToDateTime(value);
                        } else if (property.PropertyType.GetInterfaces().Any(ie => ie == typeof(ICollection))) {
                            value = DeserializeCollection(((Pair)value).Car as Pair,
                                property.PropertyType.GetGenericArguments());
                        } else if (!IsBaseType(property.PropertyType)) {
                            value = DeserealizeObject(value as Pair, property.PropertyType);
                        }

                        property.SetValue(o, value, null);
                    }

                }
                if (data.MoveNext() == null) break;
            }

            return o;
        }

        private static ICollection DeserializeCollection(Pair lst, Type[] types) {
            var d1 = typeof(List<>);
            var makeme = d1.MakeGenericType(types);
            var list = (IList)Activator.CreateInstance(makeme);

            while (true) {

                var o = lst.AsObject();

                if (o is Pair) {
                    var pair = o as Pair;
                    o = DeserealizeObject(pair, types[0]);
                }

                if (o != null) {
                    list.Add(o);
                }

                if (lst.MoveNext() == null)
                    break;
            }

            return list;
        }

        private static ICollection DeserializeDictionary(Pair lst, Type[] types) {
            var d1 = typeof(Dictionary<,>);
            var makeme = d1.MakeGenericType(types);
            var dic = (IDictionary)Activator.CreateInstance(makeme);

            while (true) {

                var p = lst.AsObject() as Pair;
                var k = p.Car;
                var v = p.Cdr;
                
                if (k is Pair) {
                    var pair = k as Pair;
                    k = DeserealizeObject(pair, types[0]);
                }

                if (v is Pair) {
                    var pair = v as Pair;
                    v = DeserealizeObject(pair, types[1]);
                }

                if (k != null) {
                    //dic.Add(k,v);
                    dic[k] = v;
                }

                if (lst.MoveNext() == null)
                    break;
            }

            return dic;
        }


        private static Pair SerializeObject(object obj) {
            if (obj == null) {
                return new Pair();
            }
          
            if (obj is IDictionary) {
                return SerializeDictionary(obj as IDictionary);
            }

            if (obj is ICollection) {
                return SerializeCollection(obj as ICollection);
            }

            var type = obj.GetType();
            //var fields = type.GetFields();
            var members = type.GetMembers();
            
            var root = new Pair();
            var current = root;

            foreach (var memberInfo in members) {
                var name = memberInfo.Name;
                object value = null;// = fieldInfo.GetValue(obj);

                if (memberInfo is FieldInfo) {
                    value = ((FieldInfo) memberInfo).GetValue(obj);
                } else if (memberInfo is PropertyInfo) {
                    value = ((PropertyInfo) memberInfo).GetValue(obj, null);
                } else continue;

                if (!IsBaseType(value)) {
                    if (value is ICollection) {
                        value = new Pair(SerializeCollection(value as ICollection));
                    }
                    else {
                        value = SerializeObject(value);
                    }

                } else if (value is string) {
                    //value = Pair.O2String(value);
                    value = value;
                }


                current = current.Add(new Pair(name.ToSymbol(), value));
                
            }

            return root;
        }

        private static Pair SerializeDictionary(IDictionary dictionary) {
            var root = new Pair();
            var current = root;

            var keys = dictionary.Keys;

            foreach (var key in keys) {
                var item = new Pair(O2Primitive(key), O2Primitive(dictionary[key]));
                current = current.Add(item);
            }

            return root;
        }

        private static Pair SerializeCollection(ICollection collection) {
            var root = new Pair();
            var current = root;

            foreach (var item in collection) {
                current = current.Add(O2Primitive(item));
            }

            return root;
        }

        private static object O2Primitive(object obj) {
            return IsBaseType(obj) ? obj : SerializeObject(obj);
        }

        private static bool IsBaseType(object o) {
            return o == null || IsBaseType(o.GetType());
        }

        private static bool IsBaseType(Type t) {
            return (t == typeof(byte))
                   || (t == typeof(sbyte))
                   || (t == typeof(bool))
                   || (t == typeof(char))
                   || (t == typeof(decimal))
                   || (t == typeof(double))
                   || (t == typeof(float))
                   || (t == typeof(int))
                   || (t == typeof(uint))
                   || (t == typeof(long))
                   || (t == typeof(ulong))
                   || (t == typeof(short))
                   || (t == typeof(ushort))
                   || (t == typeof(DateTime))
                   || (t == typeof(string));

        }
    }
}
