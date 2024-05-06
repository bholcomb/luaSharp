solution "Lua"
   location("../")
   configurations { "Debug", "Release" }
   platforms{"x64"}
   startproject "lua"
   
   filter {"system:Windows"}
      defines{"LUA_BUILD_AS_DLL"}
   
  configuration { "Debug" }
    defines { "DEBUG", "TRACE"}
    symbols "On"
    optimize "Off"
 
  configuration { "Release" }
    optimize "Speed"
	
  configuration {"x64"}
   targetdir "../bin/x64"
   debugdir "../bin/x64"

   
project "lua5.4"
	kind "SharedLib"
	language "C"
	location "luadll"
	files{"../lua-5.4.6/src/*.h", "../lua-5.4.6/src/*.c"}
	excludes {"../lua-5.4.6/src/luac.c", "../lua-5.4.6/src/lua.c"}
  filter{"system:Linux"}
    links {"m"}
	
project "luac"
	kind "ConsoleApp"
	language "C"
	location "luac"
	files{
   "../lua-5.4.6/src/luac.c", 
   "../lua-5.4.6/src/lopcodes.c",
   "../lua-5.4.6/src/lopcodes.h",
   "../lua-5.4.6/src/ldump.c",
   "../lua-5.4.6/src/ldebug.c",
   "../lua-5.4.6/src/lobject.c",
   "../lua-5.4.6/src/lvm.c",
   "../lua-5.4.6/src/ltm.c",
   "../lua-5.4.6/src/ldo.c",
   "../lua-5.4.6/src/lstate.c",
   "../lua-5.4.6/src/ltable.c",
   "../lua-5.4.6/src/lmem.c",
   "../lua-5.4.6/src/lgc.c",
   "../lua-5.4.6/src/lfunc.c",
   "../lua-5.4.6/src/lzio.c",
   "../lua-5.4.6/src/lparser.c",
   "../lua-5.4.6/src/lundump.c",
   "../lua-5.4.6/src/llex.c",
   "../lua-5.4.6/src/lcode.c",
   "../lua-5.4.6/src/lctype.c",
   "../lua-5.4.6/src/lstring.c"}
   links {"lua5.4"}
   filter{"system:Linux"}
      links {"m"}
 
project "lua"
	kind "ConsoleApp"
	language "C"
	location "lua-exe"
	files{"../lua-5.4.6/src/lua.c"}
	links{"lua5.4"}
  filter{"system:Linux"}
    links {"m"}
 
project "LuaSharp"
	kind "SharedLib"
	language "C#"
	location "LuaSharp"
	files{"../src/*.cs"}
	targetdir "../bin"
	links("System")
	namespace("LuaSharp")
   
project "Test Lua"
	language  "C#"
	kind      "ConsoleApp"
	location "testLua"
	files     { "../testLua/**.cs" }
	vpaths { ["*"] = "../testLua" }
	targetdir "../bin"
	links     { "System", "LuaSharp"}
	namespace("luaTest")
