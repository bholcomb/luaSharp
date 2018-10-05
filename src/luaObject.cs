using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;

namespace Lua
{
   public class LuaObject : LuaValue, IDisposable, IEnumerable<LuaObject>
   {
      protected bool disposed = false;
      protected int myReference;
      protected LuaState myState;

      static LuaCSFunction theErrorFunction;

      static LuaObject()
      {
         theErrorFunction = new LuaCSFunction(luaError);
      }

      public LuaObject(LuaState state, int index)
         :base()
      {
         myState=state;
         myReference = myState.getReference(index);
         switch (myState.getType(index))
         {
            case LuaTypes.NIL: myType = DataType.NIL; break;
            case LuaTypes.BOOLEAN: myType = DataType.BOOL; break;
            case LuaTypes.NUMBER: myType = DataType.DOUBLE; break;
            case LuaTypes.STRING: myType = DataType.STRING; break;
            case LuaTypes.USERDATA: myType = DataType.POINTER; break;
            case LuaTypes.TABLE: myType = DataType.TABLE; break;
            case LuaTypes.FUNCTION: myType = DataType.FUNCTION; break;
         }

      }

      public LuaObject(LuaObject obj)
      {
         myState = obj.myState;
         myReference = obj.myReference;
         myType = obj.myType;
      }

      public LuaState state { get { return myState; } }

      public void push()
      {
         myState.pushReference(myReference);
      }

      #region indexing
      public bool contains(String name)
      {
         if (myType != DataType.TABLE)
         {
            return false;
         }

         using(LuaAutoStackCleaner cleaner =new LuaAutoStackCleaner(myState))
         {
            push();
            String[] pieces = name.Split('.');
            foreach (String s in pieces)
            {
               if (s.Contains("[") == true)
               {
                  int arrayLocEnd = s.IndexOf("]");
                  String tableName = s.Substring(0, s.IndexOf("]"));
                  LuaDLL.lua_pushstring(myState.statePtr, tableName);
                  LuaDLL.lua_gettable(myState.statePtr, -2);

                  if (LuaDLL.lua_isnil(myState.statePtr, -1) == true)
                  {
                     return false;
                  }

                  string indexName = s.Substring(s.IndexOf("["), s.IndexOf("]"));
                  int n;
                  bool isNumeric = int.TryParse(indexName, out n);
                  if (isNumeric == true)
                  {
                     LuaDLL.lua_pushnumber(myState.statePtr, n);
                  }
                  else
                  {
                     LuaDLL.lua_pushstring(myState.statePtr, indexName);
                  }

                  LuaDLL.lua_gettable(myState.statePtr, -2);
                  if (stackValueOk() == false)
                     return false;
               }
               else
               {
                  LuaDLL.lua_pushstring(myState.statePtr, s);
                  LuaDLL.lua_gettable(myState.statePtr, -2);

                  if (LuaDLL.lua_isnil(myState.statePtr, -1) == true)
                  {
                     return false;
                  }
               }
            }

            return LuaDLL.lua_isnil(myState.statePtr, -1) == false;
         }         
      }

      public bool contains(int index)
      {
         if (myType != DataType.TABLE)
         {
            return false;
         }

         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
         {
            push();
            LuaDLL.lua_pushinteger(myState.statePtr, index);
            LuaDLL.lua_gettable(myState.statePtr, -2);
            return LuaDLL.lua_isnil(myState.statePtr, -1) == false;
         }
      }
 
      public T get<T>(int index)
      {
         if (myType != DataType.TABLE)
         {
            throw new Exception("Cannot set field on non-table");
         }

         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
         {
            push();
            LuaDLL.lua_pushnumber(myState.statePtr, index);
            LuaDLL.lua_gettable(myState.statePtr, -2);
            T temp = myState.getValue<T>(-1);
            LuaDLL.lua_pop(myState.statePtr, 2);
            return temp;
         }
      }

      public void set<T>(T value, int index)
      {
         if (myType != DataType.TABLE)
         {
            throw new Exception("Cannot set field on non-table");
         }

         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
         {
            push();
            LuaDLL.lua_pushnumber(myState.statePtr, index);
            myState.pushValue<T>(value);
            LuaDLL.lua_settable(myState.statePtr, -3);
         }
      }

      public T getOr<T>(String index, T def)
      {
         if(contains(index) == false)
         {
            return def;
         }

         return get<T>(index);
      }

      public T getOr<T>(int index, T def)
      {
         if (contains(index) == false)
         {
            return def;
         }

         return get<T>(index);
      }

      public T get<T>(String index)
      {
         if (myType != DataType.TABLE)
         {
            throw new Exception("Cannot get field on non-table");
         }

         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
         {
            push();
            String[] pieces = index.Split('.');
            foreach (String s in pieces)
            {
               if (s.Contains("[") == true)
               {
                  int arrayLocBegin = s.IndexOf("[");
                  int arrayLocEnd = s.IndexOf("]");
                  String tableName = s.Substring(0, arrayLocBegin);
                  LuaDLL.lua_pushstring(myState.statePtr, tableName);
                  LuaDLL.lua_gettable(myState.statePtr, -2);

                  if (LuaDLL.lua_isnil(myState.statePtr, -1) == true)
                  {
                     return (T)Convert.ChangeType(null, typeof(T));
                  }

                  string indexName = s.Substring(arrayLocBegin + 1, arrayLocEnd - arrayLocBegin - 1);
                  int n;
                  bool isNumeric = int.TryParse(indexName, out n);
                  if (isNumeric == true)
                  {
                     LuaDLL.lua_pushnumber(myState.statePtr, n);
                  }
                  else
                  {
                     LuaDLL.lua_pushstring(myState.statePtr, indexName);
                  }

                  LuaDLL.lua_gettable(myState.statePtr, -2);
                  if (stackValueOk() == false)
                  {
                     return (T)Convert.ChangeType(null, typeof(T));
                  }
               }
               else
               {
                  LuaDLL.lua_pushstring(myState.statePtr, s);
                  LuaDLL.lua_gettable(myState.statePtr, -2);
               }
            }

            T temp = myState.getValue<T>(-1);
            LuaDLL.lua_pop(myState.statePtr, 2);
            return temp;
         }
      }

      public void set<T>(T value, String index)
      {
         if (myType != DataType.TABLE)
         {
            throw new Exception("Cannot set field on non-table");
         }

         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
         {
            push();
            LuaDLL.lua_pushstring(myState.statePtr, index);
            myState.pushValue<T>(value);
            LuaDLL.lua_settable(myState.statePtr, -3);
         }
      }

      public LuaObject this[int index]
      {
         get
         {
            return get<LuaObject>(index);
         }
         set
         {
            set(value, index);
         }
      }

      public LuaObject this[string index]
      {
         get
         {
            return get<LuaObject>(index);
         }
         set
         {
            set(value, index);
         }
      }

      bool stackValueOk()
      {
         if (LuaDLL.lua_isfunction(myState.statePtr, -1) == true)
         {
            LuaDLL.lua_call(myState.statePtr, 0, 1);
         }

         //if it's nil
         if (LuaDLL.lua_isnil(myState.statePtr, -1))
         {
            return false;
         }

         return true;
      }
      #endregion

      #region function interface
      static int luaError(IntPtr state)
      {
         Console.WriteLine("Error running lua function");
         return 0;
      }

      public LuaObject call(params LuaValue[] parameters)
      {
         if (myType != DataType.FUNCTION)
         {
            return null;
         }

         using(LuaAutoStackCleaner auto=new LuaAutoStackCleaner(myState))
         {
            LuaDLL.lua_pushcfunction(myState.statePtr, theErrorFunction);
            int errorIndex=LuaDLL.lua_gettop(myState.statePtr);
            push();

            for(int i=0; i<parameters.Length; i++)
            {
               switch(parameters[i].type())
               {
                  case DataType.NIL: LuaDLL.lua_pushnil(myState.statePtr); break;
                  case DataType.BOOL: LuaDLL.lua_pushboolean(myState.statePtr, (int)parameters[i]); break;
                  case DataType.INT: LuaDLL.lua_pushnumber(myState.statePtr, (double)parameters[i]); break;
                  case DataType.FLOAT: LuaDLL.lua_pushnumber(myState.statePtr, (double)parameters[i]); break;
                  case DataType.DOUBLE: LuaDLL.lua_pushnumber(myState.statePtr, (double)parameters[i]); break;
                  case DataType.STRING: LuaDLL.lua_pushstring(myState.statePtr, (string)parameters[i]); break;
                  case DataType.TABLE: LuaObject obj = (LuaObject)parameters[i]; obj.push(); break;
               }          
            }

            int error=LuaDLL.lua_pcall(myState.statePtr, parameters.Length, LuaDLL.LUA_MULTRET, errorIndex);
            if(error!=0)
            {
               Console.Write("Error running function: ");
               Console.Write(LuaDLL.lua_tostring(myState.statePtr, -1));
            }

            return new LuaObject(myState, -1);
         }
      }

      #endregion

      #region implicit casting
      public static implicit operator String(LuaObject j)
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(j.myState))
         {
            j.push();
            IntPtr strPtr = LuaDLL.lua_tostring(j.myState.statePtr, -1);
            return Marshal.PtrToStringAnsi(strPtr);
         }
      }

      public static implicit operator double(LuaObject j)
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(j.myState))
         {
            j.push();
            return LuaDLL.lua_tonumber(j.myState.statePtr, -1);
         }
      }

      public static implicit operator float(LuaObject j)
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(j.myState))
         {
            j.push();
            double num= LuaDLL.lua_tonumber(j.myState.statePtr, -1);
            return (float)num;
         }
      }

      public static implicit operator long(LuaObject j)
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(j.myState))
         {
            j.push();
            return LuaDLL.lua_tointeger(j.myState.statePtr, -1);
         }
      }

      public static implicit operator int(LuaObject j)
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(j.myState))
         {
            j.push();
            Int64 num= LuaDLL.lua_tointeger(j.myState.statePtr, -1);
            return (int)num;
         }
      }

      public static implicit operator bool(LuaObject j)
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(j.myState))
         {
            j.push();
            return LuaDLL.lua_toboolean(j.myState.statePtr, -1)==0 ? false : true;
         }
      }

      public static implicit operator Vector3(LuaObject j)
      {
         Vector3 v = Vector3.Zero;
         if (j.myType == DataType.TABLE)
         {
            v.X = (float)j["x"];
            v.Y = (float)j["y"];
            v.Z = (float)j["z"];
         }

         return v;
      }

      public static implicit operator Color4(LuaObject j)
      {
         Color4 v = Color4.White;
         if (j.myType == DataType.TABLE)
         {
            v.R = (float)j["r"];
            v.G = (float)j["g"];
            v.B = (float)j["b"];
            if (j.contains("a"))
               v.A = (float)j["a"];
            else
               v.A = 1.0f;
         }

         return v;
      }

      #endregion

      #region enumerator interface
      public List<string> keys
      {
         get
         {
            List<String> keys = new List<string>();
            using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
            {
               push();
               LuaDLL.lua_pushnil(myState.statePtr);  //first key
               while (LuaDLL.lua_next(myState.statePtr, -2) != 0)
               {
                  // uses 'key' (at index -2) and 'value' (at index -1) 
                  if (myState.getType(-2) == Lua.LuaTypes.STRING)
                  {
                     IntPtr strPtr = LuaDLL.lua_tostring(myState.statePtr, -2);
                     String str = Marshal.PtrToStringAnsi(strPtr);
                     keys.Add(str);
                  }
                  else
                  {
                     //warn << "List contains something other than strings: " << lua_typename(myState, lua_type(myState, -1)) <<endl;
                  }

                  LuaDLL.lua_pop(myState.statePtr, 1);
               }
            }

            return keys;
         }
      }

      public IEnumerator<LuaObject> GetEnumerator()
      {
         List<String> keyList = this.keys;

         foreach (String s in keyList)
         {
            yield return this[s];
         }
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
         return this.GetEnumerator();
      }

      //this should only be used for luaObjects that are arrays
      public int count()
      {
         using (LuaAutoStackCleaner cleaner = new LuaAutoStackCleaner(myState))
         {
            push();
            return LuaDLL.lua_rawlen(myState.statePtr, -1);
         }
      }

      #endregion

      #region disposable pattern

      public void Dispose()
      {
         // Dispose of unmanaged resources.
         Dispose(true);
         // Suppress finalization.
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposed)
            return;

         if (disposing)
         {
            myState.unreference(myReference);
         }

         // Free any unmanaged objects here. 
         //
         disposed = true;
      }

      #endregion

   }
}