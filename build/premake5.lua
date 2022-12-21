solution "Lua"
   location("../")
   configurations { "Debug", "Release" }
   platforms{"x32", "x64"}
   startproject "lua"
   
   defines{"LUA_BUILD_AS_DLL"}
   
  configuration { "Debug" }
    defines { "DEBUG", "TRACE"}
    symbols "On"
    optimize "Off"
 
  configuration { "Release" }
    optimize "Speed"
	
  configuration {"x32"}
   targetdir "../bin/x32"
   debugdir "../bin/x32"

  configuration {"x64"}
   targetdir "../bin/x64"
   debugdir "../bin/x64"

   
project "lua5.4"
	kind "SharedLib"
	language "C"
	location "luadll"
	files{"../lua-5.4.4/src/*.h", "../lua-5.4.4/src/*.c"}
	excludes {"../lua-5.4.4/src/luac.c", "../lua-5.4.4/src/lua.c"}
	
project "luac"
	kind "ConsoleApp"
	language "C"
	location "luac"
	files{
   "../lua-5.4.4/src/luac.c", 
   "../lua-5.4.4/src/lopcodes.c",
   "../lua-5.4.4/src/lopcodes.h",
   "../lua-5.4.4/src/ldump.c",
   "../lua-5.4.4/src/ldebug.c",
   "../lua-5.4.4/src/lobject.c",
   "../lua-5.4.4/src/lvm.c",
   "../lua-5.4.4/src/ltm.c",
   "../lua-5.4.4/src/ldo.c",
   "../lua-5.4.4/src/lstate.c",
   "../lua-5.4.4/src/ltable.c",
   "../lua-5.4.4/src/lmem.c",
   "../lua-5.4.4/src/lgc.c",
   "../lua-5.4.4/src/lfunc.c",
   "../lua-5.4.4/src/lzio.c",
   "../lua-5.4.4/src/lparser.c",
   "../lua-5.4.4/src/lundump.c",
   "../lua-5.4.4/src/llex.c",
   "../lua-5.4.4/src/lcode.c",
   "../lua-5.4.4/src/lctype.c",
   "../lua-5.4.4/src/lstring.c"}
   links {"lua5.4"}
 
project "lua"
	kind "ConsoleApp"
	language "C"
	location "lua-exe"
	files{"../lua-5.4.4/src/lua.c"}
	links{"lua5.4"}
 
project "Lua#"
	kind "SharedLib"
	language "C#"
	location "Lua#"
	files{"../src/*.cs"}
	targetdir "../bin"
	links("System")
	namespace("Lua")
   
project "Test Lua"
	language  "C#"
	kind      "ConsoleApp"
	location "testLua"
	files     { "../testLua/**.cs" }
	vpaths { ["*"] = "../testLua" }
	targetdir "../bin"
	links     { "System", "Lua#"}
	namespace("luaTest")
