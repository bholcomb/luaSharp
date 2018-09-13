using System;

using OpenTK;
using OpenTK.Graphics;

using Lua;

namespace LuaExt
{
   public static class LuaOpenTK
   {
      public static void extendLua(LuaState state)
      {
         state.myDecoders[typeof(Vector3)] = new Vector3Decoder();
         state.myDecoders[typeof(Color4)] = new Color4Decoder();
         state.myDecoders[typeof(Matrix4)] = new Matrix4Decoder();
         state.myDecoders[typeof(Quaternion)] = new QuaternionDecoder();

         state.myEncoders[typeof(Vector3)] = new Vector3Encoder();
         state.myEncoders[typeof(Color4)] = new Color4Encoder();
         state.myEncoders[typeof(Matrix4)] = new Matrix4Encoder();
         state.myEncoders[typeof(Quaternion)] = new QuaternionEncoder();
      }

      #region Vector3
      public class Vector3Decoder : BaseLuaDecoder
      {
         public override object get(LuaState state, int index)
         {
            LuaObject temp = state.getValue<LuaObject>(index);
            float x = temp["x"];
            float y = temp["y"];
            float z = temp["z"];
            temp.Dispose();
            return new Vector3(x, y, z);
         }
      }

      public class Vector3Encoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            Vector3 v = (Vector3)o;
            LuaObject temp = state.createTable();
            temp.set<float>(v.X, "x");
            temp.set<float>(v.Y, "y");
            temp.set<float>(v.Z, "z");
            temp.push();
            temp.Dispose();
         }
      }
      #endregion

      #region Color4
      public class Color4Decoder : BaseLuaDecoder
      {
         public override object get(LuaState state, int index)
         {
            LuaObject temp = state.getValue<LuaObject>(index);
            float r = temp["r"];
            float g = temp["g"];
            float b = temp["b"];
            float a = temp.contains("a") ? temp["a"] : 1.0f;
            temp.Dispose();
            return new Color4(r, g, b, a);
         }
      }

      public class Color4Encoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            Color4 v = (Color4)o;
            LuaObject temp = state.createTable();
            temp.set<float>(v.R, "r");
            temp.set<float>(v.G, "g");
            temp.set<float>(v.B, "b");
            temp.set<float>(v.A, "a");
            temp.push();
            temp.Dispose();
         }
      }
      #endregion

      #region Matrix4
      public class Matrix4Decoder : BaseLuaDecoder
      {
         public override object get(LuaState state, int index)
         {
            LuaObject temp = state.getValue<LuaObject>(index);
            float[] v = new float[16];
            for (int i=0; i<16; i++)
            {
               v[i] = (float)temp[i + 1];
            }
            temp.Dispose();
            return new Matrix4(
               v[0], v[1], v[2], v[3],
               v[4], v[5], v[6], v[7],
               v[8], v[9], v[10], v[11],
               v[12], v[13], v[14], v[15]);
         }
      }

      public class Matrix4Encoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            Matrix4 mat = (Matrix4)o;
            float[] v = new float[16];
            v[00] = mat.Column0.X;
            v[01] = mat.Column1.X;
            v[02] = mat.Column2.X;
            v[03] = mat.Column3.X;
            v[04] = mat.Column0.Y;
            v[05] = mat.Column1.Y;
            v[06] = mat.Column2.Y;
            v[07] = mat.Column3.Y;
            v[08] = mat.Column0.Z;
            v[09] = mat.Column1.Z;
            v[10] = mat.Column2.Z;
            v[11] = mat.Column3.Z;
            v[12] = mat.Column0.W;
            v[13] = mat.Column1.W;
            v[14] = mat.Column2.W;
            v[15] = mat.Column3.W;

            LuaObject temp = state.createTable();
            for(int i=0; i < 16; i++)
            {
               temp.set<float>(v[i], i + 1);
            }

            temp.push();
            temp.Dispose();
         }
      }
      #endregion

      #region Quaternion
      public class QuaternionDecoder : BaseLuaDecoder
      {
         public override object get(LuaState state, int index)
         {
            LuaObject temp = state.getValue<LuaObject>(index);
            float x = temp["x"];
            float y = temp["y"];
            float z = temp["z"];
            float w = temp["w"];
            temp.Dispose();
            return new Quaternion(x, y, z, w);
         }
      }

      public class QuaternionEncoder : BaseLuaEncoder
      {
         public override void push(LuaState state, object o)
         {
            Quaternion q = (Quaternion)o;
            LuaObject temp = state.createTable();
            temp.set<float>(q.X, "x");
            temp.set<float>(q.Y, "y");
            temp.set<float>(q.Z, "z");
            temp.set<float>(q.W, "w");
            temp.push();
            temp.Dispose();
         }
      }
      #endregion
   }
}