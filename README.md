# LuaSharp
Simple bindings to the Lua C API for C#

I originally wanted simple Lua bindings so I could reuse a lot of code I had written for previous projects, follow tutorials for the C API, and wanted to learn more about P/Invoke.  
I was also unhappy with other projects that reimplemented (most of) the Lua VM in C# or had a custom version of the VM.  I wanted to use the standard VM and have the ability to use other lua C modules (such as socket and lfs) as well as someday use LuaJIT.

I've also thrown in some of the higher level C# classes that I use to make using the bindings a little easier.