using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Lua
{
   public abstract class BaseLuaDecoder
   {
      public BaseLuaDecoder() { }
      public abstract Object get(LuaState state, int index);
   }

   public abstract class BaseLuaEncoder
   {
      public BaseLuaEncoder() { }
      public abstract void push(LuaState state, Object o);
   }

   public class LuaState
   {
      LuaCSFunction myPrintFuction;
      IntPtr myStatePtr;
      LuaObject myGlobalTable;

      public Dictionary<Type, BaseLuaDecoder> myDecoders = new Dictionary<Type, BaseLuaDecoder>();
      public Dictionary<Type, BaseLuaEncoder> myEncoders = new Dictionary<Type, BaseLuaEncoder>();

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

         initEncoders();
         initDecoders();
      }

      public void close()
      {
         LuaDLL.lua_close(myStatePtr);
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

      public LuaObject this[string index]
      {
         get { return myGlobalTable[index]; }
      }

      public LuaObject this[int index]
      {
         get { return myGlobalTable[index]; }
      }

      public LuaObject global { get { return myGlobalTable; } }

      public bool doFile(String s)
      {
         int ret = LuaDLL.luaL_dofile(myStatePtr, s);
         if (ret != 0)
         {
            printCallback(getValue<string>(-1));
         }

         return ret == 0;
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
         BaseLuaDecoder getter = null;
         if (myDecoders.TryGetValue(typeof(T), out getter) == true)
         {
            return (T)getter.get(this, index);
         }

         throw new Exception("Can't get this type");
      }

      public void pushValue<T>(T value)
      {
         BaseLuaEncoder setter = null;
         if (myEncoders.TryGetValue(typeof(T), out setter) == true)
         {
            setter.push(this, value);
         }
         else
         {
            throw new Exception("Can't push this type on");
         }
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

#region decoders
      void initDecoders()
      {
         myDecoders[typeof(String)] = new StringDecoder();
         myDecoders[typeof(UInt32)] = new NumberDecoder<UInt32>();
         myDecoders[typeof(Int32)] = new NumberDecoder<Int32>();
         myDecoders[typeof(UInt64)] = new NumberDecoder<UInt64>();
         myDecoders[typeof(Int64)] = new NumberDecoder<Int64>();
         myDecoders[typeof(float)] = new NumberDecoder<float>();
         myDecoders[typeof(double)] = new NumberDecoder<double>();
         myDecoders[typeof(bool)] = new BoolDecoder();
         myDecoders[typeof(LuaObject)] = new LuaObjectDecoder();
      }

      class StringDecoder : BaseLuaDecoder
      {
         public override Object get(LuaState state, int index)
         {
            IntPtr strPtr = LuaDLL.lua_tostring(state.statePtr, index);

            //allow for null strings
            if (strPtr == IntPtr.Zero)
               return "";

            String str = Marshal.PtrToStringAnsi(strPtr);
            return str;
         }
      }

      class NumberDecoder<T> : BaseLuaDecoder
      {
         public override Object get(LuaState state, int index)
         {
            double val = LuaDLL.lua_tonumber(state.statePtr, index);
            return (T)Convert.ChangeType(val, typeof(T));
         }
      }

      class BoolDecoder : BaseLuaDecoder
      {
         public override Object get(LuaState state, int index)
         {
            return LuaDLL.lua_toboolean(state.statePtr, index) != 0;
         }
      }

      class LuaObjectDecoder : BaseLuaDecoder
      {
         public override object get(LuaState state, int index)
         {
            LuaObject ret = new LuaObject(state, index);
            return ret;
         }
      }

#endregion

#region encoders
      void initEncoders()
      {
         myEncoders[typeof(String)] = new StringEncoder();
         myEncoders[typeof(UInt32)] = new NumberEncoder<UInt32>();
         myEncoders[typeof(Int32)] = new NumberEncoder<Int32>();
         myEncoders[typeof(UInt64)] = new NumberEncoder<UInt64>();
         myEncoders[typeof(Int64)] = new NumberEncoder<Int64>();
         myEncoders[typeof(float)] = new NumberEncoder<float>();
         myEncoders[typeof(double)] = new NumberEncoder<double>();
         myEncoders[typeof(bool)] = new BoolEncoder();
         myEncoders[typeof(LuaObject)] = new LuaObjectEncoder();
      }

      class StringEncoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            LuaDLL.lua_pushstring(state.statePtr, (String)o);
         }
      }

      class NumberEncoder<T> : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            double d = (double)Convert.ChangeType(o, typeof(double));
            LuaDLL.lua_pushnumber(state.statePtr, d);
         }
      }

      class BoolEncoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            int v = (bool)o == true ? 1 : 0;
            LuaDLL.lua_pushboolean(state.statePtr, v);
         }
      }

      class LuaObjectEncoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            (o as LuaObject).push();
         }
      }
      #endregion
   }
}