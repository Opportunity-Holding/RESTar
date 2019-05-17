using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using RESTar.Resources;
using RESTar.WebSockets;

namespace RESTarExample
{
    /// <inheritdoc />
    /// <summary>
    /// Terminal to compile and run code in a restricted manner
    /// </summary>
    [RESTar]
    public class CShell : ITerminal
    {
        /// <inheritdoc />
        public IWebSocket WebSocket { private get; set; }

        /// <inheritdoc />
        public void Open() => WebSocket.SendText("Now open! Enter some C#!");

        /// <inheritdoc />
        public void HandleTextInput(string input) => WebSocket.SendJson(Eval(input));

        /// <inheritdoc />
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();

        /// <inheritdoc />
        public bool SupportsTextInput { get; } = true;

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; } = false;

        private static IEnumerable<object> Eval(string query)
        {
            var codeProvider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            ReferenceAssemblies(parameters);
            parameters.CompilerOptions = "/t:library";
            parameters.GenerateInMemory = true;
            var sb = new StringBuilder("");
            sb.Append("using System;\n");
            sb.Append("using System.Linq;\n");
            sb.Append("using System.Collections.Generic;\n");
            sb.Append("using Starcounter;\n");
            sb.Append("namespace RESTarExample.DynamicCodeGeneration { \n");
            sb.Append("public class DynamicClass{ \n");
            sb.Append("public IEnumerable<Object> Result(){\n");
            sb.Append($"return {query}; \n");
            sb.Append("} \n");
            sb.Append("} \n");
            sb.Append("}\n");
            var cResult = codeProvider.CompileAssemblyFromSource(parameters, sb.ToString());
            if (cResult.Errors.Count > 0)
            {
                var list = new List<object>();
                foreach (var error in cResult.Errors) list.Add(error);
                return list;
            }
            var obj = cResult.CompiledAssembly.CreateInstance("FacebookManager.DynamicCodeGeneration.DynamicClass");
            return obj?.GetType().GetMethod("Result")?.Invoke(obj, null) as IEnumerable<object>;
        }

        // Load core assemblies 
        private static void ReferenceAssemblies(CompilerParameters cp)
        {
            //var ass = GetCurrentAssemblyPaths();
            foreach (var assemblyPath in GetCurrentAssemblyPaths()) cp.ReferencedAssemblies.Add(assemblyPath);
            cp.ReferencedAssemblies.Add(Starcounter.Application.Current.FilePath);
            cp.ReferencedAssemblies.Add("System.Core.dll");
        }

        // Gets all the current loaded assemblies from FacebookManager project
        private static IEnumerable<string> GetCurrentAssemblyPaths()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            //return loadedAssemblies.Where(assembly => !assembly.IsDynamic).Select(assembly => assembly.Location).Where(l => !String.IsNullOrWhiteSpace(l));
            return Assembly
                .GetEntryAssembly()?
                .GetReferencedAssemblies()
                .Select(name => loadedAssemblies
                    .SingleOrDefault(a => a.FullName == name.FullName)?.Location)
                .Where(l => l != null);
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}