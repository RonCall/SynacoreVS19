using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SynacoreVS19
{
    class Program
    {
        static void Main(string[] args)
        {
            var sy = new Synacor();

            sy.Load();
            sy.Go();
        }
    }
}
