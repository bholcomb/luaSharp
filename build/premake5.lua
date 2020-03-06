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
	files{"../lua-5.4.0/src/*.h", "../lua-5.4.0/src/*.c"}
	excludes {"../lua-5.4.0/src/luac.c", "../lua-5.4.0/src/lua.c"}
	
project "luac"
	kind "ConsoleApp"
	language "C"
	location "luac"
	files{
   "../lua-5.4.0/src/luac.c", 
   "../lua-5.4.0/src/lopcodes.c",
   "../lua-5.4.0/src/lopcodes.h",
   "../lua-5.4.0/src/ldump.c",
   "../lua-5.4.0/src/ldebug.c",
   "../lua-5.4.0/src/lobject.c",
   "../lua-5.4.0/src/lvm.c",
   "../lua-5.4.0/src/ltm.c",
   "../lua-5.4.0/src/ldo.c",
   "../lua-5.4.0/src/lstate.c",
   "../lua-5.4.0/src/ltable.c",
   "../lua-5.4.0/src/lmem.c",
   "../lua-5.4.0/src/lgc.c",
   "../lua-5.4.0/src/lfunc.c",
   "../lua-5.4.0/src/lzio.c",
   "../lua-5.4.0/src/lparser.c",
   "../lua-5.4.0/src/lundump.c",
   "../lua-5.4.0/src/llex.c",
   "../lua-5.4.0/src/lcode.c",
   "../lua-5.4.0/src/lctype.c",
   "../lua-5.4.0/src/lstring.c"}
   
	links {"lua5.4"}
 
project "lua"
	kind "ConsoleApp"
	language "C"
	location "lua-exe"
	files{"../lua-5.4.0/src/lua.c"}
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