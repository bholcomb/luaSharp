using System;
using System.Runtime.InteropServices;


namespace Lua
{
    public class LuaState
    {
        LuaCSFunction myPrintFuction;
        IntPtr myStatePtr;
        LuaObject myGlobalTable;

        public LuaState()
        {
            myStatePtr = LuaDLL.luaL_newstate();
            LuaDLL.luaL_openlibs(myStatePtr);

            LuaDLL.lua_getglobal(myStatePtr, "_G");
            myGlobalTable = new LuaObject(this, -1);

            myPrintFuction = new LuaCSFunction(print);

            LuaDLL.lua_pushcclosure(myStatePtr, myPrintFuction, 0);
            LuaDLL.lua_setglobal(myStatePtr, "print");

            printCallback = new PrintCallback(defaultPrint);
        }

        public IntPtr statePtr { get { return myStatePtr; } }
        public delegate void PrintCallback(String s);
        public PrintCallback printCallback { get; set; }

        void defaultPrint(String s)
        {
            Console.Write(s);
        }

        int print(IntPtr state)
        {
            int n = LuaDLL.lua_gettop(state); //number of arguments
            LuaDLL.lua_getglobal(state, "tostring");
            for (int i = 1; i <= n; i++)
            {
                String s;
                LuaDLL.lua_pushvalue(state, -1); //function to be called
                LuaDLL.lua_pushvalue(state, i); //value to print
                LuaDLL.lua_call(state, 1, 1);
                s = getValue<String>(-1);
                if (s == null)
                {
                    return LuaDLL.luaL_error(state, "\"tostring\" must return a string to \"print\"");
                }
                if (i > 1)
                {
                    printCallback("\t");
                }

                printCallback(s);
                LuaDLL.lua_pop(state, 1);
            }

            printCallback("\n");
            return 0;
        }

        public int doString(String s)
        {
            return LuaDLL.luaL_dostring(myStatePtr, s);
        }

        public int doFile(String s)
        {
            return LuaDLL.luaL_dofile(myStatePtr, s);
        }

        public LuaObject findObject(String name)
        {
            using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(this))
            {
                return myGlobalTable[name];
            }
        }

        public LuaObject createTable()
        {
            using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(this))
            {
                LuaDLL.lua_createtable(myStatePtr, 0/*narr*/, 0/*nrec*/);
                return new LuaObject(this, -1);
            }
        }

        public LuaTypes getType(int index)
        {
            return (LuaTypes)LuaDLL.lua_type(myStatePtr, index);
        }

        public T getValue<T>(int index)
        {
            if (typeof(T) == typeof(String))
            {
                IntPtr strPtr = LuaDLL.lua_tostring(statePtr, index);

                //allow for null strings
                if (strPtr == IntPtr.Zero)
                    return (T)Convert.ChangeType(null, typeof(T));

                String str = Marshal.PtrToStringAnsi(strPtr);
                return (T)Convert.ChangeType(str, typeof(T));
            }

            if (typeof(T) == typeof(bool))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_toboolean(statePtr, index) == 0 ? false : true, typeof(T));
            }

            if (typeof(T) == typeof(float))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_tonumber(statePtr, index), typeof(T));
            }

            if (typeof(T) == typeof(double))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_tonumber(statePtr, index), typeof(T));
            }

            if (typeof(T) == typeof(Int32))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_tointeger(statePtr, index), typeof(T));
            }

            if (typeof(T) == typeof(UInt32))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_tointeger(statePtr, index), typeof(T));
            }

            if (typeof(T) == typeof(UInt64))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_tointeger(statePtr, index), typeof(T));
            }

            if (typeof(T) == typeof(Int64))
            {
                return (T)Convert.ChangeType(LuaDLL.lua_tointeger(statePtr, index), typeof(T));
            }

            if (typeof(T) == typeof(LuaObject))
            {
                LuaObject temp = new LuaObject(this, index);
                return (T)Convert.ChangeType(temp, typeof(T));
            }

            throw new Exception("Can't get this type");
        }

        public void pushValue<T>(T value)
        {
            if (typeof(T) == typeof(String))
            {
                LuaDLL.lua_pushstring(statePtr, (String)Convert.ChangeType(value, typeof(String)));
                return;
            }

            if (typeof(T) == typeof(bool))
            {
                LuaDLL.lua_pushboolean(statePtr, (int)Convert.ChangeType(value, typeof(int)));
                return;
            }

            if (typeof(T) == typeof(float))
            {
                LuaDLL.lua_pushnumber(statePtr, (float)Convert.ChangeType(value, typeof(float)));
                return;
            }

            if (typeof(T) == typeof(double))
            {
                LuaDLL.lua_pushnumber(statePtr, (double)Convert.ChangeType(value, typeof(double)));
                return;
            }

            if (typeof(T) == typeof(Int32))
            {
                LuaDLL.lua_pushnumber(statePtr, (Int32)Convert.ChangeType(value, typeof(Int32)));
                return;
            }

            if (typeof(T) == typeof(UInt32))
            {
                LuaDLL.lua_pushnumber(statePtr, (UInt32)Convert.ChangeType(value, typeof(UInt32)));
                return;
            }

            if (typeof(T) == typeof(UInt64))
            {
                LuaDLL.lua_pushnumber(statePtr, (UInt64)Convert.ChangeType(value, typeof(UInt64)));
                return;
            }

            if (typeof(T) == typeof(Int64))
            {
                LuaDLL.lua_pushnumber(statePtr, (Int64)Convert.ChangeType(value, typeof(Int64)));
                return;
            }

            if (typeof(T) == typeof(LuaObject))
            {
                (value as LuaObject).push();
                return;
            }

            throw new Exception("Can't push this type on");
        }

        public int getReference(int index)
        {
            LuaDLL.lua_pushvalue(myStatePtr, index);
            return LuaDLL.luaL_ref(myStatePtr, (int)Lua.LuaStackConstants.LUA_REGISTRYINDEX);
        }

        public void unreference(int reference)
        {
            LuaDLL.luaL_unref(myStatePtr, (int)Lua.LuaStackConstants.LUA_REGISTRYINDEX, reference);
        }

        public void pushReference(int reference)
        {
            LuaDLL.lua_rawgeti(myStatePtr, (int)Lua.LuaStackConstants.LUA_REGISTRYINDEX, reference);
        }

        public void clearStack()
        {
            LuaDLL.lua_settop(myStatePtr, 0);
        }

        public int getTop()
        {
            return LuaDLL.lua_gettop(myStatePtr);
        }

        public void setTop(int top)
        {
            LuaDLL.lua_settop(myStatePtr, top);
        }

        public void stackDump()
        {
            int stackTop = getTop();
            printCallback("---------------Stack Dump---------------\n");
            if (stackTop == 0)
            {
                printCallback("Empty Stack\n");
                return;
            }

            for (int i = 1; i <= stackTop; i++)
            {
                int type = LuaDLL.lua_type(myStatePtr, i);
                printCallback(String.Format("{0}: Type={1}---Value=", i, getType(i)));
                printValue(i);
                printCallback("\n");
            }
        }

        public void printValue(int index)
        {
            LuaTypes type = getType(index);
            switch (type)
            {
                case LuaTypes.NIL:
                    printCallback("NIL");
                    break;
                case LuaTypes.BOOLEAN:
                    printCallback(getValue<string>(index));
                    break;
                case LuaTypes.LIGHTUSERDATA:
                    printCallback(getValue<string>(index));
                    break;
                case LuaTypes.NUMBER:
                    printCallback(getValue<string>(index));
                    break;
                case LuaTypes.STRING:
                    printCallback(getValue<string>(index));
                    break;
                case LuaTypes.TABLE:
                    printCallback(LuaDLL.lua_topointer(myStatePtr, index).ToString());
                    printCallback("\n");
                    printTable(index);
                    break;
                case LuaTypes.FUNCTION:
                    printCallback(LuaDLL.lua_tocfunction(myStatePtr, index).ToString());
                    break;
                case LuaTypes.USERDATA:
                    printCallback(LuaDLL.lua_touserdata(myStatePtr, index).ToString());
                    break;
                case LuaTypes.THREAD:
                    printCallback("Thread: no usable value");
                    break;
                default:
                    break;
            }
        }

        public void printTable(int index)
        {
            if (LuaDLL.lua_istable(myStatePtr, index) == false)
            {
                printCallback(String.Format("LuaState.PrintTable()-Cannot print non-table at index {0}\n", index));
                return;
            }

            LuaDLL.lua_pushnil(myStatePtr);
            while (LuaDLL.lua_next(myStatePtr, index) != 0)
            {
                printCallback(String.Format("\t[{0}]=", getValue<string>(-2)));
                switch (getType(-1))
                {
                    case LuaTypes.NIL:
                        printCallback("NIL");
                        break;
                    case LuaTypes.BOOLEAN:
                        printCallback(String.Format("Bool: {0}", getValue<string>(-1)));
                        break;
                    case LuaTypes.LIGHTUSERDATA:
                        printCallback(String.Format("Light userdata: {0}", getValue<string>(-1)));
                        break;
                    case LuaTypes.NUMBER:
                        printCallback(String.Format("Number: {0}", getValue<string>(-1)));
                        break;
                    case LuaTypes.STRING:
                        printCallback(String.Format("String: {0}", getValue<string>(-1)));
                        break;
                    case LuaTypes.TABLE:
                        printCallback(String.Format("Table: {0}", LuaDLL.lua_topointer(myStatePtr, -1).ToString()));
                        break;
                    case LuaTypes.FUNCTION:
                        printCallback(String.Format("Function: {0}", LuaDLL.lua_tocfunction(myStatePtr, -1).ToString()));
                        break;
                    case LuaTypes.USERDATA:
                        printCallback(String.Format("Userdata: {0}", LuaDLL.lua_touserdata(myStatePtr, -1).ToString()));
                        break;
                    case LuaTypes.THREAD:
                        printCallback("Thread: no usable value");
                        break;
                }

                printCallback("\n");
                LuaDLL.lua_pop(myStatePtr, 1);
            }
        }

        public void close()
        {
            LuaDLL.lua_close(myStatePtr);
        }

        public void error()
        {
            LuaDLL.lua_error(myStatePtr);
        }
    }
}