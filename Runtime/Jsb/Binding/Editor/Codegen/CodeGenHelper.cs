#if UNITY_EDITOR || JSB_RUNTIME_REFLECT_BINDING
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace QuickJS.Binding
{
    public class CSDebugCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public CSDebugCodeGen(CodeGenerator cg)
        {
            this.cg = cg;
            if (cg.bindingManager.prefs.debugCodegen)
            {
                this.cg.cs.AppendLine("/*");
            }
        }

        public void Dispose()
        {
            if (cg.bindingManager.prefs.debugCodegen)
            {
                this.cg.cs.AppendLine("*/");
            }
        }
    }

    public class CSTopLevelCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public CSTopLevelCodeGen(CodeGenerator cg, TypeBindingInfo typeBindingInfo)
        {
            this.cg = cg;
            this.AppendCommonHead();
            // this.cg.typescript.AppendLine("// {0} {1}", Environment.UserName, this.cg.bindingManager.dateTime);
        }

        public CSTopLevelCodeGen(CodeGenerator cg, string name)
        {
            this.cg = cg;
            this.AppendCommonHead();
            this.cg.cs.AppendLine("// Special: {0}", name);
        }

        private void AppendCommonHead()
        {
#if !JSB_UNITYLESS
            this.cg.cs.AppendLine("// Unity: {0}", UnityEngine.Application.unityVersion);
#endif
            this.cg.cs.AppendLine("using System;");
            this.cg.cs.AppendLine("using System.Collections.Generic;");
            this.cg.cs.AppendLine();
        }

        private void AppendCommonTail()
        {
        }

        public void Dispose()
        {
            AppendCommonTail();
        }
    }

    public class CSNamespaceCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        protected string csNamespace;

        public CSNamespaceCodeGen(CodeGenerator cg, string csNamespace, params string[] csUsings)
        {
            this.cg = cg;
            this.csNamespace = csNamespace;

            if (!string.IsNullOrEmpty(csNamespace))
            {
                this.cg.cs.AppendLine("namespace {0} {{", csNamespace);
                this.cg.cs.AddTabLevel();
            }

            this.cg.cs.AppendLine("using QuickJS.Errors;");
            this.cg.cs.AppendLine("using JSValue = QuickJS.Native.JSValue;");
            this.cg.cs.AppendLine("using JSApi = QuickJS.Native.JSApi;");
            this.cg.cs.AppendLine("using JSNative = QuickJS.JSNative;");
            this.cg.cs.AppendLine("using JSContext = QuickJS.Native.JSContext;");
            this.cg.cs.AppendLine("using Values = QuickJS.Binding.Values;");
            this.cg.cs.AppendLine("using ScriptEngine = QuickJS.ScriptEngine;");
            this.cg.cs.AppendLine("using JSBindingAttribute = QuickJS.JSBindingAttribute;");
            this.cg.cs.AppendLine("using MonoPInvokeCallbackAttribute = QuickJS.MonoPInvokeCallbackAttribute;");
            foreach (var @using in csUsings)
            {
                this.cg.cs.AppendLine("using {0};", @using);
            }
            // this.cg.cs.AppendLine("using QuickJS;");
            // this.cg.cs.AppendLine("using QuickJS.Binding;");
            // this.cg.cs.AppendLine("using QuickJS.Native;");
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(csNamespace))
            {
                this.cg.cs.DecTabLevel();
                this.cg.cs.AppendLine("}");
            }
        }
    }

    public class TSNamespaceCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        protected string tsNamespace;

        public TSNamespaceCodeGen(CodeGenerator cg, string tsNamespace)
        {
            this.cg = cg;
            this.tsNamespace = tsNamespace;

            if (!string.IsNullOrEmpty(tsNamespace))
            {
                this.cg.tsDeclare.AppendLine($"namespace {this.tsNamespace} {{");
                this.cg.tsDeclare.AddTabLevel();
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(tsNamespace))
            {
                this.cg.tsDeclare.DecTabLevel();
                this.cg.tsDeclare.AppendLine("}");
            }
        }
    }

    public class RegFuncCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public RegFuncCodeGen(CodeGenerator cg)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("public static {0} Bind({1} register)", 
                this.cg.bindingManager.GetCSTypeFullName(typeof(ClassDecl)),
                this.cg.bindingManager.GetCSTypeFullName(typeof(TypeRegister)));
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    public class RuntimeRegFuncCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public RuntimeRegFuncCodeGen(CodeGenerator cg)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("public static void Bind({0} runtime)", 
                this.cg.bindingManager.GetCSTypeFullName(typeof(ScriptRuntime)));
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    public class TypeCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        protected TypeBindingInfo typeBindingInfo;

        public TypeCodeGen(CodeGenerator cg, TypeBindingInfo typeBindingInfo)
        {
            this.cg = cg;
            this.typeBindingInfo = typeBindingInfo;
            this.cg.cs.AppendLine("// Assembly: {0}", typeBindingInfo.Assembly.GetName());
            this.cg.cs.AppendLine("// Location: {0}", typeBindingInfo.Assembly.Location);
            this.cg.cs.AppendLine("// Type: {0}", typeBindingInfo.FullName);
            this.cg.cs.AppendLine("[{0}]", nameof(JSBindingAttribute));
            // this.cg.cs.AppendLine("[UnityEngine.Scripting.Preserve]");
            this.cg.cs.AppendLine("public class {0}", typeBindingInfo.csBindingName);
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public string GetTypeName(Type type)
        {
            return type.FullName.Replace(".", "_");
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    public class PreservedCodeGen : IDisposable
    {
        public PreservedCodeGen(CodeGenerator cg)
        {
            cg.cs.AppendLine("[UnityEngine.Scripting.Preserve]");
        }

        public void Dispose()
        {
        }
    }

    public class PInvokeGuardCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public PInvokeGuardCodeGen(CodeGenerator cg, Type target)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("[MonoPInvokeCallbackAttribute(typeof({0}))]", target.FullName);
        }

        public void Dispose()
        {
        }
    }

    public class BindingGetterFuncDeclareCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public BindingGetterFuncDeclareCodeGen(CodeGenerator cg, string name)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("public static JSValue {0}(JSContext ctx, JSValue this_obj)", name);
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    public class BindingSetterFuncDeclareCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public BindingSetterFuncDeclareCodeGen(CodeGenerator cg, string name)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("public static JSValue {0}(JSContext ctx, JSValue this_obj, JSValue arg_val)", name);
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    // 方法绑定
    public class BindingFuncDeclareCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public BindingFuncDeclareCodeGen(CodeGenerator cg, Type func_decl, string name)
        {
            this.cg = cg;
            var invoke = func_decl.GetMethod("Invoke");
            this.cg.cs.Append("public static JSValue {0}(", name);
            var parameters = invoke.GetParameters();
            for (int i = 0, len = parameters.Length; i < len; ++i)
            {
                var p = parameters[i];
                if (i != len - 1)
                {
                    this.cg.cs.AppendL("{0} {1}, ", cg.bindingManager.GetCSTypeFullName(p.ParameterType), p.Name);
                }
                else
                {
                    this.cg.cs.AppendL("{0} {1}", cg.bindingManager.GetCSTypeFullName(p.ParameterType), p.Name);
                }
            }
            this.cg.cs.AppendLineL(")");
            // this.cg.cs.AppendLine("public static JSValue {0}(JSContext ctx, JSValue this_obj, int argc, JSValue[] argv)", name);
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    // 构造方法绑定
    public class BindingConstructorDeclareCodeGen : IDisposable
    {
        protected CodeGenerator cg;

        public BindingConstructorDeclareCodeGen(CodeGenerator cg, string name)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("public static JSValue {0}(JSContext ctx, JSValue new_target, int argc, JSValue[] argv, int magic)", name);
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    public class TryCatchGuradCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        public TryCatchGuradCodeGen(CodeGenerator cg)
        {
            this.cg = cg;
            this.cg.cs.AppendLine("try");
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
        }

        private string GetVarName(Type type)
        {
            var name = type.Name;
            if (char.IsLower(name[0]))
            {
                return name + "_";
            }
            return char.ToLower(name[0]) + name.Substring(1);
        }

        private void AddCatchClause(Type exceptionType)
        {
            var varName = GetVarName(exceptionType);
            this.cg.cs.AppendLine("catch ({0} {1})", exceptionType.Name, varName);
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
            {
                this.cg.cs.AppendLine("return JSNative.ThrowException(ctx, {0});", varName);
            }
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }

        public virtual void Dispose()
        {
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
            this.AddCatchClause(typeof(Exception));
        }
    }

    public class CSEditorOnlyCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        protected string requiredDefines;

        public CSEditorOnlyCodeGen(CodeGenerator cg, IEnumerable<string> requiredDefines)
        : this(cg, (string)(requiredDefines != null ? string.Join(" && ", from def in requiredDefines select def) : null))
        {
        }

        public CSEditorOnlyCodeGen(CodeGenerator cg, string requiredDefines)
        {
            this.cg = cg;
            this.requiredDefines = requiredDefines;
            if (!string.IsNullOrEmpty(requiredDefines))
            {
                cg.cs.AppendLineL("#if {0}", requiredDefines);
            }
        }

        public CSEditorOnlyCodeGen(CodeGenerator cg, bool isEditorRuntime = true)
        : this(cg, isEditorRuntime ? "UNITY_EDITOR" : null)
        {
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(requiredDefines))
            {
                cg.cs.AppendLineL("#endif");
            }
        }
    }

    public class CSTypeRegisterScopeCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        protected string name;

        public CSTypeRegisterScopeCodeGen(CodeGenerator cg, string name, string contextName)
        {
            this.cg = cg;
            this.name = name;
            this.cg.cs.AppendLine("{");
            this.cg.cs.AddTabLevel();
            this.cg.cs.AppendLine("var {0} = {1}.CreateTypeRegister();", name, contextName);
        }

        public void Dispose()
        {
            this.cg.cs.AppendLine("{0}.Finish();", name);
            this.cg.cs.DecTabLevel();
            this.cg.cs.AppendLine("}");
        }
    }

    public class CSPlatformCodeGen : IDisposable
    {
        protected CodeGenerator cg;
        protected TypeBindingFlags bf;
        protected string predef;

        public CSPlatformCodeGen(CodeGenerator cg, TypeBindingFlags bf)
        {
            this.cg = cg;
            this.bf = bf;
            this.predef = string.Empty;

#if !JSB_UNITYLESS
            if ((this.bf & TypeBindingFlags.BuildTargetPlatformOnly) != 0)
            {
                var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
                switch (buildTarget)
                {
                    case UnityEditor.BuildTarget.Android:
                        predef = "UNITY_ANDROID";
                        break;
                    case UnityEditor.BuildTarget.iOS:
                        predef = "UNITY_IOS";
                        break;
                    case UnityEditor.BuildTarget.WSAPlayer:
                        predef = "UNITY_WSA"; // not supported
                        break;
                    case UnityEditor.BuildTarget.StandaloneWindows:
                    case UnityEditor.BuildTarget.StandaloneWindows64:
                        predef = "UNITY_STANDALONE_WIN";
                        break;
                    case UnityEditor.BuildTarget.StandaloneOSX:
                        predef = "UNITY_STANDALONE_OSX";
                        break;
                    case UnityEditor.BuildTarget.StandaloneLinux64:
                        predef = "UNITY_STANDALONE_LINUX";
                        break;
                    default:
                        predef = string.Format("false // {0} is not supported", buildTarget);
                        break;
                }
            }
#endif

            if (!string.IsNullOrEmpty(this.predef))
            {
                cg.cs.AppendLineL("#if {0}", this.predef);
            }
        }


        public void Dispose()
        {
            if (!string.IsNullOrEmpty(predef))
            {
                cg.cs.AppendLineL("#endif");
            }
        }
    }
}
#endif
