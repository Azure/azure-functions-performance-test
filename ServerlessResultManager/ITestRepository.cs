
using System;
using System.Collections.Generic;

namespace ServerlessResultManager
{
    interface ITestRepository
    {
        Test GetTest(int id);

        ICollection<Test> GetTestsAfter(DateTime dateFrom);
    }
}
