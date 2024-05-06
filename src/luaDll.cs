using System.Runtime.InteropServices;
using System.Security;
using System.Reflection;
using System.IO;

namespace Lua
{
    #region typedefs

    //Delegate for functions passed to Lua as function pointers using cdecl vs the standard stdcall
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LuaCSFunction(IntPtr luaState);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LuaContinuationFunction(IntPtr luaState, int status, IntPtr ctx);

    // delegate for lua debug hook callback
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LuaHookFunction(IntPtr luaState, IntPtr luaDebug);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr lua_Alloc(IntPtr ud, IntPtr ptr, int osize, int nsize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr lua_Reader(IntPtr state, IntPtr ud, int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr lua_Writer(IntPtr state, IntPtr p, int size, IntPtr ud);

    public enum LuaStackConstants
    {
        LUA_MINSTACK = 20,
        LUA_MAXSTACK = 1000000,
        LUA_FIRSTPSEUDOIDX = (-LUA_MAXSTACK - 1000),
        LUA_REGISTRYINDEX = LUA_FIRSTPSEUDOIDX,
    }

    public enum LuaRegistryValues
    {
        LUA_RIDX_MAINTHREAD = 1,
        LUA_RIDX_GLOBALS = 2
    }

    public enum LuaThreadStatus
    {
        LUA_OK = 0,
        LUA_YIELD = 1,
        LUA_ERRRUN = 2,
        LUA_ERRSYNTAX = 3,
        LUA_ERRMEM = 4,
        LUA_ERRGCMM = 5,
        LUA_ERRERR = 6
    }

    public enum LuaTypes
    {
        NONE = -1,
        NIL = 0,
        BOOLEAN = 1,
        LIGHTUSERDATA = 2,
        NUMBER = 3,
        STRING = 4,
        TABLE = 5,
        FUNCTION = 6,
        USERDATA = 7,
        THREAD = 8
    };

    public enum LuaGcOpts
    {
        LUA_GCSTOP = 0,
        LUA_GCRESTART = 1,
        LUA_GCCOLLECT = 2,
        LUA_GCCOUNT = 3,
        LUA_GCCOUNTB = 4,
        LUA_GCSTEP = 5,
        LUA_GCSETPAUSE = 6,
        LUA_GCSETSTEPMUL = 7,
        LUA_GCISRUNNING = 9
    }

    public enum LuaEventCodes
    {
        LUA_HOOKCALL = 0,
        LUA_HOOKRET = 1,
        LUA_HOOKLINE = 2,
        LUA_HOOKCOUNT = 3,
        LUA_HOOKTAILRET = 4
    }

    [Flags]
    public enum LuaEventMasks
    {
        LUA_MASKCALL = (1 << LuaEventCodes.LUA_HOOKCALL),
        LUA_MASKRET = (1 << LuaEventCodes.LUA_HOOKRET),
        LUA_MASKLINE = (1 << LuaEventCodes.LUA_HOOKLINE),
        LUA_MASKCOUNT = (1 << LuaEventCodes.LUA_HOOKCOUNT),
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LuaDebug
    {
        public int evt;
        [MarshalAs(UnmanagedType.LPStr)]
        public String name; /* (n) */
        [MarshalAs(UnmanagedType.LPStr)]
        public String namewhat; /* (n) `global', `local', `field', `method' */
        [MarshalAs(UnmanagedType.LPStr)]
        public String what; /* (S) `Lua', `C', `main', `tail' */
        [MarshalAs(UnmanagedType.LPStr)]
        public String source;   /* (S) */
        public int currentline; /* (l) */
        public int linedefined; /* (S) */
        public int lastlinedefined; /* (S) */
        public byte nups;       /* (u) number of upvalues */
        public byte nparams; /* (u) number of parameters */
        public byte isvararg; /* (u) */
        public byte istailcall; /* (t) */
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60/*LUA_IDSIZE*/)]
        public string short_src; /* (S) */
        /* private part */
        IntPtr i_ci;  /* active function */
    }


    #endregion

    public static class LuaDLL
    {
        static class Constants
        {
            public const string LibraryName = "NativeLuaLib";
            public const string WindowsLibraryName = "lua5.4.dll";
            public const string LinuxLibraryName = "liblua5.4.so";
            public const string MacOsLibraryName = "lua5.4.dylib";
        }

        static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {          
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string libraryDirectory = Path.Combine(baseDirectory, "runtimes");
            string libraryPath = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
               libraryPath = Path.Combine(libraryDirectory, "win-x64", "native", Constants.WindowsLibraryName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
               libraryPath = Path.Combine(libraryDirectory, "linux-x64", "native", Constants.LinuxLibraryName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
               libraryPath = Path.Combine(libraryDirectory, "osx-x64", "native", Constants.MacOsLibraryName);
            }

            IntPtr handle;
            NativeLibrary.TryLoad(libraryPath, assembly, searchPath, out handle);
            return handle;
        }

        static LuaDLL()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
        }

        public const int LUA_MULTRET = -1;

        #region state manipulation
        /*
        LUA_API lua_State *(lua_newstate) (lua_Alloc f, void *ud);
        LUA_API void       (lua_close) (lua_State *L);
        LUA_API lua_State *(lua_newthread) (lua_State *L);
        LUA_API lua_CFunction (lua_atpanic) (lua_State *L, lua_CFunction panicf);
        */
        [DllImport(Constants.LibraryName, EntryPoint = "lua_newstate", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_newstate(lua_Alloc f, IntPtr ud);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_close", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_close(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_newthread", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_newthread(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_atpanic", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern LuaCSFunction lua_atpanic(IntPtr state, LuaCSFunction panicf);

        #endregion

        #region basic stack manipulation
        /*
  LUA_API int   (lua_absindex) (lua_State *L, int idx);
  LUA_API int   (lua_gettop) (lua_State *L);
  LUA_API void  (lua_settop) (lua_State *L, int idx);
  LUA_API void  (lua_pushvalue) (lua_State *L, int idx);
  LUA_API void  (lua_rotate) (lua_State *L, int idx, int n);
  LUA_API void  (lua_copy) (lua_State *L, int fromidx, int toidx);      
  LUA_API int   (lua_checkstack) (lua_State *L, int sz);
  LUA_API void  (lua_xmove) (lua_State *from, lua_State *to, int n);
  */

        [DllImport(Constants.LibraryName, EntryPoint = "lua_absindex", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_absindex(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_gettop", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_gettop(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_settop", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_settop(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushvalue", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushvalue(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rotate", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_rotate(IntPtr state, int idx, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_copy", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_copy(IntPtr state, int fromidx, int toidx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_checkstack", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_checkstack(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_xmove", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_xmove(IntPtr from, IntPtr to, int idx);
        #endregion

        #region access functions (stack -> C)
        /*
  LUA_API int             (lua_isnumber) (lua_State *L, int idx);
  LUA_API int             (lua_isstring) (lua_State *L, int idx);
  LUA_API int             (lua_iscfunction) (lua_State *L, int idx);
  LUA_API int             (lua_isinteger) (lua_State *L, int idx);
  LUA_API int             (lua_isuserdata) (lua_State *L, int idx);
  LUA_API int             (lua_type) (lua_State *L, int idx);
  LUA_API const char     *(lua_typename) (lua_State *L, int tp);

  LUA_API lua_Number      (lua_tonumberx) (lua_State *L, int idx, int *isnum);
  LUA_API lua_Integer     (lua_tointegerx) (lua_State *L, int idx, int *isnum);
  LUA_API int             (lua_toboolean) (lua_State *L, int idx);
  LUA_API const char     *(lua_tolstring) (lua_State *L, int idx, size_t *len);
  LUA_API size_t          (lua_rawlen) (lua_State *L, int idx);
  LUA_API lua_CFunction   (lua_tocfunction) (lua_State *L, int idx);
  LUA_API void	       *(lua_touserdata) (lua_State *L, int idx);
  LUA_API lua_State      *(lua_tothread) (lua_State *L, int idx);
  LUA_API const void     *(lua_topointer) (lua_State *L, int idx);

  */
        [DllImport(Constants.LibraryName, EntryPoint = "lua_isnumber", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_isnumber(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_isstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_isstring(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_iscfunction", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_iscfunction(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_isinteger", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_isinteger(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_isuserdata", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_isuserdata(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_type", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_type(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_typename", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_typename(IntPtr state, int tp);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_tonumberx", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern double lua_tonumberx(IntPtr state, int idx, IntPtr isnum);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_tointegerx", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern Int64 lua_tointegerx(IntPtr state, int idx, IntPtr isnum);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_toboolean", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_toboolean(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_tolstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_tolstring(IntPtr state, int idx, IntPtr len);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawlen", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_rawlen(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_tocfunction", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern LuaCSFunction lua_tocfunction(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_touserdata", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_touserdata(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_tothread", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_tothread(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_topointer", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_topointer(IntPtr state, int idx);

        #endregion

        #region comparison and arithmetic functions
        /*
  LUA_API void  (lua_arith) (lua_State *L, int op);

  LUA_API int   (lua_rawequal) (lua_State *L, int idx1, int idx2);
  LUA_API int   (lua_compare) (lua_State *L, int idx1, int idx2, int op);

         */

        public enum LuaOps
        {
            LUA_OPADD = 0,  /* ORDER TM, ORDER OP */
            LUA_OPSUB = 1,
            LUA_OPMUL = 2,
            LUA_OPMOD = 3,
            LUA_OPPOW = 4,
            LUA_OPDIV = 5,
            LUA_OPIDIV = 6,
            LUA_OPBAND = 7,
            LUA_OPBOR = 8,
            LUA_OPBXOR = 9,
            LUA_OPSHL = 10,
            LUA_OPSHR = 11,
            LUA_OPUNM = 12,
            LUA_OPBNOT = 13
        }

        public enum LuaCompare
        {
            LUA_OPEQ = 0,
            LUA_OPLT = 1,
            LUA_OPLE = 2
        }

        [DllImport(Constants.LibraryName, EntryPoint = "lua_arith", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_arith(IntPtr state, int op);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawequal", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_rawequal(IntPtr state, int idx1, int idx2);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_compare", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_compare(IntPtr state, int idx1, int idx2, int op);

        #endregion

        #region push functions (C -> stack)
        /*
  LUA_API void        (lua_pushnil) (lua_State *L);
  LUA_API void        (lua_pushnumber) (lua_State *L, lua_Number n);
  LUA_API void        (lua_pushinteger) (lua_State *L, lua_Integer n);
  LUA_API const char *(lua_pushlstring) (lua_State *L, const char *s, size_t len);
  LUA_API const char *(lua_pushstring) (lua_State *L, const char *s);
  LUA_API const char *(lua_pushvfstring) (lua_State *L, const char *fmt,
                                                        va_list argp);
  LUA_API const char *(lua_pushfstring) (lua_State *L, const char *fmt, ...);
  LUA_API void  (lua_pushcclosure) (lua_State *L, lua_CFunction fn, int n);
  LUA_API void  (lua_pushboolean) (lua_State *L, int b);
  LUA_API void  (lua_pushlightuserdata) (lua_State *L, void *p);
  LUA_API int   (lua_pushthread) (lua_State *L);
  */

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushnil", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushnil(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushnumber", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushnumber(IntPtr state, double n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushinteger", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushinteger(IntPtr state, Int64 n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushlstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr lua_pushlstring(IntPtr state, String s, int l);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_pushstring(IntPtr state, String s);

        // not implemented
        //       [DllImport(Constants.LibraryName, EntryPoint = "lua_pushvfstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        //       public static extern IntPtr lua_pushvfstring(IntPtr state, String s);

        //       [DllImport(Constants.LibraryName, EntryPoint = "lua_pushfstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        //       public static extern IntPtr lua_pushfstring(IntPtr state, String s);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushcclosure", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushcclosure(IntPtr state, LuaCSFunction func, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushboolean", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushboolean(IntPtr state, int b);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushlightuserdata", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_pushlightuserdata(IntPtr state, IntPtr p);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pushthread", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_pushthread(IntPtr state);

        #endregion

        #region  get functions (Lua -> stack)
        /*
  LUA_API int (lua_getglobal) (lua_State *L, const char *name);
  LUA_API int (lua_gettable) (lua_State *L, int idx);
  LUA_API int (lua_getfield) (lua_State *L, int idx, const char *k);
  LUA_API int (lua_geti) (lua_State *L, int idx, lua_Integer n);
  LUA_API int (lua_rawget) (lua_State *L, int idx);
  LUA_API int (lua_rawgeti) (lua_State *L, int idx, lua_Integer n);
  LUA_API int (lua_rawgetp) (lua_State *L, int idx, const void *p);

  LUA_API void  (lua_createtable) (lua_State *L, int narr, int nrec);
  LUA_API void *(lua_newuserdata) (lua_State *L, size_t sz);
  LUA_API int   (lua_getmetatable) (lua_State *L, int objindex);
  LUA_API int  (lua_getuservalue) (lua_State *L, int idx);
  */
        [DllImport(Constants.LibraryName, EntryPoint = "lua_getglobal", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_getglobal(IntPtr state, String name);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_gettable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_gettable(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getfield", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_getfield(IntPtr state, int idx, string k);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_geti", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_geti(IntPtr state, int idx, Int64 n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawget", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_rawget(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawgeti", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_rawgeti(IntPtr state, int idx, Int64 n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawgetp", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_rawgetp(IntPtr state, int idx, IntPtr p);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_createtable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_createtable(IntPtr state, int narr, int nrec);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_newuserdata", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_newuserdata(IntPtr state, int sz);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getmetatable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_getmetatable(IntPtr state, int objindex);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getuservalue", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_getuservalue(IntPtr state, int idx);


        #endregion

        #region set functions (stack -> Lua)
        /*
        LUA_API void  (lua_setglobal) (lua_State *L, const char *name);
        LUA_API void  (lua_settable) (lua_State *L, int idx);
        LUA_API void  (lua_setfield) (lua_State *L, int idx, const char *k);
        LUA_API void  (lua_seti) (lua_State *L, int idx, lua_Integer n);
        LUA_API void  (lua_rawset) (lua_State *L, int idx);
        LUA_API void  (lua_rawseti) (lua_State *L, int idx, lua_Integer n);
        LUA_API void  (lua_rawsetp) (lua_State *L, int idx, const void *p);
        LUA_API int   (lua_setmetatable) (lua_State *L, int objindex);
        LUA_API void  (lua_setuservalue) (lua_State *L, int idx);
        */
        [DllImport(Constants.LibraryName, EntryPoint = "lua_setglobal", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_setglobal(IntPtr state, String name);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_settable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_settable(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_setfield", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_setfield(IntPtr state, int idx, String k);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_seti", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_seti(IntPtr state, int idx, Int64 n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawset", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_rawset(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawseti", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_rawseti(IntPtr state, int idx, Int64 n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_rawsetp", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_rawsetp(IntPtr state, int idx, IntPtr p);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_setmetatable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_setmetatable(IntPtr state, int objindex);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_setuservalue", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_setuservalue(IntPtr state, int idx);

        #endregion

        #region `load' and `call' functions (load and run Lua code)
        /*
  LUA_API void  (lua_callk) (lua_State *L, int nargs, int nresults,
                             lua_KContext ctx, lua_KFunction k);
  #define lua_call(L,n,r)		lua_callk(L, (n), (r), 0, NULL)

  LUA_API int   (lua_pcallk) (lua_State *L, int nargs, int nresults, int errfunc,
                              lua_KContext ctx, lua_KFunction k);
  #define lua_pcall(L,n,r,f)	lua_pcallk(L, (n), (r), (f), 0, NULL)

  LUA_API int   (lua_load) (lua_State *L, lua_Reader reader, void *dt,
                            const char *chunkname, const char *mode);

  LUA_API int (lua_dump) (lua_State *L, lua_Writer writer, void *data, int strip);
  */

        [DllImport(Constants.LibraryName, EntryPoint = "lua_callk", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_callk(IntPtr state, int nargs, int nresults, IntPtr ctx, LuaContinuationFunction k);

        public static void lua_call(IntPtr state, int nargs, int nresults)
        {
            lua_callk(state, nargs, nresults, IntPtr.Zero, null);
        }

        [DllImport(Constants.LibraryName, EntryPoint = "lua_pcallk", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_pcallk(IntPtr state, int nargs, int nresults, int errfunc, IntPtr ctx, LuaContinuationFunction k);

        public static int lua_pcall(IntPtr state, int nargs, int nresults, int errfunc)
        {
            return lua_pcallk(state, nargs, nresults, errfunc, IntPtr.Zero, null);
        }

        [DllImport(Constants.LibraryName, EntryPoint = "lua_load", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_load(IntPtr state, lua_Reader reader, IntPtr dt, string chunkname, string mode);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_dump", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_dump(IntPtr state, lua_Writer writer, IntPtr data, int strip);

        #endregion

        #region coroutine functions
        /*
  LUA_API int  (lua_yieldk)     (lua_State *L, int nresults, lua_KContext ctx,
                                 lua_KFunction k);
  LUA_API int  (lua_resume)     (lua_State *L, lua_State *from, int narg);
  LUA_API int  (lua_status)     (lua_State *L);
  LUA_API int (lua_isyieldable) (lua_State *L);

  #define lua_yield(L,n)		lua_yieldk(L, (n), 0, NULL)
  */
        [DllImport(Constants.LibraryName, EntryPoint = "lua_yieldk", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_yieldk(IntPtr state, int nresults, IntPtr ctx, LuaContinuationFunction k);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_resume", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_resume(IntPtr state, IntPtr from, int narg);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_status", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_status(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_isyieldable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_isyieldable(IntPtr state);

        public static int lua_yield(IntPtr state, int nresults)
        {
            return lua_yieldk(state, nresults, IntPtr.Zero, null);
        }

        #endregion

        #region garbage-collection function and options

        /*
        LUA_API int (lua_gc) (lua_State *L, int what, int data);
         */

        [DllImport(Constants.LibraryName, EntryPoint = "lua_gc", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_gc(IntPtr state, int what, int data);
        #endregion

        #region miscellaneous functions
        /*
  LUA_API int   (lua_error) (lua_State *L);

  LUA_API int   (lua_next) (lua_State *L, int idx);

  LUA_API void  (lua_concat) (lua_State *L, int n);
  LUA_API void  (lua_len)    (lua_State *L, int idx);

  LUA_API size_t   (lua_stringtonumber) (lua_State *L, const char *s);

  LUA_API lua_Alloc (lua_getallocf) (lua_State *L, void **ud);
  LUA_API void      (lua_setallocf) (lua_State *L, lua_Alloc f, void *ud);


  */
        [DllImport(Constants.LibraryName, EntryPoint = "lua_error", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_error(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_next", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_next(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_concat", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_concat(IntPtr state, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_len", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_len(IntPtr state, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_stringtonumber", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_stringtonumber(IntPtr state, String s);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getallocf", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern lua_Alloc lua_getallocf(IntPtr state, IntPtr ppud);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_setallocf", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void lua_setallocf(IntPtr state, lua_Alloc func, IntPtr pudn);

        #endregion

        #region some useful macros (converted to functions)
        /*
  #define lua_getextraspace(L)	((void *)((char *)(L) - LUA_EXTRASPACE))

  #define lua_tonumber(L,i)	lua_tonumberx(L,(i),NULL)
  #define lua_tointeger(L,i)	lua_tointegerx(L,(i),NULL)

  #define lua_pop(L,n)		lua_settop(L, -(n)-1)

  #define lua_newtable(L)		lua_createtable(L, 0, 0)

  #define lua_register(L,n,f) (lua_pushcfunction(L, (f)), lua_setglobal(L, (n)))

  #define lua_pushcfunction(L,f)	lua_pushcclosure(L, (f), 0)

  #define lua_isfunction(L,n)	(lua_type(L, (n)) == LUA_TFUNCTION)
  #define lua_istable(L,n)	(lua_type(L, (n)) == LUA_TTABLE)
  #define lua_islightuserdata(L,n)	(lua_type(L, (n)) == LUA_TLIGHTUSERDATA)
  #define lua_isnil(L,n)		(lua_type(L, (n)) == LUA_TNIL)
  #define lua_isboolean(L,n)	(lua_type(L, (n)) == LUA_TBOOLEAN)
  #define lua_isthread(L,n)	(lua_type(L, (n)) == LUA_TTHREAD)
  #define lua_isnone(L,n)		(lua_type(L, (n)) == LUA_TNONE)
  #define lua_isnoneornil(L, n)	(lua_type(L, (n)) <= 0)

  #define lua_pushliteral(L, s)	\
     lua_pushlstring(L, "" s, (sizeof(s)/sizeof(char))-1)

  #define lua_pushglobaltable(L)  \
     lua_rawgeti(L, LUA_REGISTRYINDEX, LUA_RIDX_GLOBALS)

  #define lua_tostring(L,i)	lua_tolstring(L, (i), NULL)


  #define lua_insert(L,idx)	lua_rotate(L, (idx), 1)

  #define lua_remove(L,idx)	(lua_rotate(L, (idx), -1), lua_pop(L, 1))

  #define lua_replace(L,idx)	(lua_copy(L, -1, (idx)), lua_pop(L, 1))
  */
        public static IntPtr lua_getextraspace(IntPtr L)
        {
            Int64 ptrval = L.ToInt64();
            ptrval -= IntPtr.Size;
            return (IntPtr)ptrval;
        }

        public static double lua_tonumber(IntPtr L, int i)
        {
            return lua_tonumberx(L, i, IntPtr.Zero);
        }

        public static Int64 lua_tointeger(IntPtr L, int i)
        {
            return lua_tointegerx(L, i, IntPtr.Zero);
        }

        public static void lua_pop(IntPtr L, int n)
        {
            lua_settop(L, -(n) - 1);
        }

        public static void lua_newtable(IntPtr L)
        {
            lua_createtable(L, 0, 0);
        }

        public static void lua_register(IntPtr L, String name, LuaCSFunction func)
        {
            lua_pushcfunction(L, func);
            lua_setglobal(L, name);
        }

        public static void lua_pushcfunction(IntPtr L, LuaCSFunction func)
        {
            lua_pushcclosure(L, func, 0);
        }

        public static bool lua_isfunction(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.FUNCTION;
        }

        public static bool lua_istable(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.TABLE;
        }

        public static bool lua_islightuserdata(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.LIGHTUSERDATA;
        }

        public static bool lua_isnil(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.NIL;
        }

        public static bool lua_isboolean(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.BOOLEAN;
        }

        public static bool lua_isthread(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.THREAD;
        }

        public static bool lua_isnone(IntPtr L, int n)
        {
            return lua_type(L, n) == (int)LuaTypes.NONE;
        }

        public static bool lua_isnoneornil(IntPtr L, int n)
        {
            return lua_type(L, n) <= 0;
        }

        public static void lua_pushliteral(IntPtr L, String s)
        {
            lua_pushlstring(L, "" + s, (System.Text.Encoding.ASCII.GetByteCount(s) / sizeof(char)) - 1);
        }

        public static void lua_pushglobaltable(IntPtr L)
        {
            lua_rawgeti(L, (int)LuaStackConstants.LUA_REGISTRYINDEX, (Int64)LuaRegistryValues.LUA_RIDX_GLOBALS);
        }

        public static IntPtr lua_tostring(IntPtr L, int i)
        {
            return lua_tolstring(L, i, IntPtr.Zero);
        }

        public static void lua_insert(IntPtr L, int idx)
        {
            lua_rotate(L, idx, 1);
        }

        public static void lua_remove(IntPtr L, int idx)
        {
            lua_rotate(L, idx, -1);
            lua_pop(L, 1);
        }

        public static void lua_replace(IntPtr L, int idx)
        {
            lua_copy(L, -1, idx);
            lua_pop(L, 1);
        }

        #endregion

        #region compatibility macros for unsigned conversions
        /*
  #define lua_pushunsigned(L,n)	lua_pushinteger(L, (lua_Integer)(n))
  #define lua_tounsignedx(L,i,is)	((lua_Unsigned)lua_tointegerx(L,i,is))
  #define lua_tounsigned(L,i)	lua_tounsignedx(L,(i),NULL)
  */

        public static void lua_pushunsigned(IntPtr L, Int64 n)
        {
            lua_pushinteger(L, n);
        }

        public static UInt64 lua_tounsignedx(IntPtr L, int idx, IntPtr isint)
        {
            return (UInt64)lua_tointegerx(L, idx, isint);
        }

        public static UInt64 lua_tounsigned(IntPtr L, int idx)
        {
            return lua_tounsignedx(L, idx, IntPtr.Zero);
        }

        #endregion

        #region debug
        /*
  LUA_API int (lua_getstack) (lua_State *L, int level, lua_Debug *ar);
  LUA_API int (lua_getinfo) (lua_State *L, const char *what, lua_Debug *ar);
  LUA_API const char *(lua_getlocal) (lua_State *L, const lua_Debug *ar, int n);
  LUA_API const char *(lua_setlocal) (lua_State *L, const lua_Debug *ar, int n);
  LUA_API const char *(lua_getupvalue) (lua_State *L, int funcindex, int n);
  LUA_API const char *(lua_setupvalue) (lua_State *L, int funcindex, int n);

  LUA_API void *(lua_upvalueid) (lua_State *L, int fidx, int n);
  LUA_API void  (lua_upvaluejoin) (lua_State *L, int fidx1, int n1,
                                                 int fidx2, int n2);

  LUA_API void (lua_sethook) (lua_State *L, lua_Hook func, int mask, int count);
  LUA_API lua_Hook (lua_gethook) (lua_State *L);
  LUA_API int (lua_gethookmask) (lua_State *L);
  LUA_API int (lua_gethookcount) (lua_State *L);

  */

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getstack", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_getstack(IntPtr state, int level, IntPtr debug);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getinfo", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_getinfo(IntPtr state, String what, IntPtr debug);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getlocal", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String lua_getlocal(IntPtr state, IntPtr debug, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_setlocal", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String lua_setlocal(IntPtr state, IntPtr debug, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_getupvalue", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String lua_getupvalue(IntPtr state, int funcIndex, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_setupvalue", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String lua_setupvalue(IntPtr state, int funcIndex, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_upvalueid", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr lua_upvalueid(IntPtr state, int fidx, int n);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_upvaluejoin", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String lua_upvaluejoin(IntPtr state, int fidx1, int n1, int fidx2, int n2);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_sethook", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_sethook(IntPtr state, LuaHookFunction func, int mask, int cout);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_gethook", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern LuaHookFunction lua_gethook(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_gethookmask", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_gethookmask(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "lua_gethookcount", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lua_gethookcount(IntPtr state);

        #endregion

        #region luaLib
        /*
  LUAMOD_API int (luaopen_base) (lua_State *L);

  #define LUA_COLIBNAME	"coroutine"
  LUAMOD_API int (luaopen_coroutine) (lua_State *L);

  #define LUA_TABLIBNAME	"table"
  LUAMOD_API int (luaopen_table) (lua_State *L);

  #define LUA_IOLIBNAME	"io"
  LUAMOD_API int (luaopen_io) (lua_State *L);

  #define LUA_OSLIBNAME	"os"
  LUAMOD_API int (luaopen_os) (lua_State *L);

  #define LUA_STRLIBNAME	"string"
  LUAMOD_API int (luaopen_string) (lua_State *L);

  #define LUA_UTF8LIBNAME	"utf8"
  LUAMOD_API int (luaopen_utf8) (lua_State *L);

  #define LUA_BITLIBNAME	"bit32"
  LUAMOD_API int (luaopen_bit32) (lua_State *L);

  #define LUA_MATHLIBNAME	"math"
  LUAMOD_API int (luaopen_math) (lua_State *L);

  #define LUA_DBLIBNAME	"debug"
  LUAMOD_API int (luaopen_debug) (lua_State *L);

  #define LUA_LOADLIBNAME	"package"
  LUAMOD_API int (luaopen_package) (lua_State *L);


  //open all previous libraries
  LUALIB_API void (luaL_openlibs) (lua_State *L);
  */

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_base", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_base(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_coroutine", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_coroutine(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_table", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_table(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_io", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_io(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_os", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_os(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_string", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_string(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_utf8", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_utf8(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_bit32", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_bit32(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_math", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_math(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_debug", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_debug(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaopen_package", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaopen_package(IntPtr state);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_openlibs", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_openlibs(IntPtr state);

        #endregion

        #region luaAuxLib functions
        /*
  // extra error code for 'luaL_load' 
  #define LUA_ERRFILE     (LUA_ERRERR+1)


  typedef struct luaL_Reg {
    const char *name;
    lua_CFunction func;
  } luaL_Reg;


  #define LUAL_NUMSIZES	(sizeof(lua_Integer)*16 + sizeof(lua_Number))

  LUALIB_API void (luaL_checkversion_) (lua_State *L, lua_Number ver, size_t sz);
  #define luaL_checkversion(L)  \
       luaL_checkversion_(L, LUA_VERSION_NUM, LUAL_NUMSIZES)

  LUALIB_API int (luaL_getmetafield) (lua_State *L, int obj, const char *e);
  LUALIB_API int (luaL_callmeta) (lua_State *L, int obj, const char *e);
  LUALIB_API const char *(luaL_tolstring) (lua_State *L, int idx, size_t *len);
  LUALIB_API int (luaL_argerror) (lua_State *L, int arg, const char *extramsg);
  LUALIB_API const char *(luaL_checklstring) (lua_State *L, int arg,
                                                            size_t *l);
  LUALIB_API const char *(luaL_optlstring) (lua_State *L, int arg,
                                            const char *def, size_t *l);
  LUALIB_API lua_Number (luaL_checknumber) (lua_State *L, int arg);
  LUALIB_API lua_Number (luaL_optnumber) (lua_State *L, int arg, lua_Number def);

  LUALIB_API lua_Integer (luaL_checkinteger) (lua_State *L, int arg);
  LUALIB_API lua_Integer (luaL_optinteger) (lua_State *L, int arg,
                                            lua_Integer def);

  LUALIB_API void (luaL_checkstack) (lua_State *L, int sz, const char *msg);
  LUALIB_API void (luaL_checktype) (lua_State *L, int arg, int t);
  LUALIB_API void (luaL_checkany) (lua_State *L, int arg);

  LUALIB_API int   (luaL_newmetatable) (lua_State *L, const char *tname);
  LUALIB_API void  (luaL_setmetatable) (lua_State *L, const char *tname);
  LUALIB_API void *(luaL_testudata) (lua_State *L, int ud, const char *tname);
  LUALIB_API void *(luaL_checkudata) (lua_State *L, int ud, const char *tname);

  LUALIB_API void (luaL_where) (lua_State *L, int lvl);
  LUALIB_API int (luaL_error) (lua_State *L, const char *fmt, ...);

  LUALIB_API int (luaL_checkoption) (lua_State *L, int arg, const char *def,
                                     const char *const lst[]);

  LUALIB_API int (luaL_fileresult) (lua_State *L, int stat, const char *fname);
  LUALIB_API int (luaL_execresult) (lua_State *L, int stat);

  // pre-defined references
  #define LUA_NOREF       (-2)
  #define LUA_REFNIL      (-1)

  LUALIB_API int (luaL_ref) (lua_State *L, int t);
  LUALIB_API void (luaL_unref) (lua_State *L, int t, int ref);

  LUALIB_API int (luaL_loadfilex) (lua_State *L, const char *filename,
                                                 const char *mode);

  #define luaL_loadfile(L,f)	luaL_loadfilex(L,f,NULL)

  LUALIB_API int (luaL_loadbufferx) (lua_State *L, const char *buff, size_t sz,
                                     const char *name, const char *mode);
  LUALIB_API int (luaL_loadstring) (lua_State *L, const char *s);

  LUALIB_API lua_State *(luaL_newstate) (void);

  LUALIB_API lua_Integer (luaL_len) (lua_State *L, int idx);

  LUALIB_API const char *(luaL_gsub) (lua_State *L, const char *s, const char *p,
                                                    const char *r);

  LUALIB_API void (luaL_setfuncs) (lua_State *L, const luaL_Reg *l, int nup);

  LUALIB_API int (luaL_getsubtable) (lua_State *L, int idx, const char *fname);

  LUALIB_API void (luaL_traceback) (lua_State *L, lua_State *L1,
                                    const char *msg, int level);

  LUALIB_API void (luaL_requiref) (lua_State *L, const char *modname,
                                   lua_CFunction openf, int glb);

  */
        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checkversion_", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_checkversion_(IntPtr state, double ver, int size);

        public static void luaL_checkVersion(IntPtr L)
        {
            luaL_checkversion_(L, 503, sizeof(Int64) * 16 + sizeof(double));
        }

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_getmetafield", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_getmetafield(IntPtr state, int obj, String name);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_callmeta", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_callmeta(IntPtr state, int obj, String name);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_tolstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String luaL_tolstring(IntPtr state, int idx, IntPtr len);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_argerror", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int lauL_argerror(IntPtr state, int numarg, string extramsg);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checklstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String luaL_checklstring(IntPtr state, int numArg, IntPtr size);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_optlstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String luaL_optlstring(IntPtr state, int numArg, String def, IntPtr size);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checknumber", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern double luaL_checknumber(IntPtr state, int numArg);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_optnumber", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern double luaL_optnumber(IntPtr state, int numArg, double def);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checkinteger", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern Int64 luaL_checkinteger(IntPtr state, int numArg);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_optinteger", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern Int64 luaL_optinteger(IntPtr state, int numArg, int def);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checkstack", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_checkstack(IntPtr state, int sz, String msg);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checktype", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_checktype(IntPtr state, int numArg, int t);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checkany", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_checkany(IntPtr state, int numArg);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_newmetatable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_newmetatable(IntPtr state, String s);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_setmetatable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_setmetatable(IntPtr state, String s);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_testudata", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr luaL_testudata(IntPtr state, int ud, String name);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checkudata", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr luaL_checkudata(IntPtr state, int ud, String name);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_where", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_where(IntPtr state, int lvl);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_error", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_error(IntPtr state, String s);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_checkoption", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_checkoption(IntPtr state, int narg, String def, String[] list);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_fileresult", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_fileresult(IntPtr state, int stat, String fname);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_execresult", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_execresult(IntPtr state, int stat);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_ref", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_ref(IntPtr state, int t);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_unref", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_unref(IntPtr state, int t, int Ref);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_loadfilex", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_loadfilex(IntPtr state, String filename, String mode);

        public static int luaL_loadfile(IntPtr L, String f)
        {
            return luaL_loadfilex(L, f, null);
        }

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_loadbufferx", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_loadbufferx(IntPtr state, IntPtr buffer, int bufferSize, String name, String mode);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_loadstring", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_loadstring(IntPtr state, String s);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_newstate", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr luaL_newstate();

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_len", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern Int64 luaL_len(IntPtr state, int idx);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_gsub", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern String luaL_gsub(IntPtr state, String s, String p, String r);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_setfuncs", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_setfuncs(IntPtr state, IntPtr luaReg, int nup);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_getsubtable", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int luaL_getsubtable(IntPtr state, int idx, String fname);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_traceback", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_traceback(IntPtr state, IntPtr L1, String msg, int level);

        [DllImport(Constants.LibraryName, EntryPoint = "luaL_requiref", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void luaL_requiref(IntPtr state, String modname, LuaCSFunction openf, int glb);

        #endregion

        #region lauxlib macros
        /*
  *
  ** ===============================================================
  ** some useful macros
  ** ===============================================================
  *

  #define luaL_newlibtable(L,l)	\
    lua_createtable(L, 0, sizeof(l)/sizeof((l)[0]) - 1)

  #define luaL_newlib(L,l)  \
    (luaL_checkversion(L), luaL_newlibtable(L,l), luaL_setfuncs(L,l,0))

  #define luaL_argcheck(L, cond,arg,extramsg)	\
        ((void)((cond) || luaL_argerror(L, (arg), (extramsg))))
  #define luaL_checkstring(L,n)	(luaL_checklstring(L, (n), NULL))
  #define luaL_optstring(L,n,d)	(luaL_optlstring(L, (n), (d), NULL))

  #define luaL_typename(L,i)	lua_typename(L, lua_type(L,(i)))

  #define luaL_dofile(L, fn) \
     (luaL_loadfile(L, fn) || lua_pcall(L, 0, LUA_MULTRET, 0))

  #define luaL_dostring(L, s) \
     (luaL_loadstring(L, s) || lua_pcall(L, 0, LUA_MULTRET, 0))

  #define luaL_getmetatable(L,n)	(lua_getfield(L, LUA_REGISTRYINDEX, (n)))

  #define luaL_opt(L,f,n,d)	(lua_isnoneornil(L,(n)) ? (d) : f(L,(n)))

  #define luaL_loadbuffer(L,s,sz,n)	luaL_loadbufferx(L,s,sz,n,NULL)


  */

        public static void luaL_newlibtable(IntPtr L, IntPtr l)
        {
            int size = 0; //todo
            lua_createtable(L, 0, size);
        }

        public static void luaL_newlib(IntPtr L, IntPtr l)
        {
            luaL_checkVersion(L);
            luaL_newlibtable(L, l);
            luaL_setfuncs(L, l, 0);
        }

        public static void luaL_argcheck(IntPtr L, String cond, String arg, String extramsg)
        {
            //todo
        }

        public static String luaL_checkstring(IntPtr state, int n)
        {
            return luaL_checklstring(state, n, IntPtr.Zero);
        }

        public static String luaL_optstring(IntPtr state, int n, String d)
        {
            return luaL_optlstring(state, n, d, IntPtr.Zero);
        }

        public static IntPtr LuaL_typename(IntPtr L, int i)
        {
            return lua_typename(L, lua_type(L, i));
        }

        public static int luaL_dofile(IntPtr L, String filename)
        {
            int ret = luaL_loadfile(L, filename);
            if (ret != 0)
            {
                return ret;
            }

            return lua_pcall(L, 0, LUA_MULTRET, 0);
        }

        public static int luaL_dostring(IntPtr L, String s)
        {
            int ret = luaL_loadstring(L, s);
            if (ret != 0)
            {
                return ret;
            }

            return lua_pcall(L, 0, LUA_MULTRET, 0);
        }

        public static void luaL_getmetatable(IntPtr L, string n)
        {
            lua_getfield(L, (int)LuaStackConstants.LUA_REGISTRYINDEX, n);
        }

        //       public static int luaL_opt(IntPtr L, ??? func, int n, int d)
        //       {
        //          return lua_isnoneornil(L, n) != 0 ? d : func(L, n);
        //       }

        public static int luaL_loadbuffer(IntPtr L, IntPtr s, int sz, String name)
        {
            return luaL_loadbufferx(L, s, sz, name, null);
        }


        // TODO: 

        // Generic Buffer manipulation
        // File handles for IO library
        // compatibility with old module system
        // "Abstraction Layer" for basic report of messages and errors
        // Compatibility with deprecated conversions

        #endregion
    }
}