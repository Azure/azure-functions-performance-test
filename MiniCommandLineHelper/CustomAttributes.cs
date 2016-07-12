using System;

namespace MiniCommandLineHelper
{
    /// <summary>
    /// Attribute used for organizing the Help message
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CommandLineAttribute : Attribute
    {
        private readonly string _commandLineHelp;

        public string Help
        {
            get { return _commandLineHelp; }
        }

        public CommandLineAttribute(string commandLineHelp)
        {
            _commandLineHelp = commandLineHelp;
        }
    }

    /// <summary>
    /// Group a test case into a category
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class TestCategoryAttribute : Attribute
    {
        private readonly TestCategories _category;

        public TestCategories TestCategory
        {
            get { return _category;}    
        }

        public TestCategoryAttribute(TestCategories category)
        {
            _category = category;
        }
    }

    /// <summary>
    /// A specific test
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AntaresTest : Attribute
    {
        
    }

    /// <summary>
    /// Command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        
    }
}
