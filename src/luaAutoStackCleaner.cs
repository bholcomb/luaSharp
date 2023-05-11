using System;

namespace Lua
{
   public class LuaAutoStackCleaner: IDisposable
   {
      LuaState myState;
      int myStackTop;
      bool disposed = false;

      public LuaAutoStackCleaner(LuaState state)
      {
         myState = state;
         myStackTop = myState.getTop();

      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (!disposed)
         {
            if (disposing)
            {
               myState.setTop(myStackTop);
               disposed = true;
            }
         }
      }
   }
}