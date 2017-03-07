using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Ogam {
    public class OgamSerializer {
        private static readonly Dictionary<Type, Func<object, Pair>> Serializers =
            new Dictionary<Type, Func<object, Pair>>();

        private static readonly Dictionary<Type, Func<Pair, object>> Deserializers =
            new Dictionary<Type, Func<Pair, object>>();

        private static readonly List<string> RequiredNamespaces = new List<string>();

        static OgamSerializer() {
            RequiredNamespaces.Add("System");
            RequiredNamespaces.Add("System.Collections.Generic");
            RequiredNamespaces.Add("System.Collections");
            RequiredNamespaces.Add("System.Linq");
            RequiredNamespaces.Add("Ogam");
        }

        public static Pair Serialize(object data, Type t) {
            if (data == null)
                return new Pair();

            if (IsBaseType(t))
                throw new ArgumentException(
                    "This method should not be used with base types (Primitive, Decimal, String, DateTime)");

            if (!Serializers.ContainsKey(t))
                return Serializers[t](data);
            lock (Serializers) {
                if (!Serializers.ContainsKey(t)) {
                    Serializers.Add(t, GetCompiledResult(t, true).GetResult() as Func<object, Pair>);
                }
            }
            return Serializers[t](data);
        }

        public static string Serialize(object data) {
            if (data == null)
                return "#nil";

            var t = data.GetType();

            if (IsBaseType(t)) {
                return Pair.O2String(data);
            }

            return "'" + Serialize(data, t);
        }

        public static object Deserialize(Pair data, Type t) {
            if (data == null)
                return null;

            if (IsBaseType(t))
                throw new ArgumentException(
                    "This method should not be used with base types (Primitive, Decimal, String, DateTime)");


            if (Deserializers.ContainsKey(t))
                 return Deserializers[t](data);
            lock (Deserializers) {
                if (!Deserializers.ContainsKey(t)) {
                    Deserializers.Add(t, GetCompiledResult(t, false).GetResult() as Func<Pair, object>);
                }
            }
            return Deserializers[t](data);
        }

        private static IResult GetCompiledResult(Type target, bool forward) {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var DOMref =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(obj => !obj.IsDynamic)
                    .Select(obj => obj.Location)
                    .ToList();

            var currentAssembly = Assembly.GetExecutingAssembly();
            DOMref.Add(currentAssembly.Location);

            var cp = new CompilerParameters(DOMref.ToArray());
            cp.GenerateInMemory = true;

            var className = "SerializerFactory";
            var outputType = (forward ? "Func<object, Pair>" : "Func<Pair, object>");
            var arg = "data";

            var srcBuilder = new StringBuilder();

            // REQUIRED ANYWAY
            foreach (var nmsp in RequiredNamespaces) {
                srcBuilder.AppendLine($"using {nmsp};");
            }

            // ADDITIONAL FOR CONCRETE TYPE
            foreach (var nmsp in GetTypeNamespaces(target).Where(t => !RequiredNamespaces.Contains(t))) {
                srcBuilder.AppendLine($"using {nmsp};");
            }

            srcBuilder.AppendLine("namespace Ogam {");
            srcBuilder.AppendLine($"public partial class {className} : IResult {{");

            #region IResult.GetResult

            srcBuilder.AppendLine("public Delegate GetResult() {");
            srcBuilder.AppendLine($"return new {outputType}(({arg})=>{{");
            srcBuilder.AppendLine(GenerateTypeDef(target, forward, arg));
            srcBuilder.AppendLine("});");
            srcBuilder.AppendLine("}");

            #endregion

            srcBuilder.AppendLine("} // cls");
            srcBuilder.AppendLine("} // ns");

            // COMPILATION
            var src = srcBuilder.ToString();
            var cr = provider.CompileAssemblyFromSource(cp, src);

            if (cr.Errors.Count > 0) {
                var e = new Exception("Error in OgamSerializer dynamic compile module. See details in data property...");
                foreach (CompilerError ce in cr.Errors) {
                    e.Data[ce.ErrorNumber] = $"{target.Assembly}|{target.Name}  {ce.ToString()}";
                }
                throw e;
            } else {
                var type = cr.CompiledAssembly.GetType("Ogam." + className);
                var obj = (IResult)Activator.CreateInstance(type);
                return obj;
            }
            return null;
        }

        private static ICollection<string> GetTypeNamespaces(Type t) {
            var res = new List<string> { t.Namespace };

            if (t.IsGenericType) {
                Array.ForEach<Type>(t.GetGenericArguments(), tt => res.AddRange(GetTypeNamespaces(tt)));
            }

            foreach (var mb in t.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {
                if (mb is FieldInfo) {
                    var f = (FieldInfo)mb;
                    if (IsBaseType(f.FieldType))
                        continue;
                    res.AddRange(GetTypeNamespaces(f.FieldType));
                } else if (mb is PropertyInfo) {
                    var p = (PropertyInfo)mb;
                    if (IsBaseType(p.PropertyType))
                        continue;
                    res.AddRange(GetTypeNamespaces(p.PropertyType));
                }
            }
            return res.Distinct().ToList();
        }

        private static string GetGenericTypeArguments(Type t, bool withTypeName = true) {
            var res = string.Empty;
            if (t.IsGenericType) {
                Array.ForEach<Type>(t.GetGenericArguments(), tt => res += ", " + GetGenericTypeArguments(tt));
                res = res.Substring(2);
                res = "<" + res;
                res += ">";
            }
            res = withTypeName ? t.Name + res : res;
            return Regex.Replace(res, "`[0-9]", string.Empty);
        }

        private static bool IsBaseType(Type t) {
            return t.IsPrimitive || t == typeof(string) || t == typeof(DateTime) || t == typeof(Decimal);
        }

        private static string GenerateTypeDef(Type t, bool forward, string arg) {
            if (forward) {
                #region SERIALIZER DEFINITION

                StringBuilder res = new StringBuilder();
                res.AppendLine("var result = new Pair();");
                res.AppendLine("var current = result;");

                if (t.GetInterfaces().Any(ie => ie == typeof(ICollection))) {
                    var internalType = t.GetInterface("ICollection`1").GetGenericArguments()[0];
                    var internalTypeFullName = GetGenericTypeArguments(internalType);

                    var defaultCondition = internalType.IsValueType
                        ? $"el.Equals(default({internalTypeFullName}))"
                        : "el==null";
                    var defaultValue = internalType.IsEnum
                        ? $"OgamSerializer.Serialize(default({internalTypeFullName}), typeof({internalTypeFullName}))"
                        : "null";

                    res.AppendLine($"foreach (var el in (ICollection){arg})");
                    res.AppendLine("{");

                    res.AppendLine(!IsBaseType(internalType)
                        ? $"var elPair = ({defaultCondition}) ? {defaultValue} : OgamSerializer.Serialize(el, typeof({internalTypeFullName}));"
                        : $"var elPair = el;");
                    res.AppendLine("current = current.Add(elPair);");

                    res.AppendLine("}");
                } else if (t.IsEnum) {
                    res.AppendLine($"current = current.Add((int){arg});");
                } else {
                    res.AppendLine($"var argCasted = ({GetGenericTypeArguments(t)}){arg};");

                    foreach (var mb in t.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {
                        if (mb is FieldInfo) {
                            var f = (FieldInfo)mb;

                            if (f.IsLiteral)
                                continue;

                            var internalTypeFullName = GetGenericTypeArguments(f.FieldType);
                            var defaultCondition = f.FieldType.IsValueType
                                ? $"argCasted.{f.Name}.Equals(default({internalTypeFullName}))"
                                : $"argCasted.{f.Name}==null";

                            res.AppendLine("{");

                            res.AppendLine(IsBaseType(f.FieldType)
                                ? $"var val = argCasted.{f.Name};"
                                : $"var val = ({defaultCondition}) ? null : OgamSerializer.Serialize(argCasted.{f.Name}, typeof({internalTypeFullName}));");

                            res.AppendLine(f.FieldType.GetInterfaces().Any(ie => ie == typeof(ICollection))
                                ? $"current = current.Add(new Pair(\"{f.Name}\".ToSymbol(), val==null ? null : new Pair(val)));"
                                : $"current = current.Add(new Pair(\"{f.Name}\".ToSymbol(), val));");

                            res.AppendLine("}");
                        } else if (mb is PropertyInfo) {
                            var p = (PropertyInfo)mb;

                            var internalTypeFullName = GetGenericTypeArguments(p.PropertyType);
                            var defaultCondition = p.PropertyType.IsValueType
                                ? $"argCasted.{p.Name}.Equals(default({internalTypeFullName}))"
                                : $"argCasted.{p.Name}==null";

                            res.AppendLine("{");

                            res.AppendLine(IsBaseType(p.PropertyType)
                                ? $"var val = argCasted.{p.Name};"
                                : $"var val = ({defaultCondition}) ? null : OgamSerializer.Serialize(argCasted.{p.Name}, typeof({internalTypeFullName}));");

                            res.AppendLine(p.PropertyType.GetInterfaces().Any(ie => ie == typeof(ICollection))
                                ? $"current = current.Add(new Pair(\"{p.Name}\".ToSymbol(), val==null ? null : new Pair(val)));"
                                : $"current = current.Add(new Pair(\"{p.Name}\".ToSymbol(), val));");

                            res.AppendLine("}");
                        }
                    }
                }
                res.AppendLine("return result;");
                return res.ToString();

                #endregion
            } else {
                #region DESERIALIZER DEFINITION

                StringBuilder res = new StringBuilder();
                res.AppendLine($"var result = new {GetGenericTypeArguments(t)}();");

                if (t.GetInterfaces().Any(ie => ie == typeof(ICollection))) {
                    var internalType = t.GetInterface("ICollection`1").GetGenericArguments()[0];
                    var internalTypeFullName = GetGenericTypeArguments(internalType);

                    var addStatement1 = internalType.IsValueType ? "" : "if (elem != null) ";
                    var addStatement2 = t.GetInterfaces().Any(ie => ie == typeof(IDictionary))
                        ? "elem.Key, elem.Value"
                        : "elem";

                    res.AppendLine("while (true)");
                    res.AppendLine("{");

                    res.AppendLine($"var p = {arg}.AsObject();");
                    res.AppendLine($"if (p == null) break;");

                    if (IsBaseType(internalType))
                        res.AppendLine($"var elem = Convert.To{internalType.Name}(p);");
                    else {
                        res.AppendLine($"var el = {arg}.AsObject() as Pair;");
                        res.AppendLine(
                            $"var elem = ({internalTypeFullName})OgamSerializer.Deserialize(el, typeof({internalTypeFullName}));");
                    }
                    res.AppendLine($"{addStatement1}result.Add({addStatement2});");
                    res.AppendLine($"if ({arg}.MoveNext() == null) break;");

                    res.AppendLine("}");
                } else if (t.IsEnum) {
                    res.AppendLine($"result = ({t.Name}){arg}.AsObject();");
                } else {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                        var types = t.GetGenericArguments();
                        res.AppendLine("var Key=result.Key;");
                        res.AppendLine("var Value=result.Value;");
                        {
                            var type = types[0];
                            var typeFullName = GetGenericTypeArguments(type);

                            res.AppendLine("{");
                            res.AppendLine($"var pair = {arg}.AsObject() as Pair;");
                            res.AppendLine("var value = pair.Cdr;");

                            res.AppendLine(IsBaseType(type)
                                ? $"Key =(value==null) ? default({type.Name}) : Convert.To{type.Name}(value);"
                                : $"Key = ((Pair)value==null) ? default({typeFullName}) : ({typeFullName})OgamSerializer.Deserialize((Pair)value, typeof({typeFullName}));");
                            res.AppendLine($"{arg}.MoveNext();");
                            res.AppendLine("}");
                        }
                        {
                            var type = types[1];
                            var typeFullName = GetGenericTypeArguments(type);

                            res.AppendLine("{");
                            res.AppendLine($"var pair = {arg}.AsObject() as Pair;");
                            res.AppendLine("var value = pair.Cdr;");

                            res.AppendLine(IsBaseType(type)
                                ? $"Value =(value==null) ? default({type.Name}) : Convert.To{type.Name}(value);"
                                : $"Value = ((Pair)value==null) ? default({typeFullName}) : ({typeFullName})OgamSerializer.Deserialize((Pair)value, typeof({typeFullName}));");
                            res.AppendLine($"{arg}.MoveNext();");
                            res.AppendLine("}");
                        }
                        res.AppendLine($"result = new {GetGenericTypeArguments(t)}(Key, Value);");
                    } else {
                        res.AppendLine($"Pair p = {arg}; ");
                        res.AppendLine("while (p != null) ");
                        res.AppendLine("{");
                        res.AppendLine("var car = p.Car as Pair; ");
                        res.AppendLine("if (car != null) ");
                        res.AppendLine("switch (car.Car.ToString())");
                        res.AppendLine("{");

                        foreach (var mb in t.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {
                            if (mb is FieldInfo) {
                                var f = (FieldInfo)mb;

                                if (f.IsLiteral)
                                    continue;

                                var type = f.FieldType;
                                var typeFullName = GetGenericTypeArguments(type);

                                res.AppendLine($"case \"{f.Name}\":");
                                res.AppendLine("{");

                                res.AppendLine(type.GetInterfaces().Any(ie => ie == typeof(ICollection))
                                    ? "var prevalue = (Pair)car.Cdr; var value = prevalue==null?null:prevalue.Car as Pair;"
                                    : "var value = car.Cdr;");

                                res.AppendLine(IsBaseType(type)
                                    ? $"result.{f.Name} = (value==null) ? default({type.Name}) : Convert.To{type.Name}(value);"
                                    : $"result.{f.Name} = ((Pair)value==null) ? default({typeFullName}) : ({typeFullName})OgamSerializer.Deserialize((Pair)value, typeof({typeFullName}));");

                                res.AppendLine("}");
                                res.AppendLine("break;");
                            } else if (mb is PropertyInfo) {
                                var p = (PropertyInfo)mb;

                                var type = p.PropertyType;
                                var typeFullName = GetGenericTypeArguments(type);

                                res.AppendLine($"case \"{p.Name}\":");
                                res.AppendLine("{");

                                res.AppendLine(type.GetInterfaces().Any(ie => ie == typeof(ICollection))
                                    ? "var prevalue = (Pair)car.Cdr; var value = prevalue==null?null:prevalue.Car as Pair;"
                                    : "var value = car.Cdr;");

                                res.AppendLine(IsBaseType(type)
                                    ? $"result.{p.Name} = (value==null) ? default({type.Name}) : Convert.To{type.Name}(value);"
                                    : $"result.{p.Name} = ((Pair)value==null) ? default({typeFullName}) : ({typeFullName})OgamSerializer.Deserialize((Pair)value, typeof({typeFullName}));");

                                res.AppendLine("}");
                                res.AppendLine("break;");
                            }
                        }

                        res.AppendLine("}");
                        res.AppendLine("p = p.Cdr as Pair; ");
                        res.AppendLine("}");
                    }
                }
                res.AppendLine("return result;");
                return Regex.Replace(res.ToString(), "`[0-9]", string.Empty);

                #endregion
            }
        }
    }

    public interface IResult {
        Delegate GetResult();
    }
}