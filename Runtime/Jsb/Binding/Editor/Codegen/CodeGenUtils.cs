#if UNITY_EDITOR || JSB_RUNTIME_REFLECT_BINDING
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace QuickJS.Binding
{
    public static class CodeGenUtils
    {
        public const string CodeEmitWarning = "Codegen for delegate binding with ref/out parameters will not work properly, because CodeDomProvider is not supported with current dotnet api compatibility settings. Please consider switching to .NET 4.6 in player settings.";

        public static bool IsCodeEmitSupported()
        {
#if JSB_UNITYLESS
#if NETCOREAPP
            return false;
#else
            return true;
#endif
#else
            var apiCompatibilityLevel = UnityEditor.PlayerSettings.GetApiCompatibilityLevel(UnityEditor.BuildTargetGroup.Standalone);
            return apiCompatibilityLevel == UnityEditor.ApiCompatibilityLevel.NET_4_6;
#endif
        }

        /// <summary>
        /// Compiles C# source into assembly, usually used for dynamically generating delegate function with ref/out parameters at runtime.
        /// NOTE: It will directly return null if CodeDom is not supported without throwing any exception.
        /// </summary>
        public static Assembly Compile(string source, IEnumerable<Assembly> referencedAssemblies, string compilerOptions, IBindingLogger logger)
        {
#if !(NETCOREAPP || NET_STANDARD_2_0 || NET_STANDARD_2_1 || NET_STANDARD)
            using (var codeDomProvider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("cs"))
            {
                var compilerParameters = new System.CodeDom.Compiler.CompilerParameters();
                compilerParameters.GenerateInMemory = true;
                compilerParameters.TreatWarningsAsErrors = false;
                compilerParameters.CompilerOptions = compilerOptions;
                // compilerParameters.TempFiles = new System.CodeDom.Compiler.TempFileCollection("Temp", false);
                compilerParameters.OutputAssembly = "Temp/_Generated_" + Guid.NewGuid().ToString() + ".dll";
                compilerParameters.ReferencedAssemblies.AddRange((from a in referencedAssemblies select a.Location).ToArray());
                var result = codeDomProvider.CompileAssemblyFromSource(compilerParameters, source);

                if (result.Errors.HasErrors)
                {
                    if (logger != null)
                    {
                        logger.LogError($"failed to compile source [{result.Errors.Count} errors]\nSource: {source}");
                        foreach (var err in result.Errors)
                        {
                            logger.LogError(err.ToString());
                        }
                    }
                }
                else
                {
                    return result.CompiledAssembly;
                }
            }
#endif
            return null;
        }

        /// <summary>
        /// Check if the type directly implements the given interface
        /// </summary>
        public static bool IsDirectlyImplements(Type type, Type interfaceType)
        {
            return type.BaseType != null && interfaceType.IsAssignableFrom(type) && !interfaceType.IsAssignableFrom(type.BaseType);
        }

        public static void RemoveAt<T>(ref T[] array, int index)
        {
#if JSB_UNITYLESS
            for (var i = index; i < array.Length - 1; i++)
            {
                array[i] = array[i + 1];
            }
            Array.Resize(ref array, array.Length - 1);
#else
            UnityEditor.ArrayUtility.RemoveAt(ref array, index);
#endif
        }

        public static string ToExpression(bool v)
        {
            return v ? "true" : "false";
        }

        /// <summary>
        /// remove part of generic definition from TS naming result
        /// </summary>
        public static string StripGenericDeclaration(string name)
        {
            var index = name.IndexOf("<");
            return index < 0 ? name : name.Substring(0, index);
        }

        /// <summary>
        /// remove part of generic definition from CSharp type name
        /// </summary>
        public static string StripGenericDefinition(string name)
        {
            var index = name.IndexOf("`");
            return index < 0 ? name : name.Substring(0, index);
        }

        public static string[] Strip(string[] values, string additional)
        {
            var list = new List<string>(values.Length + 1);
            list.AddRange(values);
            list.Add(additional);
            return Strip(list.ToArray());
        }

        /// <summary>
        /// return an array copy without empty elements
        /// </summary>
        public static string[] Strip(params string[] values)
        {
            return (from value in values where !string.IsNullOrEmpty(value) select value).ToArray();
        }

        public static string Join(string sp, string left, string right)
        {
            if (right.Length > 0)
            {
                return left.Length > 0 ? left + sp + right : right;
            }
            return left;
        }

        public static string Join(string sp, string[] values, int offset, int count)
        {
            var r = "";
            for (int i = 0; i < count; ++i)
            {
                var value = values[i + offset];
                if (value.Length != 0)
                {
                    r += i != count - 1 ? value + sp : value;
                }
            }
            return r;
        }

        public static string Join(string sp, params string[] values)
        {
            return string.Join(sp, Strip(values));
        }

        // 将类型名转换成简单字符串 (比如用于文件名)
        public static string GetFileName(Type type)
        {
            string typeName = "";
            if (type.IsGenericType)
            {
                typeName += type.Name.Substring(0, type.Name.IndexOf('`'));
                foreach (var gp in type.GetGenericArguments())
                {
                    typeName += "_" + gp.Name;
                }
            }
            else
            {
                typeName = type.IsArray ? type.Name + "_array" : type.Name;
            }

            if (type.DeclaringType != null)
            {
                return GetFileName(type.DeclaringType) + "_" + typeName;
            }

            if (string.IsNullOrEmpty(type.Namespace))
            {
                return typeName;
            }

            return type.Namespace.Replace(".", "_") + "_" + typeName;
        }

        public static bool IsValidTypeDeclarationName(string name)
        {
            return !(
                name.Contains(".") ||
                name.Contains("<") ||
                name.Contains(">") ||
                name.Contains("`") ||
                name.Contains("+") ||
                name.Contains(" ") ||
                name.Contains(",") ||
                name.Contains("=")
            );
        }

        /// <summary>
        /// concat strings as: "value1", "value2", "value3"
        /// </summary>
        public static string JoinExpression(string sp, params string[] values)
        {
            return string.Join(sp, from value in values select $"\"{value}\"");
        }

        // 保证生成一个以 prefix 为前缀, 与参数列表中所有参数名不同的名字
        public static string GetUniqueName(ParameterInfo[] parameters, string prefix)
        {
            return GetUniqueName(parameters, prefix, 0);
        }

        public static string GetUniqueName(ParameterInfo[] parameters, string prefix, int index)
        {
            var size = parameters.Length;
            var name = prefix + index;
            for (var i = 0; i < size; i++)
            {
                var parameter = parameters[i];
                if (parameter.Name == prefix)
                {
                    return GetUniqueName(parameters, prefix, index + 1);
                }
            }
            return name;
        }

        /// <summary>
        /// e.g ["TypeA", "TypeB", "ThisType"]
        /// </summary>
        public static string[] GetModuleRegistrationPathSlice(ITSTypeNaming tSTypeNaming)
        {
            return tSTypeNaming.classPath;
        }

        public static string GetTSClassName(TypeBindingInfo typeBindingInfo)
        {
            var tsTypeNaming = typeBindingInfo.tsTypeNaming;
            var type = typeBindingInfo.type;
            if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition)
                {
                    var args = string.Join(", ", from arg in type.GetGenericArguments() select arg.Name);
                    return string.Format("{0}<{1}>", tsTypeNaming.className, args);
                }
            }

            return Join("", tsTypeNaming.className, tsTypeNaming.genericDefinition);
        }

        [Conditional("JSB_DEBUG")]
        public static void Assert(bool condition, string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(condition, msg);
#endif
        }
    }
}

#endif
