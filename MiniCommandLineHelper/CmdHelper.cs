using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            try
            {
                var command = args[0];
                var userCommandArgs = args.Skip(1).ToArray();

                var assembly = Assembly.GetEntryAssembly();
                var mainProgram = (from type in assembly.GetTypes() where type.Name == "Program" select type).First();
                var methodInfo = mainProgram.GetMethods().First(method => method.Name.ToLower() == command.ToLower() && method.IsDefined(typeof(CommandAttribute)));

                var commandArgs = Utility.CombineParameters(userCommandArgs, methodInfo.GetParameters());
                methodInfo.Invoke(this, commandArgs);
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot execute arguments: {0}", String.Join(" ", args));
                Console.Read();
            }
        }

        protected void Help()
        {
            var assembly = Assembly.GetExecutingAssembly();

            //print out all StressCommands
            var commands = from type in assembly.GetTypes()
                                 where type.Name == "Program"
                                 from method in type.GetMethods()
                                 where
                                     Attribute.IsDefined(method, typeof(CommandAttribute)) &&
                                     Attribute.IsDefined(method, typeof(CommandLineAttribute))
                                 select method.GetCustomAttribute(typeof(CommandLineAttribute)) as CommandLineAttribute;

            var helpList = new List<CommandLineAttribute>();
            helpList.AddRange(commands);

            foreach (var commandLineAttr in helpList)
            {
                // ReSharper disable once PossibleNullReferenceException
                var help = commandLineAttr.Help;
                Console.WriteLine("{0}", help);
            }
        }
    }
}
