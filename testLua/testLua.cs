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

            bool quit = false;
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
                else if(line == "stackdump")
                {
                    theLuaVm.stackDump();
                }

                else
                {
                    var result = theLuaVm.doString(line);
                    if (LuaThreadStatus.LUA_ERRSYNTAX == (LuaThreadStatus)result)
                    {
                        System.Console.WriteLine("Lua Error");
                    }
                }


            }

            theLuaVm.close();
        }
    }
}
