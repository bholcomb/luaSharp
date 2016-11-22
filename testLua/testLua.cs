using System;
using System.Collections.Generic;

using Lua;

namespace luaTest
{
   class Program
   {
      static LuaState theLuaVm;

      static void Main(string[] args)
      {
         theLuaVm = new LuaState();

         bool quit=false;
         System.Console.WriteLine("Lua Test Interpretor");
         while (!quit)
         {
            System.Console.Write("> ");
            string line = System.Console.ReadLine();
            if (line == "quit" || line == "exit")
            {
               quit = true;
               continue;
            }

            if (line == "stackdump")
            {
               theLuaVm.stackDump();
            }

            try
            {
               theLuaVm.doString(line);
            }
            catch
            {
               System.Console.WriteLine("Lua Error");
            }
         }
      }
   }
}
