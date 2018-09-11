using System;
using System.Runtime.InteropServices;

namespace Lua
{
   public enum DataType
   {
      NIL,
      BOOL,
      INT,
      FLOAT,
      DOUBLE,
      STRING,
      POINTER,
      FUNCTION,
      TABLE
   }

   public class LuaValue
   {
      protected DataType myType;

      Object myValue;

      public LuaValue() { myType = DataType.NIL; }
      public LuaValue(bool v) { myValue = v; myType = DataType.BOOL; }
      public LuaValue(int v) { myValue = v; myType = DataType.INT; }
      public LuaValue(float v) { myValue = v; myType = DataType.FLOAT; }
      public LuaValue(double v) { myValue = v; myType = DataType.DOUBLE; }
      public LuaValue(string v) { myValue = v; myType = DataType.STRING; }
      public LuaValue(Object v) { myValue = v; myType = DataType.POINTER; }

      public LuaValue(LuaValue l)
      {
         myType = l.myType;
         myValue = l.myValue;
      }

      public DataType type()
      {
         return myType;
      }

      public bool isNil() { return myType == DataType.NIL; }
      public bool isBool() { return myType == DataType.BOOL; }
      public bool isInt() { return myType == DataType.INT; }
      public bool isFloat() { return myType == DataType.FLOAT; }
      public bool isDouble() { return myType == DataType.DOUBLE; }
      public bool isString() { return myType == DataType.STRING; }
      public bool isPtr() { return myType == DataType.POINTER; }
      public bool isTable() { return myType == DataType.TABLE; }
      public bool isFunction() { return myType == DataType.FUNCTION; }

      #region explicit casting
      public static explicit operator string(LuaValue j)
      {
         switch (j.myType)
         {
            case DataType.STRING:
               {
                  return System.Convert.ToString(j.myValue);
               }
            default:
               {
                  return "";
               }
         }
      }

      public static explicit operator double(LuaValue j)
      {
         switch (j.myType)
         {
            case DataType.INT:
               {
                  return System.Convert.ToDouble(j.myValue);
               }
            case DataType.DOUBLE:
               {
                  return System.Convert.ToDouble(j.myValue);
               }
            case DataType.FLOAT:
               {
                  return System.Convert.ToDouble(j.myValue);
               }
            default:
               {
                  return double.NaN;
               }
         }
      }

      public static explicit operator float(LuaValue j)
      {
         switch (j.myType)
         {
            case DataType.INT:
               {
                  return System.Convert.ToSingle(j.myValue);
               }
            case DataType.DOUBLE:
               {
                  return System.Convert.ToSingle(j.myValue);
               }
            case DataType.FLOAT:
               {
                  return System.Convert.ToSingle(j.myValue);
               }
            default:
               {
                  return float.NaN;
               }
         }
      }

      public static explicit operator int(LuaValue j)
      {
         switch (j.myType)
         {
            case DataType.BOOL:
               {
                  return System.Convert.ToInt32(j.myValue);
               }
            case DataType.INT:
               {
                  return System.Convert.ToInt32(j.myValue);
               }
            case DataType.FLOAT:
               {
                  return System.Convert.ToInt32(j.myValue);
               }
            case DataType.DOUBLE:
               {
                  return System.Convert.ToInt32(j.myValue);
               }
            default:
               {
                  return int.MaxValue;
               }
         }
      }

      public static explicit operator bool(LuaValue j)
      {
         switch (j.myType)
         {
            case DataType.BOOL:
               {
                  return System.Convert.ToBoolean(j.myValue);
               }
            case DataType.INT:
               {
                  return System.Convert.ToInt32(j.myValue) != 0;
               }
            case DataType.FLOAT:
               {
                  return System.Convert.ToSingle(j.myValue) != 0;
               }
            case DataType.DOUBLE:
               {
                  return System.Convert.ToDouble(j.myValue) != 0;
               }
            default:
               {
                  return false;
               }
         }
      }

      #endregion

      #region implicit assignment
      public static implicit operator LuaValue(double d)
      {
         return new LuaValue(d);
      }

      public static implicit operator LuaValue(float d)
      {
         return new LuaValue(d);
      }

      public static implicit operator LuaValue(int d)
      {
         return new LuaValue(d);
      }

      public static implicit operator LuaValue(String d)
      {
         return new LuaValue(d);
      }

      public static implicit operator LuaValue(bool d)
      {
         return new LuaValue(d);
      }

      #endregion
   }
}