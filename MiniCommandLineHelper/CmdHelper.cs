using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiniCommandLineHelper
{
    public abstract class CmdHelper
    {
        public void Main(string[] args)
        {
            RunCommand(args);
        }

        //protected abstract Assembly ExecutingAssembly { get; }

        protected void RunCommand(string[] args)
        {
            MethodInfo methodInfo = null;

            try
            {
                var command = args[0];
                var userCommandArgs = args.Skip(1).ToArray();

                var assembly = Assembly.GetEntryAssembly();
                var mainProgram = (from type in assembly.GetTypes() where type.Name == "Program" select type).First();
                methodInfo = mainProgram.GetMethods().First(method => method.Name.ToLower() == command.ToLower() && method.IsDefined(typeof(CommandAttribute)));

                var commandArgs = Utility.CombineParameters(userCommandArgs, methodInfo.GetParameters());
                methodInfo.Invoke(this, commandArgs);
            }
            catch (Exception ex)
            {
                WriteMethodData(methodInfo);
                Console.WriteLine("Cannot execute arguments: {0}", String.Join(" ", args));
                var fgc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                var exception = ex.InnerException ?? ex;
                Console.WriteLine(exception);
                Console.ForegroundColor = fgc;
            }
        }

        private void WriteMethodData(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                Console.WriteLine("Method unknown. Allowed methods: {0}", Environment.NewLine);
                Help();
            }
            else
            {
                Help(methodInfo);                
            }
        }

        protected void Help()
        {
            var assembly = Assembly.GetEntryAssembly();

            //print out all StressCommands
            var methods = from type in assembly.GetTypes()
                                 where type.Name == "Program"
                                 from method in type.GetMethods()
                                 where
                                     Attribute.IsDefined(method, typeof(CommandAttribute)) &&
                                     Attribute.IsDefined(method, typeof(CommandLineAttribute))
                                 select method;
            
            
            foreach (var method in methods)
            {
                var methodInfoParsed = GetMethodInfoParsed(method);
                Console.WriteLine(methodInfoParsed);
                Console.WriteLine();
            }
        }

        protected void Help(MethodInfo method)
        {
            if (Attribute.IsDefined(method, typeof (CommandAttribute)) &&
                Attribute.IsDefined(method, typeof (CommandLineAttribute)))
            {
                Console.WriteLine(GetMethodInfoParsed(method));
                Console.WriteLine();
            }
        }

        private string GetMethodInfoParsed(MethodInfo method)
        {
            var info = new StringBuilder();
            var methodName = method.Name;
            var parameters = method.GetParameters();

            info.Append("    ");
            info.Append(methodName);
            info.Append(" ");

            foreach (var param in parameters)
            {
                var paramInfo = string.Empty;

                if (param.HasDefaultValue)
                {
                    paramInfo = string.Format(
                        "[-{0}:<{1}>:{2}]", 
                        param.Name,
                        GetTypeInfo(param.ParameterType),
                        param.DefaultValue);
                }
                else
                {
                    paramInfo = string.Format(
                        "<{0}:{1}>",
                        param.Name,
                        GetTypeInfo(param.ParameterType));
                }

                info.Append(paramInfo);
                info.Append(" ");
            }

            return info.ToString();
        }

        private string GetTypeInfo(Type type)
        {
            if (!type.IsEnum)
            {
                return type.Name;
            }
            else
            {
                return string.Join("|", Enum.GetNames(type));
            }
        }
    }
}
