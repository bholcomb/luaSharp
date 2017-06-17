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

   
project "lua5.3"
	kind "SharedLib"
	language "C"
	location "luadll"
	files{"../lua5.3.4/src/*.h", "../lua5.3.4/src/*.c"}
	excludes {"../lua5.3.4/src/luac.c", "../lua5.3.4/src/lua.c"}
	systemversion("10.0.15063.0")
	
project "luac"
	kind "ConsoleApp"
	language "C"
	location "luac"
	files{"../lua5.3.4/src/luac.c", "../lua5.3.4/src/lopcodes.c", "../lua5.3.4/src/lopcodes.h","../lua5.3.4/src/ldump.c"}
	links {"lua5.3"}
	systemversion("10.0.15063.0")
 
project "lua"
	kind "ConsoleApp"
	language "C"
	location "lua-exe"
	files{"../lua5.3.4/src/lua.c"}
	links{"lua5.3"}
	systemversion("10.0.15063.0")
 
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